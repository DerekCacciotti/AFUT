using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.Pages;
using AFUT.Tests.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.BaselineForm
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class BaselineFormValidationTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        public BaselineFormValidationTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(1)]
        public void SubmitShowsRelationshipValidationMessage(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToBaselineForm(driver, formsPane);
            _output.WriteLine("[PASS] Intake (Baseline) form loaded successfully");

            SelectRelationshipDropdown(driver);
            _output.WriteLine("[INFO] Ensured relationship dropdown is set to '--Select--'");

            var submitButton = driver.FindElements(By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    (el.Text?.Contains("Submit", StringComparison.OrdinalIgnoreCase) ?? false) &&
                    (el.GetAttribute("title")?.Contains("Save", StringComparison.OrdinalIgnoreCase) ?? true))
                ?? throw new InvalidOperationException("Baseline form Submit button was not found.");

            _output.WriteLine($"[INFO] Found Submit button: {submitButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit button without entering required fields");

            var validationElement = driver.FindElements(By.CssSelector(
                    ".text-danger, " +
                    "span.text-danger, " +
                    "span[style*='color: red'], " +
                    "span[style*='color:Red'], " +
                    "div.alert.alert-danger"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    !string.IsNullOrWhiteSpace(el.Text) &&
                    el.Text.Contains("relationship to target child", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(validationElement);
            var validationText = validationElement!.Text.Trim();
            _output.WriteLine($"[INFO] Validation message displayed: {validationText}");
            Assert.Contains("Please enter relationship to target child", validationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Relationship validation is shown when submitting empty Baseline form");
        }

        private void NavigateToBaselineForm(IPookieWebDriver driver, IWebElement formsPane)
        {
            var baselineFormLink = formsPane.FindElements(By.CssSelector(
                    "a.list-group-item.moreInfo[href*='Intake.aspx'], " +
                    "a.moreInfo[data-formtype='in'], " +
                    "a.list-group-item[title='Intake']"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    (el.Text?.Contains("Baseline", StringComparison.OrdinalIgnoreCase) ?? false))
                ?? throw new InvalidOperationException("Baseline Form link was not found in the Forms tab.");

            _output.WriteLine($"[INFO] Found Baseline Form link: {baselineFormLink.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, baselineFormLink);
            driver.WaitForReady(30);
            driver.WaitForUpdatePanel(30);
            Thread.Sleep(1000);

            var currentUrl = driver.Url ?? string.Empty;
            Assert.Contains("Intake.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ipk=", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Navigated to Intake page: {currentUrl}");
        }

        private void SelectRelationshipDropdown(IPookieWebDriver driver)
        {
            var dropdownSelector = "select.form-control[id$='ddlRelation'], select[id*='PC1Form_ddlRelation']";
            WebElementHelper.SelectDropdownOption(
                driver,
                dropdownSelector,
                "Relationship to target child dropdown",
                "--Select--",
                string.Empty);
        }
    }
}


