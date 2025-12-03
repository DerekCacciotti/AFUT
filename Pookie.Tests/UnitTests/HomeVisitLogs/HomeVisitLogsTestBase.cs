using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.UnitTests;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.HomeVisitLogs
{
    public abstract class HomeVisitLogsTestBase : IClassFixture<AppConfig>
    {
        protected readonly AppConfig _config;
        protected readonly IPookieDriverFactory _driverFactory;
        protected readonly ITestOutputHelper _output;

        protected HomeVisitLogsTestBase(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");
        }

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        protected void NavigateToHomeVisitLogs(IPookieWebDriver driver, string pc1Id)
        {
            var (_, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            _output.WriteLine("[INFO] Forms tab loaded.");

            var linkSelector = "a.list-group-item.moreInfo[href*='HomeVisitLogs.aspx'], " +
                               "a.moreInfo[data-formtype='hv']";
            var homeVisitLogsLink = formsPane.FindElements(By.CssSelector(linkSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Home Visit Logs link was not found on the Forms tab.");

            CommonTestHelper.ClickElement(driver, homeVisitLogsLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
        }

        protected void OpenNewHomeVisitLog(IPookieWebDriver driver)
        {
            var newLogButton = driver.FindElements(By.CssSelector(
                    "a.btn.btn-default.pull-right[href='#'], " +
                    "a.btn.btn-default[href='#'][title*='New Log'], " +
                    "a.btn.btn-default[data-formtype='hv'][data-action*='new'], " +
                    "a.btn.btn-default span.glyphicon-plus"))
                .Select(btn => btn.TagName.Equals("a", StringComparison.OrdinalIgnoreCase) ? btn : btn.FindElement(By.XPath("./ancestor::a[1]")))
                .FirstOrDefault(anchor => anchor.Displayed && anchor.Text.Contains("New Log", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("New Log button was not found on the Home Visit Logs page.");

            _output.WriteLine("[INFO] Clicking New Log button.");
            CommonTestHelper.ClickElement(driver, newLogButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(500);
        }

        protected static void ClickSubmit(IPookieWebDriver driver)
        {
            var submitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary[href*='btnSubmit'], " +
                "a.btn.btn-primary[id$='btnSubmit'], " +
                "button.btn.btn-primary[type='submit']",
                "Submit button",
                15);

            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
        }

        protected static void SetDateOfVisit(IPookieWebDriver driver, string dateValue)
        {
            var dateInput = WaitForDateOfVisitInput(driver);

            WebElementHelper.SetInputValue(driver, dateInput, dateValue, "Date of Visit", triggerBlur: true);
        }

        protected static void SetVisitStartTime(IPookieWebDriver driver, string startTime)
        {
            var timeInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.form-control[id$='txtVisitStartTime'], input.time.form-control[id*='txtVisitStartTime']",
                "Visit start time input",
                15);

            WebElementHelper.SetInputValue(driver, timeInput, startTime, "Visit start time", triggerBlur: true);
        }

        protected static void SelectVisitStartPeriod(IPookieWebDriver driver, string optionText, string optionValue)
        {
            WebElementHelper.SelectDropdownOption(driver,
                "select.form-control[id$='ddlTimeAMPM']",
                "Visit AM/PM dropdown",
                optionText,
                optionValue);
        }

        protected static void SelectVisitTypeOption(IPookieWebDriver driver, int optionIndex)
        {
            var checkbox = driver.FindElements(By.CssSelector($"input[type='checkbox'][id$='cblVisitType_{optionIndex}']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Visit type checkbox with index {optionIndex} was not found.");

            if (!checkbox.Selected)
            {
                checkbox.Click();
                driver.WaitForUpdatePanel(10);
                driver.WaitForReady(10);
                Thread.Sleep(250);
            }
        }

        protected static IWebElement WaitForDateOfVisitInput(IPookieWebDriver driver)
        {
            var selectors = new[]
            {
                "div.input-group.date input.form-control[id$='txtDateofVisit']",
                "div.input-group.date input.form-control[id*='DateofVisit']",
                "input.form-control[id*='DateofVisit']",
                "input.form-control[name*='DateofVisit']",
                "input.form-control[data-field*='DateofVisit']"
            };

            var endTime = DateTime.Now.AddSeconds(20);
            while (DateTime.Now <= endTime)
            {
                foreach (var selector in selectors)
                {
                    var candidate = driver.FindElements(By.CssSelector(selector))
                        .FirstOrDefault(el => el.Displayed && el.Enabled);
                    if (candidate != null)
                    {
                        return candidate;
                    }
                }

                Thread.Sleep(250);
            }

            throw new InvalidOperationException("'Date of Visit input' was not found within the expected time.");
        }

        protected static string GetValidationMessages(IPookieWebDriver driver)
        {
            var selectors = ".validation-summary-errors, .alert.alert-danger, .alert-danger, .text-danger, .modal-body .alert";
            var messages = driver.FindElements(By.CssSelector(selectors))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .ToList();

            return messages.Count == 0 ? string.Empty : string.Join(" | ", messages);
        }

        protected static void AssertValidationContains(string validationText, params string[] expectedMessages)
        {
            foreach (var expected in expectedMessages)
            {
                Assert.Contains(expected, validationText, StringComparison.OrdinalIgnoreCase);
            }
        }

        protected static void SelectFromChosen(IPookieWebDriver driver, string chosenContainerSelector, string optionText)
        {
            var chosenContainer = driver.FindElements(By.CssSelector(chosenContainerSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Chosen container '{chosenContainerSelector}' was not found or visible.");

            var chosenSingle = chosenContainer.FindElement(By.CssSelector(".chosen-single"));
            chosenSingle.Click();
            driver.WaitForReady(2);

            var options = chosenContainer.FindElements(By.CssSelector(".chosen-drop .chosen-results li"));
            var targetOption = options.FirstOrDefault(li =>
                li.Displayed &&
                li.Text.Trim().Equals(optionText, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Option '{optionText}' was not found in Chosen dropdown '{chosenContainerSelector}'.");

            targetOption.Click();
            driver.WaitForReady(2);
        }
    }
}

