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

namespace AFUT.Tests.UnitTests.Discharge
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class DischargeTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;
        private string TargetPc1Id => _config.TestPc1Id;

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        public DischargeTests(AppConfig config, ITestOutputHelper output)
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
        public void NavigateToDischargeForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Discharge form
            NavigateToDischargeForm(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Discharge form page");

            var pc1Display = CommonTestHelper.FindPc1Display(driver, pc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on Discharge page.");
            Assert.Contains(pc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Verified PC1 ID display: {pc1Display}");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void SubmitDischargeFormWithValidation(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Discharge form
            NavigateToDischargeForm(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Discharge form page");

            // Enter today's date in the Discharge Date field
            var dateInput = driver.FindElements(By.CssSelector(
                "div.input-group.date input.form-control, " +
                "input.form-control[class*='2dy']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Discharge Date input was not found.");

            var todayDate = DateTime.Now.ToString("MM/dd/yy");
            WebElementHelper.SetInputValue(driver, dateInput, todayDate, "Discharge Date", triggerBlur: true);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Submit to proceed to reason selection
            var submitButton = driver.FindElements(By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found.");

            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            // Select "Other" as the discharge reason
            var reasonDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlDischargeReason']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Discharge Reason dropdown was not found.");

            var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(reasonDropdown);
            selectElement.SelectByValue("99");
            _output.WriteLine("[INFO] Selected 'Other' as discharge reason");
            
            // Trigger change event to ensure Specify field appears
            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", reasonDropdown);
            
            driver.WaitForUpdatePanel(15);
            driver.WaitForReady(15);
            Thread.Sleep(1500);

            // Verify Specify text box appears
            var specifyTextBox = driver.WaitforElementToBeInDOM(By.CssSelector(
                "input.form-control[id*='Specify'], " +
                "div[id*='DischargeReasonSpecify'] input.form-control"), 10)
                ?? throw new InvalidOperationException("Specify text box was not found after selecting 'Other'");

            Assert.True(specifyTextBox.Displayed, "Specify text box is not displayed after selecting 'Other'");
            _output.WriteLine("[PASS] Specify text box appeared after selecting 'Other'");

            // Submit without filling the Specify field - should trigger validation
            var submitReasonButton = driver.FindElements(By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found.");

            CommonTestHelper.ClickElement(driver, submitReasonButton);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1000);

            // Verify validation message appears
            var validationMessage = driver.FindElements(By.CssSelector(
                "span[style*='color:Red'], " +
                "span[style*='color: red'], " +
                ".text-danger, " +
                "span[id*='rfvSpecify']"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

            Assert.NotNull(validationMessage);
            _output.WriteLine("[PASS] Specify field validation message displayed correctly");

            // Select a random valid reason (not "Other")
            reasonDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlDischargeReason']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Discharge Reason dropdown was not found.");

            selectElement = new OpenQA.Selenium.Support.UI.SelectElement(reasonDropdown);
            
            var validOptions = selectElement.Options
                .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) && 
                             opt.GetAttribute("value") != "99" &&
                             !opt.Text.Contains("Select", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!validOptions.Any())
            {
                throw new InvalidOperationException("No valid discharge reason options found.");
            }

            var random = new Random();
            var randomOption = validOptions[random.Next(validOptions.Count)];
            selectElement.SelectByValue(randomOption.GetAttribute("value"));
            _output.WriteLine($"[INFO] Selected random discharge reason: {randomOption.Text.Trim()}");
            
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Submit with valid data
            submitReasonButton = driver.FindElements(By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found.");

            CommonTestHelper.ClickElement(driver, submitReasonButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);

            // Verify success toast message
            var toastMessage = WebElementHelper.GetToastMessage(driver, 1500);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed.");

            Assert.Contains("saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Discharge form submitted successfully");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(3)]
        public void ReinstateCaseFromDischargeForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Discharge form
            NavigateToDischargeForm(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Discharge form page");

            // Find and click Reinstate button
            var reinstateButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-warning, " +
                "a[id*='btnReinstate']"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("Reinstate", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Reinstate button was not found on the Discharge form page.");

            _output.WriteLine("[INFO] Found Reinstate button, clicking...");
            CommonTestHelper.ClickElement(driver, reinstateButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);

            // Verify success toast message
            var toastMessage = WebElementHelper.GetToastMessage(driver, 1500);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Reinstate success toast message was not displayed.");

            Assert.Contains("Case Reinstated", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("reinstated", toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Case reinstated successfully");
        }

        #region Helper Methods

        /// <summary>
        /// Navigates to the Discharge form page from the forms pane
        /// </summary>
        private void NavigateToDischargeForm(IPookieWebDriver driver, IWebElement formsPane, string pc1Id)
        {
            // Find Discharge link using CSS classes and attributes (NOT ASP.NET IDs)
            var dischargeLink = formsPane.FindElements(By.CssSelector(
                "a.list-group-item.moreInfo[href*='Discharge.aspx'], " +
                "a.list-group-item.moreInfo[href*='PreDischarge.aspx'], " +
                "a.moreInfo[data-formtype='dc'], " +
                "a.list-group-item[title='Discharge']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Discharge link was not found inside the Forms tab.");

            _output.WriteLine($"Found Discharge link: {dischargeLink.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, dischargeLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            // Verify we're on a Discharge page (can be PreDischarge.aspx for new or discharge.aspx for existing)
            var currentUrl = driver.Url;
            var isDischargeOrPreDischarge = currentUrl.Contains("discharge.aspx", StringComparison.OrdinalIgnoreCase) || 
                                            currentUrl.Contains("PreDischarge.aspx", StringComparison.OrdinalIgnoreCase);
            Assert.True(isDischargeOrPreDischarge, $"Expected Discharge page but got: {currentUrl}");
            Assert.Contains(pc1Id, currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Discharge form page opened successfully: {currentUrl}");

            // Wait for page to be fully loaded
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Verify form container is present
            var formContainer = driver.WaitforElementToBeInDOM(By.CssSelector(
                ".panel-body, " +
                ".form-horizontal, " +
                "form, " +
                ".container-fluid"), 10);

            Assert.NotNull(formContainer);
            _output.WriteLine("[PASS] Discharge form container is present on the page");
        }

        #endregion
    }
}

