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

namespace AFUT.Tests.UnitTests.CaseLevels
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class CaseLevelsTests : IClassFixture<AppConfig>
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

        public CaseLevelsTests(AppConfig config, ITestOutputHelper output)
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
        public void NavigateToCaseLevelsForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Case Levels
            NavigateToCaseLevels(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Case Levels form page");

            var pc1Display = CommonTestHelper.FindPc1Display(driver, pc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on Case Levels page.");
            Assert.Contains(pc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Verified PC1 ID display: {pc1Display}");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void VerifyAddNewLevelButtonOpensForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Case Levels
            NavigateToCaseLevels(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Case Levels form page");

            // Click "New Level Record" button
            OpenNewLevelForm(driver);
            _output.WriteLine("[PASS] Successfully clicked New Level Record button");

            // Wait for form heading to appear (this indicates the form is now visible)
            var formHeading = WaitForFormHeading(driver);
            var headingText = formHeading.Text.Trim();
            _output.WriteLine($"[INFO] Form heading: {headingText}");
            Assert.Contains("Add New Level", headingText, StringComparison.OrdinalIgnoreCase);

            // Explicitly verify the Add New Level title span is visible (fallback to ID selector as needed)
            var headingById = driver.FindElements(By.CssSelector("span[id$='lblAddEditLevelTitle']"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                ?? throw new InvalidOperationException("Add New Level heading span was not visible on the page.");

            _output.WriteLine($"[PASS] Verified heading span text: {headingById.Text.Trim()}");

            // Verify form fields are present
            VerifyFormFieldsPresent(driver);
            _output.WriteLine("[PASS] All required form fields are present");
        }

        #region Helper Methods

        /// <summary>
        /// Navigates to the Case Levels form page from the forms pane
        /// </summary>
        private void NavigateToCaseLevels(IPookieWebDriver driver, IWebElement formsPane, string pc1Id)
        {
            // Find Case Levels link using CSS classes and attributes (NOT ASP.NET IDs)
            var caseLevelsLink = formsPane.FindElements(By.CssSelector(
                "a.list-group-item.moreInfo[href*='LevelForm.aspx'], " +
                "a.moreInfo[data-formtype='lv'], " +
                "a.list-group-item[title='Case Levels']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Case Levels link was not found inside the Forms tab.");

            _output.WriteLine($"Found Case Levels link: {caseLevelsLink.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, caseLevelsLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            // Verify we're on the Case Levels page
            var currentUrl = driver.Url;
            Assert.Contains("LevelForm.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Case Levels form page opened successfully: {currentUrl}");

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
            _output.WriteLine("[PASS] Case Levels form container is present on the page");
        }

        /// <summary>
        /// Clicks the "New Level Record" button to open the add new level form
        /// </summary>
        private void OpenNewLevelForm(IPookieWebDriver driver)
        {
            // Find "New Level Record" button using CSS classes (NOT ASP.NET IDs)
            var newLevelButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-default.pull-right, " +
                "a.btn.btn-default"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("New Level Record", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("New Level Record button was not found on the Case Levels page.");

            var buttonText = newLevelButton.Text?.Trim() ?? string.Empty;
            _output.WriteLine($"Found New Level Record button: {buttonText}");
            
            CommonTestHelper.ClickElement(driver, newLevelButton);
            driver.WaitForReady(30);
            driver.WaitForUpdatePanel(30);
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Waits for the form heading to appear and be displayed
        /// </summary>
        private IWebElement WaitForFormHeading(IPookieWebDriver driver)
        {
            var endTime = DateTime.Now.AddSeconds(15);

            while (DateTime.Now <= endTime)
            {
                var heading = driver.FindElements(By.CssSelector(
                    ".panel-heading span, " +
                    "span[id*='lblAddEditLevelTitle'], " +
                    ".panel-heading"))
                    .FirstOrDefault(el => el.Displayed && 
                        !string.IsNullOrWhiteSpace(el.Text) && 
                        el.Text.Contains("Add New Level", StringComparison.OrdinalIgnoreCase));

                if (heading != null)
                {
                    return heading;
                }

                Thread.Sleep(500);
            }

            throw new InvalidOperationException("Form heading 'Add New Level' was not found or did not become visible within the expected time.");
        }

        /// <summary>
        /// Verifies that all required form fields are present in the Add New Level form
        /// </summary>
        private void VerifyFormFieldsPresent(IPookieWebDriver driver)
        {
            // Verify Level dropdown
            var levelDropdown = driver.FindElements(By.CssSelector(
                "select.form-control"))
                .FirstOrDefault(el => el.Displayed && 
                    el.FindElements(By.CssSelector("option[value='12']")).Any())
                ?? throw new InvalidOperationException("Level dropdown was not found in the form.");
            
            _output.WriteLine("[INFO] Level dropdown is present");

            // Verify Additional Case Weight dropdown
            var caseWeightDropdown = driver.FindElements(By.CssSelector(
                "select.form-control"))
                .FirstOrDefault(el => el.Displayed && 
                    el.FindElements(By.CssSelector("option[value='0.00']")).Any());
            
            if (caseWeightDropdown != null)
            {
                _output.WriteLine("[INFO] Additional Case Weight dropdown is present");
            }

            // Verify Level Date input
            var levelDateInput = driver.FindElements(By.CssSelector(
                "div.input-group.date input.form-control, " +
                "input.form-control[type='text'][class*='2dy'], " +
                "input.form-control[class*='2dy']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Level Date input was not found in the form.");
            
            _output.WriteLine("[INFO] Level Date input is present");

            // Verify Level Comments textarea
            var levelCommentsTextarea = driver.FindElements(By.CssSelector(
                "textarea.form-control, " +
                "textarea.form-control.is-maxlength"))
                .FirstOrDefault(el => el.Displayed);
            
            if (levelCommentsTextarea != null)
            {
                _output.WriteLine("[INFO] Level Comments textarea is present");
            }

            // Verify Submit button
            var submitButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-primary"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found in the form.");
            
            _output.WriteLine("[INFO] Submit button is present");

            // Verify Cancel button
            var cancelButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-default"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("Cancel", StringComparison.OrdinalIgnoreCase));
            
            if (cancelButton != null)
            {
                _output.WriteLine("[INFO] Cancel button is present");
            }
        }

        #endregion
    }
}

