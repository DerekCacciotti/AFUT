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
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.PHQ9
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class PHQ9Tests : IClassFixture<AppConfig>
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

        public PHQ9Tests(AppConfig config, ITestOutputHelper output)
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
        public void CheckingTheAddNewOfPHQ9Form(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to PHQ9
            NavigateToPHQ9(driver, formsPane);
            _output.WriteLine("[PASS] Successfully navigated to PHQ9 list page");

            var pc1Display = CommonTestHelper.FindPc1Display(driver, pc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on PHQ9 page.");
            Assert.Contains(pc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Verified PC1 ID display: {pc1Display}");

            // Click New PHQ9 button
            CreateNewPHQ9Entry(driver, pc1Id);
            _output.WriteLine("[PASS] Successfully opened new PHQ9 form");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void CheckingPHQ9FormValidationForParticipantField(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to PHQ9 and create new entry
            NavigateToPHQ9(driver, formsPane);
            _output.WriteLine("[PASS] Successfully navigated to PHQ9 list page");

            CreateNewPHQ9Entry(driver, pc1Id);
            _output.WriteLine("[PASS] Successfully opened new PHQ9 form");

            // Click Submit button without filling any fields
            var submitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary[title*='Save'], " +
                "a.btn.btn-primary",
                "Submit button",
                15);

            _output.WriteLine($"Found submit button: {submitButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked submit button");

            // Verify validation message appears for Participant field
            var validationMessage = driver.FindElements(By.CssSelector(
                "span[style*='color: red'][style*='display: inline'], " +
                "span[style*='color:Red'][style*='display:inline'], " +
                ".text-danger, " +
                "span.text-danger, " +
                "span[id*='rfvParticipant']"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

            Assert.NotNull(validationMessage);
            var validationText = validationMessage.Text.Trim();
            _output.WriteLine($"[INFO] Validation message: {validationText}");

            Assert.Contains("Participant is required", validationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Participant validation message displayed correctly");

            // Select a random participant from the dropdown
            SelectRandomParticipant(driver);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Click Submit button again
            submitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary[title*='Save'], " +
                "a.btn.btn-primary",
                "Submit button",
                15);

            _output.WriteLine("Clicking submit button again after selecting participant");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            // Verify date validation message appears
            var dateValidationMessage = driver.FindElements(By.CssSelector(
                "span[style*='color:Red'], " +
                "span[style*='color: red'], " +
                ".text-danger, " +
                "span.text-danger"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("date", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(dateValidationMessage);
            var dateValidationText = dateValidationMessage.Text.Trim();
            _output.WriteLine($"[INFO] Date validation message: {dateValidationText}");

            Assert.Contains("PHQ date administered", dateValidationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Date validation message displayed correctly");

            // Enter a date that is before the case start date
            var dateInput = driver.FindElements(By.CssSelector(
                "div.input-group.date input.form-control, " +
                "input.form-control[type='text'][class*='2dy'], " +
                "input.form-control.replaceBlank"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ date administered input was not found.");

            _output.WriteLine("Setting date to 10/01/01 (before case start date)");
            WebElementHelper.SetInputValue(driver, dateInput, "10/01/01", "PHQ Date Administered", triggerBlur: true);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Click Submit button again
            submitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary[title*='Save'], " +
                "a.btn.btn-primary",
                "Submit button",
                15);

            _output.WriteLine("Clicking submit button again after entering date");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            // Verify case start date validation message appears
            var caseStartDateValidationMessage = driver.FindElements(By.CssSelector(
                "span[style*='color:Red'], " +
                "span[style*='color: red'], " +
                ".text-danger, " +
                "span.text-danger"))
                .FirstOrDefault(el => el.Displayed && 
                    !string.IsNullOrWhiteSpace(el.Text) && 
                    el.Text.Contains("case start date", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(caseStartDateValidationMessage);
            var caseStartDateValidationText = caseStartDateValidationMessage.Text.Trim();
            _output.WriteLine($"[INFO] Case start date validation message: {caseStartDateValidationText}");

            Assert.Contains("on or after the case start date", caseStartDateValidationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Case start date validation message displayed correctly");

            // Enter a valid date (after case start date)
            dateInput = driver.FindElements(By.CssSelector(
                "div.input-group.date input.form-control, " +
                "input.form-control[type='text'][class*='2dy'], " +
                "input.form-control.replaceBlank"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ date administered input was not found.");

            _output.WriteLine("Setting date to 10/25/25 (valid date)");
            WebElementHelper.SetInputValue(driver, dateInput, "10/25/25", "PHQ Date Administered", triggerBlur: true);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Click Submit button for final submission
            submitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary[title*='Save'], " +
                "a.btn.btn-primary",
                "Submit button",
                15);

            _output.WriteLine("Clicking submit button with valid data");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);

            // Verify success toast message appears
            var toastMessage = WebElementHelper.GetToastMessage(driver, 1000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed.");
            _output.WriteLine($"[INFO] Toast message: {toastMessage}");

            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Success toast message displayed correctly");

            // Wait for grid to refresh
            driver.WaitForReady(10);
            driver.WaitForUpdatePanel(10);
            Thread.Sleep(1000);

            // Verify the record appears in the grid
            var phq9Row = FindPHQ9Row(driver, "10/25/2025");
            Assert.NotNull(phq9Row);
            _output.WriteLine($"[INFO] Found PHQ9 row in grid: {phq9Row.Text}");
            
            // Verify the row contains expected data
            var rowText = phq9Row.Text;
            Assert.Contains("10/25/2025", rowText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PHQ9", rowText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] PHQ9 record successfully saved and appears in grid");
        }

        #region Helper Methods

        /// <summary>
        /// Finds a PHQ9 row in the grid by date
        /// </summary>
        private IWebElement? FindPHQ9Row(IPookieWebDriver driver, string dateText)
        {
            var grid = driver.WaitforElementToBeInDOM(By.CssSelector(
                "table.table.table-condensed, " +
                "table[id*='grPHQ9'], " +
                "div.panel-body table"), 20);

            if (grid == null)
            {
                _output.WriteLine("[WARN] PHQ9 grid not found.");
                return null;
            }

            var rows = grid.FindElements(By.CssSelector("tr"))
                .Where(tr => tr.Displayed && tr.FindElements(By.CssSelector("td")).Any())
                .ToList();

            _output.WriteLine($"[INFO] Found {rows.Count} data rows in PHQ9 grid.");

            foreach (var row in rows)
            {
                var rowText = row.Text ?? string.Empty;
                if (rowText.Contains(dateText, StringComparison.OrdinalIgnoreCase))
                {
                    _output.WriteLine($"[INFO] Matched row found with date '{dateText}'.");
                    return row;
                }
            }

            _output.WriteLine($"[WARN] No row matched date '{dateText}'.");
            return null;
        }

        /// <summary>
        /// Selects a random participant from the dropdown (excluding "Other" which requires additional input)
        /// </summary>
        private void SelectRandomParticipant(IPookieWebDriver driver)
        {
            var participantDropdown = driver.FindElements(By.CssSelector(
                "select.form-control"))
                .FirstOrDefault(el => el.Displayed && 
                    el.FindElements(By.CssSelector("option[value='01']")).Any())
                ?? throw new InvalidOperationException("Participant dropdown was not found.");

            var selectElement = new SelectElement(participantDropdown);
            
            // Get all options except "--Select--" and "Other" (to avoid additional input requirement)
            var validOptions = selectElement.Options
                .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) && 
                             opt.GetAttribute("value") != "04") // Exclude "Other"
                .ToList();

            if (!validOptions.Any())
            {
                throw new InvalidOperationException("No valid participant options found in dropdown.");
            }

            // Select a random option
            var random = new Random();
            var randomOption = validOptions[random.Next(validOptions.Count)];
            var optionText = randomOption.Text.Trim();
            var optionValue = randomOption.GetAttribute("value");

            selectElement.SelectByValue(optionValue);
            _output.WriteLine($"[INFO] Selected random participant: {optionText} (value: {optionValue})");
        }

        /// <summary>
        /// Navigates to the PHQ9 list page from the forms pane
        /// </summary>
        private void NavigateToPHQ9(IPookieWebDriver driver, IWebElement formsPane)
        {
            var phq9Link = formsPane.FindElements(By.CssSelector(
                "a.list-group-item.moreInfo[href*='PHQ9s.aspx'], " +
                "a.moreInfo[data-formtype='pq'], " +
                "a.list-group-item[title='PHQ9']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ9 link was not found inside the Forms tab.");

            _output.WriteLine($"Found PHQ9 link: {phq9Link.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, phq9Link);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            // Verify we're on the PHQ9s page
            var currentUrl = driver.Url;
            Assert.Contains("PHQ9s.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"Current URL: {currentUrl}");
        }

        /// <summary>
        /// Clicks the "New PHQ9" button and verifies the form opens
        /// </summary>
        private void CreateNewPHQ9Entry(IPookieWebDriver driver, string pc1Id, bool expectSuccess = true)
        {
            var newPHQ9Button = driver.FindElements(By.CssSelector(
                "a.btn.btn-default.pull-right[href*='PHQ9.aspx'][href*='phq9pk=0'], " +
                "a.btn[href*='PHQ9.aspx'][href*='phq9pk=0']"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("PHQ9", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("New PHQ9 button was not found on the PHQ9 page.");

            var buttonText = newPHQ9Button.Text?.Trim() ?? string.Empty;
            _output.WriteLine($"Found New PHQ9 button: {buttonText}");
            
            CommonTestHelper.ClickElement(driver, newPHQ9Button);
            driver.WaitForReady(30);
            driver.WaitForUpdatePanel(30);
            Thread.Sleep(1500);

            if (!expectSuccess)
            {
                return;
            }

            // Verify we're on the PHQ9 form page
            var currentUrl = driver.Url;
            Assert.Contains("PHQ9.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("phq9pk=0", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] PHQ9 form page opened successfully: {currentUrl}");

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
            _output.WriteLine("[PASS] PHQ9 form container is present on the page");
            
            // Log form elements for debugging
            var formElements = driver.FindElements(By.CssSelector(
                "input.form-control, " +
                "select.form-control, " +
                "textarea.form-control, " +
                "input[type='radio'], " +
                "input[type='checkbox']"));
            
            _output.WriteLine($"[INFO] Found {formElements.Count} form elements on the page");
        }

        #endregion
    }
}

