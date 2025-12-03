using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.HomeVisitLogs
{
    public class HomeVisitLogsTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public HomeVisitLogsTests(AppConfig config, ITestOutputHelper output)
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

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(1)]
        public void HomeVisitLogsLinkOpensForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting Home Visit Logs navigation test for PC1 {pc1Id}.");

            NavigateToHomeVisitLogs(driver, pc1Id);

            var currentUrl = driver.Url ?? string.Empty;
            _output.WriteLine($"[INFO] Current URL after click: {currentUrl}");

            Assert.Contains("HomeVisitLogs.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, currentUrl, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine("[PASS] Home Visit Logs URL loaded successfully.");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void HomeVisitLogsValidatesDateOfVisitField(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting Date of Visit validation test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);

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

            ClickSubmit(driver);
            var validationText = GetValidationMessages(driver);
            Assert.Contains("Missing Date of Visit.", validationText, StringComparison.OrdinalIgnoreCase);

            SetDateOfVisit(driver, "11/16/16");
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            AssertValidationContains(validationText,
                "Not a valid Date of Visit, Date of Visit must not be prior to the Case Start Date.",
                "Not a valid Date of Visit, Date of Visit must not be prior to the Intake Date.",
                "Not a valid Date of Visit, must be after 9/2/2017");

            SetDateOfVisit(driver, "11/11/18");
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            AssertValidationContains(validationText,
                "Not a valid Date of Visit, Date of Visit must not be prior to the Case Start Date.",
                "Not a valid Date of Visit, Date of Visit must not be prior to the Intake Date.");

            const string validDate = "11/16/25";
            SetDateOfVisit(driver, validDate);
            ClickSubmit(driver);
            driver.WaitForUpdatePanel(45);
            driver.WaitForReady(45);
            Thread.Sleep(1500);

            var summaryContainer = driver.FindElements(By.CssSelector("#main-div .noprint, div.noprint"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text) &&
                                       el.Text.IndexOf("PC1ID:", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? throw new InvalidOperationException("Confirmation summary with PC1 information was not displayed after submitting a valid Home Visit Log.");

            var summaryText = summaryContainer.Text?.Trim() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(summaryText), "Confirmation summary was empty.");
            Assert.Contains("PC1ID:", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Date of Visit:", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(validDate, summaryText, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine($"[PASS] Date of Visit validation flow completed. Summary text: {summaryText}");
        }

        private void NavigateToHomeVisitLogs(IPookieWebDriver driver, string pc1Id)
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

        private static void ClickSubmit(IPookieWebDriver driver)
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

        private static void SetDateOfVisit(IPookieWebDriver driver, string dateValue)
        {
            var dateInput = WaitForDateOfVisitInput(driver);

            WebElementHelper.SetInputValue(driver, dateInput, dateValue, "Date of Visit", triggerBlur: true);
        }

        private static IWebElement WaitForDateOfVisitInput(IPookieWebDriver driver)
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

                System.Threading.Thread.Sleep(250);
            }

            throw new InvalidOperationException("'Date of Visit input' was not found within the expected time.");
        }

        private static string GetValidationMessages(IPookieWebDriver driver)
        {
            var selectors = ".validation-summary-errors, .alert.alert-danger, .alert-danger, .text-danger, .modal-body .alert";
            var messages = driver.FindElements(By.CssSelector(selectors))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .ToList();

            return messages.Count == 0 ? string.Empty : string.Join(" | ", messages);
        }

        private static void AssertValidationContains(string validationText, params string[] expectedMessages)
        {
            foreach (var expected in expectedMessages)
            {
                Assert.Contains(expected, validationText, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

