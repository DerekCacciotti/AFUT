using System;
using System.Collections.Generic;
using System.Globalization;
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

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(3)]
        public void SubmitNewLevelRecordShowsValidationThenSaves(string pc1Id)
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

            // Open the Add New Level form
            OpenNewLevelForm(driver);
            _output.WriteLine("[PASS] Successfully clicked New Level Record button");

            // Ensure the form is visible
            var formHeading = WaitForFormHeading(driver);
            Assert.Contains("Add New Level", formHeading.Text, StringComparison.OrdinalIgnoreCase);

            // Select random values in dropdowns
            // Submit without filling anything – expect Level validation
            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            _output.WriteLine("[INFO] Clicked Submit button with empty form");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            AssertValidationMessageDisplayed(driver, "Level", "level dropdown");

            // Select Level only and submit – expect Level Date validation
            SelectRandomLevelOption(driver);
            _output.WriteLine("[INFO] Selected Level, submitting again to trigger date validation");

            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            AssertValidationMessageDisplayed(driver, "Level Date", "level date input");

            // Fill remaining fields and submit
            SelectAdditionalCaseWeightOption(driver);

            var levelDateInput = driver.FindElements(By.CssSelector(
                "div.input-group.date input.form-control, " +
                "input.form-control[type='text'][class*='2dy'], " +
                "input.form-control[class*='2dy']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Level Date input was not found in the form.");

            WebElementHelper.SetInputValue(driver, levelDateInput, "11/30/25", "Level Date", triggerBlur: true);
            _output.WriteLine("[INFO] Set Level Date to 11/30/25");

            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            _output.WriteLine("[INFO] Clicked Submit button after filling required fields");
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 1500);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed.");
            _output.WriteLine($"[INFO] Toast message: {toastMessage}");

            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Success toast message displayed correctly after submitting Level form");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(4)]
        public void SubmitNewLevelRecordWithInvalidDateShowsRequiredMessage(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToCaseLevels(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Case Levels form page");

            OpenNewLevelForm(driver);
            _output.WriteLine("[PASS] Successfully clicked New Level Record button");

            var formHeading = WaitForFormHeading(driver);
            Assert.Contains("Add New Level", formHeading.Text, StringComparison.OrdinalIgnoreCase);

            SelectRandomLevelOption(driver);
            SelectAdditionalCaseWeightOption(driver);

            var levelDateInput = driver.FindElements(By.CssSelector(
                    "div.input-group.date input.form-control, " +
                    "input.form-control[type='text'][class*='2dy'], " +
                    "input.form-control[class*='2dy']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Level Date input was not found in the form.");

            WebElementHelper.SetInputValue(driver, levelDateInput, "30/30/30", "Level Date", triggerBlur: true);
            _output.WriteLine("[INFO] Set Level Date to invalid value 30/30/30");

            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            _output.WriteLine("[INFO] Submitted Level form with invalid date");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            AssertValidationMessageDisplayed(driver, "Level Date is required!", "level date input");
            _output.WriteLine("[PASS] Validation message displayed for invalid Level Date");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(5)]
        public void SubmitNewLevelRecordWithDateBeforeCaseStartShowsValidation(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToCaseLevels(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Case Levels form page");

            const string caseStartDateText = "11/15/25";
            const string invalidLevelDateText = "11/14/25";
            const string expectedCaseStartMessageDate = "11/15/2025";

            _output.WriteLine($"[INFO] Case start date: {caseStartDateText} (parsed: {expectedCaseStartMessageDate})");

            OpenNewLevelForm(driver);
            _output.WriteLine("[PASS] Successfully clicked New Level Record button");

            var formHeading = WaitForFormHeading(driver);
            Assert.Contains("Add New Level", formHeading.Text, StringComparison.OrdinalIgnoreCase);

            SelectRandomLevelOption(driver);
            SelectAdditionalCaseWeightOption(driver);

            var levelDateInput = driver.FindElements(By.CssSelector(
                    "div.input-group.date input.form-control, " +
                    "input.form-control[type='text'][class*='2dy'], " +
                    "input.form-control[class*='2dy']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Level Date input was not found in the form.");

            WebElementHelper.SetInputValue(driver, levelDateInput, invalidLevelDateText, "Level Date", triggerBlur: true);
            _output.WriteLine($"[INFO] Set Level Date to {invalidLevelDateText} (one day before case start)");

            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            _output.WriteLine("[INFO] Submitted Level form with Level Date before case start");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            var expectedValidation = $"Level Date must be on or after the case start date of {expectedCaseStartMessageDate}!";
            AssertValidationMessageDisplayed(driver, expectedValidation, "level date input");
            _output.WriteLine($"[PASS] Validation message displayed for Level Date before case start ({expectedCaseStartMessageDate})");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(6)]
        public void EditLevelRecordUpdatesCaseWeight(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToCaseLevels(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Case Levels form page");

            var rowInfo = FindEditableLevelRow(driver);
            var initialCaseWeightValue = NormalizeCaseWeightValue(rowInfo.caseWeightText);

            _output.WriteLine($"[INFO] Editing Level '{rowInfo.levelText}' dated '{rowInfo.levelDateText}' with current weight '{rowInfo.caseWeightText}'.");

            CommonTestHelper.ClickElement(driver, rowInfo.editButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            var formHeading = WaitForFormHeading(driver, "Level");
            Assert.Contains("Level", formHeading.Text, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Form heading during edit: {formHeading.Text.Trim()}");

            var newCaseWeightValue = SelectAdditionalCaseWeightOption(driver, initialCaseWeightValue);
            Assert.False(string.IsNullOrWhiteSpace(newCaseWeightValue), "Unable to select a new Additional Case Weight option.");

            if (string.Equals(newCaseWeightValue, initialCaseWeightValue, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Failed to select a different Additional Case Weight option.");
            }

            // Submit the form
            var submitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary[title*='Save'], " +
                "a.btn.btn-primary",
                "Submit button",
                15);

            CommonTestHelper.ClickElement(driver, submitButton);
            _output.WriteLine("[INFO] Clicked Submit button (edit)");
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 1500);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after editing.");
            _output.WriteLine($"[INFO] Toast message: {toastMessage}");
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);

            driver.WaitForReady(10);
            driver.WaitForUpdatePanel(10);
            Thread.Sleep(1000);

            var updatedRow = FindLevelRowByLevelAndDate(driver, rowInfo.levelText, rowInfo.levelDateText);
            var updatedCaseWeightText = ExtractAdditionalCaseWeight(updatedRow);
            var normalizedUpdatedValue = NormalizeCaseWeightValue(updatedCaseWeightText);

            var expectedNormalizedValue = NormalizeCaseWeightValue(newCaseWeightValue);

            _output.WriteLine($"[INFO] Updated grid case weight: {updatedCaseWeightText} (normalized: {normalizedUpdatedValue})");
            Assert.Equal(expectedNormalizedValue, normalizedUpdatedValue);
            _output.WriteLine("[PASS] Grid displays the newly selected Additional Case Weight value");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(7)]
        public void DeleteLevelRecordShowsConfirmationAndSuccessToast(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToCaseLevels(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Case Levels form page");

            var rowInfo = FindEditableLevelRow(driver);
            _output.WriteLine($"[INFO] Preparing to delete Level '{rowInfo.levelText}' dated '{rowInfo.levelDateText}'.");

            // First attempt: open delete modal and cancel
            var deleteButton = FindDeleteButtonInRow(rowInfo.row);
            CommonTestHelper.ClickElement(driver, deleteButton);
            driver.WaitForReady(5);
            Thread.Sleep(500);

            DismissDeleteModal(driver, confirm: false);
            _output.WriteLine("[INFO] Cancelled delete to verify modal behaviour.");
            driver.WaitForReady(5);
            Thread.Sleep(500);

            // Second attempt: confirm deletion
            deleteButton = FindDeleteButtonInRow(rowInfo.row);
            CommonTestHelper.ClickElement(driver, deleteButton);
            driver.WaitForReady(5);
            Thread.Sleep(500);

            DismissDeleteModal(driver, confirm: true);
            _output.WriteLine("[INFO] Confirmed delete from modal.");
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 1500);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Delete success toast was not displayed.");
            _output.WriteLine($"[INFO] Toast message: {toastMessage}");

            Assert.Contains("Level Deleted", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(rowInfo.levelDateText, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Delete toast displayed expected message.");
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
        /// Waits for the form heading to appear and be displayed//
        /// </summary>
        private IWebElement WaitForFormHeading(IPookieWebDriver driver, string expectedText = "Add New Level")
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
                        el.Text.Contains(expectedText, StringComparison.OrdinalIgnoreCase));

                if (heading != null)
                {
                    return heading;
                }

                Thread.Sleep(500);
            }

            throw new InvalidOperationException($"Form heading containing '{expectedText}' was not found or did not become visible within the expected time.");
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


        /// <summary>
        /// Selects a random option from the Level dropdown
        /// </summary>
        private void SelectRandomLevelOption(IPookieWebDriver driver)
        {
            var levelDropdown = driver.FindElements(By.CssSelector(
                    "select.form-control[id$='ddlLevel'], " +
                    "select.form-control[name$='ddlLevel'], " +
                    "select.form-control[name*='ddlLevel'], " +
                    "select.form-control[id*='ddlLevel']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Level dropdown was not found in the form.");

            var selectElement = new SelectElement(levelDropdown);
            var validOptions = selectElement.Options
                .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")))
                .ToList();

            if (!validOptions.Any())
            {
                throw new InvalidOperationException("No selectable Level options were found.");
            }

            var random = new Random();
            var randomOption = validOptions[random.Next(validOptions.Count)];
            selectElement.SelectByValue(randomOption.GetAttribute("value"));
            _output.WriteLine($"[INFO] Selected Level option: {randomOption.Text.Trim()}");

            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(250);
        }

        /// <summary>
        /// Selects an option from the Additional Case Weight dropdown (optionally excluding a specific value)
        /// </summary>
        private string SelectAdditionalCaseWeightOption(IPookieWebDriver driver, string? excludeValue = null)
        {
            var dropdown = driver.FindElements(By.CssSelector("select.form-control"))
                .FirstOrDefault(el => el.Displayed &&
                    el.FindElements(By.CssSelector("option[value='0.00']")).Any());

            if (dropdown == null)
            {
                _output.WriteLine("[WARN] Additional Case Weight dropdown was not found; skipping selection.");
                return string.Empty;
            }

            var selectElement = new SelectElement(dropdown);
            var validOptions = selectElement.Options
                .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) &&
                              !string.Equals(opt.GetAttribute("value"), excludeValue, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!validOptions.Any())
            {
                _output.WriteLine("[WARN] No valid Additional Case Weight options; skipping selection.");
                return string.Empty;
            }

            var random = new Random();
            var randomOption = validOptions[random.Next(validOptions.Count)];
            var selectedValue = randomOption.GetAttribute("value") ?? string.Empty;
            selectElement.SelectByValue(selectedValue);
            var selectedText = randomOption.Text?.Trim() ?? selectedValue;
            _output.WriteLine($"[INFO] Selected Additional Case Weight option: {selectedText} (value: {selectedValue})");

            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(250);

            return selectedValue;
        }

        /// <summary>
        /// Asserts that a validation message containing the provided text becomes visible
        /// </summary>
        private void AssertValidationMessageDisplayed(IPookieWebDriver driver, string expectedText, string fieldDescription)
        {
            var validationMessage = driver.FindElements(By.CssSelector(
                "span[style*='color:Red'][style*='display'], " +
                "span.text-danger, " +
                ".validation-summary, " +
                "div.alert.alert-danger"))
                .FirstOrDefault(el => el.Displayed &&
                    !string.IsNullOrWhiteSpace(el.Text) &&
                    el.Text.Contains(expectedText, StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(validationMessage);
            _output.WriteLine($"[PASS] Validation for {fieldDescription} displayed: {validationMessage.Text.Trim()}");
        }

        /// <summary>
        /// Finds the primary Submit button on the Level form
        /// </summary>
        private IWebElement FindSubmitButton(IPookieWebDriver driver)
        {
            return WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary[title*='Save'], " +
                "a.btn.btn-primary",
                "Submit button",
                15);
        }

        /// <summary>
        /// Finds the delete button within a grid row
        /// </summary>
        private IWebElement FindDeleteButtonInRow(IWebElement row)
        {
            return row.FindElements(By.CssSelector("a.btn.btn-danger"))
                .FirstOrDefault(btn => btn.Displayed &&
                    (btn.Text?.Contains("Delete", StringComparison.OrdinalIgnoreCase) ?? false) &&
                    btn.FindElements(By.CssSelector(".glyphicon-trash")).Any())
                ?? throw new InvalidOperationException("Delete button was not found in the row.");
        }

        /// <summary>
        /// Handles the delete confirmation modal by cancelling or confirming
        /// </summary>
        private void DismissDeleteModal(IPookieWebDriver driver, bool confirm)
        {
            var modal = driver.FindElements(By.CssSelector(".dc-confirmation-modal.modal"))
                .FirstOrDefault(m => m.Displayed)
                ?? throw new InvalidOperationException("Delete confirmation modal did not appear.");

            var buttonSelector = confirm
                ? "a.btn.btn-primary"
                : "button.btn.btn-default";

            var targetButton = modal.FindElements(By.CssSelector(buttonSelector))
                .FirstOrDefault(btn => btn.Displayed)
                ?? throw new InvalidOperationException("Expected button was not found inside the delete modal.");

            CommonTestHelper.ClickElement(driver, targetButton);
            driver.WaitForReady(5);
            Thread.Sleep(500);
        }

        /// <summary>
        /// Finds the first level row that contains an Edit button
        /// </summary>
        private (IWebElement row, IWebElement editButton, string levelText, string levelDateText, string caseWeightText) FindEditableLevelRow(IPookieWebDriver driver)
        {
            var grid = driver.WaitforElementToBeInDOM(By.CssSelector(
                "table.table, " +
                "table[id*='gvLevel'], " +
                "div.panel-body table"), 15)
                ?? throw new InvalidOperationException("Levels grid was not found on the page.");

            var rows = grid.FindElements(By.CssSelector("tr"))
                .Where(tr => tr.Displayed && tr.FindElements(By.CssSelector("td")).Any())
                .ToList();

            foreach (var row in rows)
            {
                var editButton = row.FindElements(By.CssSelector("a.btn.btn-default"))
                    .FirstOrDefault(btn => btn.Displayed &&
                        (btn.Text?.Contains("Edit", StringComparison.OrdinalIgnoreCase) ?? false) &&
                        btn.FindElements(By.CssSelector(".glyphicon-pencil")).Any());

                if (editButton == null)
                {
                    continue;
                }

                var displayedCells = row.FindElements(By.CssSelector("td"))
                    .Where(td => td.Displayed)
                    .ToList();

                if (displayedCells.Count < 4)
                {
                    continue;
                }

                var levelText = displayedCells.ElementAtOrDefault(1)?.Text?.Trim() ?? string.Empty;
                var levelDateText = displayedCells.ElementAtOrDefault(2)?.Text?.Trim() ?? string.Empty;
                var caseWeightText = displayedCells.ElementAtOrDefault(3)?.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(levelText) || string.IsNullOrWhiteSpace(levelDateText))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(caseWeightText))
                {
                    caseWeightText = "0";
                }

                return (row, editButton, levelText, levelDateText, caseWeightText);
            }

            throw new InvalidOperationException("No editable level row was found in the grid.");
        }

        /// <summary>
        /// Finds a level row in the grid matching the provided level and date text
        /// </summary>
        private IWebElement FindLevelRowByLevelAndDate(IPookieWebDriver driver, string levelText, string levelDateText)
        {
            var grid = driver.WaitforElementToBeInDOM(By.CssSelector(
                "table.table, " +
                "table[id*='gvLevel'], " +
                "div.panel-body table"), 15)
                ?? throw new InvalidOperationException("Levels grid was not found on the page.");

            var rows = grid.FindElements(By.CssSelector("tr"))
                .Where(tr => tr.Displayed && tr.FindElements(By.CssSelector("td")).Any())
                .ToList();

            foreach (var row in rows)
            {
                var rowText = row.Text ?? string.Empty;
                if (rowText.Contains(levelText, StringComparison.OrdinalIgnoreCase) &&
                    rowText.Contains(levelDateText, StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            throw new InvalidOperationException($"Unable to locate the level row for '{levelText}' dated '{levelDateText}'.");
        }

        /// <summary>
        /// Extracts the displayed Additional Case Weight text from a grid row
        /// </summary>
        private string ExtractAdditionalCaseWeight(IWebElement row)
        {
            var displayedCells = row.FindElements(By.CssSelector("td"))
                .Where(td => td.Displayed)
                .ToList();

            return displayedCells.ElementAtOrDefault(3)?.Text?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Normalizes case weight values to a consistent numeric string (e.g., 0.5 -> 0.50)
        /// </summary>
        private static string NormalizeCaseWeightValue(string? weightText)
        {
            if (string.IsNullOrWhiteSpace(weightText))
            {
                return "0.00";
            }

            if (double.TryParse(weightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value.ToString("0.00", CultureInfo.InvariantCulture);
            }

            if (double.TryParse(weightText, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return value.ToString("0.00", CultureInfo.InvariantCulture);
            }

            return weightText.Trim();
        }

        #endregion
    }
}

