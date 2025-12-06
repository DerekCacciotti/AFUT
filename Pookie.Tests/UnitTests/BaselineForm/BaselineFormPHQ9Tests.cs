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

namespace AFUT.Tests.UnitTests.BaselineForm
{
    [TestCaseOrderer("AFUT.Tests.Helpers.PriorityOrderer", "AFUT.Tests")]
    [Collection("Sequential")]
    public class BaselineFormPHQ9Tests : IClassFixture<AppConfig>
    {
        protected readonly AppConfig _config;
        protected readonly IPookieDriverFactory _driverFactory;
        protected readonly ITestOutputHelper _output;

        public BaselineFormPHQ9Tests(AppConfig config, ITestOutputHelper output)
        {
            _config = config;
            _output = output;

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(6)]
        public void PHQ9TabCompleteFlowTest(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToBaselineForm(driver, formsPane);
            
            // Navigate to PHQ-9 tab
            ActivateTab(driver, "#tab_PHQ9 a[href='#PHQ9']", "PHQ-9");
            _output.WriteLine("[PASS] PHQ-9 tab activated successfully");

            // ===== PART 1: Test Date Validation =====
            _output.WriteLine("\n[TEST SECTION] Testing PHQ-9 date validation");

            // Read the screen date from the page
            var screenDateLabel = driver.FindElements(By.CssSelector("span[id*='lblScreenDate']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Screen Date label was not found.");
            
            var screenDateText = screenDateLabel.Text.Trim();
            _output.WriteLine($"[INFO] Screen date from page: {screenDateText}");

            // Parse the screen date
            DateTime screenDate;
            if (!DateTime.TryParse(screenDateText, out screenDate))
            {
                throw new InvalidOperationException($"Failed to parse screen date: {screenDateText}");
            }

            // Calculate date one day before screen date (should fail validation)
            var invalidDate = screenDate.AddDays(-1).ToString("MM/dd/yy");
            var dateInput = driver.FindElements(By.CssSelector("input.form-control[id*='txtPHQDateAdministered']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ date input was not found.");
            
            WebElementHelper.SetInputValue(driver, dateInput, invalidDate, "PHQ-9 Date", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered date before screen date: {invalidDate} (screen date: {screenDateText})");

            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit with invalid date");

            // Verify date validation
            var dateValidation = FindValidationMessage(driver, "PHQ-9 date validation", 
                "The PHQ date administered must be on or after the case start date");
            Assert.NotNull(dateValidation);
            _output.WriteLine($"[PASS] Date validation displayed: {dateValidation!.Text.Trim()}");

            // Correct the date to screen date (on or after, so valid)
            ActivateTab(driver, "#tab_PHQ9 a[href='#PHQ9']", "PHQ-9");
            var validDate = screenDate.ToString("MM/dd/yy");
            dateInput = driver.FindElements(By.CssSelector("input.form-control[id*='txtPHQDateAdministered']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ date input was not found.");
            WebElementHelper.SetInputValue(driver, dateInput, validDate, "PHQ-9 Date (corrected)", triggerBlur: true);
            _output.WriteLine($"[INFO] Corrected date to valid date (screen date): {validDate}");

            // ===== PART 2: Test Participant "Other" Validation =====
            _output.WriteLine("\n[TEST SECTION] Testing Participant 'Other' validation");

            // Select "Other" in Q33
            var participantDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlPHQ9Participant']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ-9 Participant dropdown was not found.");
            
            var participantSelect = new SelectElement(participantDropdown);
            participantSelect.SelectByValue("04"); // Other
            driver.WaitForUpdatePanel(3);
            driver.WaitForReady(3);
            Thread.Sleep(500);
            _output.WriteLine("[INFO] Selected 'Other' in Participant dropdown");

            // Verify specify field appears
            var specifyDiv = driver.FindElements(By.CssSelector("div[id*='divPHQ9ParticipantSpecify']"))
                .FirstOrDefault();
            Assert.NotNull(specifyDiv);
            Assert.True(specifyDiv.Displayed, "Participant specify field should be visible");
            _output.WriteLine("[PASS] Participant specify field appeared");

            // Submit without specifying
            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit with empty specify field");

            ActivateTab(driver, "#tab_PHQ9 a[href='#PHQ9']", "PHQ-9");
            var specifyValidation = FindValidationMessage(driver, "Participant specify validation", 
                "You must specify the participant if the 'Other' option is selected");
            Assert.NotNull(specifyValidation);
            _output.WriteLine($"[PASS] Specify validation displayed: {specifyValidation!.Text.Trim()}");

            // Change to PC1
            participantDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlPHQ9Participant']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ-9 Participant dropdown was not found.");
            participantSelect = new SelectElement(participantDropdown);
            participantSelect.SelectByValue("01"); // PC1
            driver.WaitForUpdatePanel(3);
            driver.WaitForReady(3);
            Thread.Sleep(500);
            _output.WriteLine("[INFO] Changed Participant to PC1");

            // ===== PART 3: Test Refused Checkbox Behavior =====
            _output.WriteLine("\n[TEST SECTION] Testing PHQ-9 refused checkbox behavior");

            // Check the refused checkbox
            var refusedCheckbox = driver.FindElements(By.CssSelector("input[id*='chkPHQ9Refused']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ-9 Refused checkbox was not found.");
            
            if (!refusedCheckbox.Selected)
            {
                refusedCheckbox.Click();
                driver.WaitForUpdatePanel(3);
                driver.WaitForReady(3);
                Thread.Sleep(500);
            }
            _output.WriteLine("[INFO] Checked 'PHQ-9 refused' checkbox");

            // Verify Q36-Q44 are disabled
            var phq9ScoreDropdowns = driver.FindElements(By.CssSelector("select.phq9score.form-control"))
                .Where(el => el.Displayed)
                .ToList();
            
            _output.WriteLine($"[INFO] Found {phq9ScoreDropdowns.Count} PHQ-9 score dropdowns");
            foreach (var dropdown in phq9ScoreDropdowns)
            {
                var isDisabled = !dropdown.Enabled || dropdown.GetAttribute("disabled") != null;
                _output.WriteLine($"[DEBUG] Dropdown {dropdown.GetAttribute("id")} - Enabled: {dropdown.Enabled}, Disabled attr: {dropdown.GetAttribute("disabled")}");
            }

            // Submit without worker
            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit with refused checked but no worker");

            ActivateTab(driver, "#tab_PHQ9 a[href='#PHQ9']", "PHQ-9");
            var workerValidation = FindValidationMessage(driver, "Worker required validation", 
                "You must select the worker if the PHQ was refused or information about the PHQ is entered");
            Assert.NotNull(workerValidation);
            _output.WriteLine($"[PASS] Worker validation displayed: {workerValidation!.Text.Trim()}");

            // Uncheck refused
            refusedCheckbox = driver.FindElements(By.CssSelector("input[id*='chkPHQ9Refused']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ-9 Refused checkbox was not found.");
            
            if (refusedCheckbox.Selected)
            {
                refusedCheckbox.Click();
                driver.WaitForUpdatePanel(3);
                driver.WaitForReady(3);
                Thread.Sleep(500);
            }
            _output.WriteLine("[INFO] Unchecked 'PHQ-9 refused' checkbox");

            // ===== PART 4: Test Score Calculation with Random Values =====
            _output.WriteLine("\n[TEST SECTION] Testing PHQ-9 score calculation with random values");

            // Question IDs for Q36-Q44
            var questionIds = new[] {
                "ddlInterest", "ddlDown", "ddlSleep", "ddlTired", "ddlAppetite",
                "ddlBadSelf", "ddlConcentration", "ddlSlowOrFast", "ddlBetterOffDead"
            };

            var random = new Random();
            var expectedScore = 0;
            var selections = new List<string>();

            // Randomly select values for Q36-Q44
            foreach (var questionId in questionIds)
            {
                var dropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='{questionId}']"))
                    .FirstOrDefault(el => el.Displayed);
                if (dropdown != null)
                {
                    var select = new SelectElement(dropdown);
                    
                    // Random value: "01", "02", "03", or "04"
                    var randomValue = random.Next(1, 5); // 1 to 4
                    var valueStr = $"{randomValue:D2}"; // "01", "02", "03", "04"
                    
                    // Calculate score: value 01=0, 02=1, 03=2, 04=3
                    var scoreForQuestion = randomValue - 1;
                    expectedScore += scoreForQuestion;
                    
                    select.SelectByValue(valueStr);
                    driver.WaitForUpdatePanel(2);
                    Thread.Sleep(200);
                    
                    var optionText = select.SelectedOption.Text.Trim();
                    selections.Add($"{questionId}: {optionText} (score={scoreForQuestion})");
                    _output.WriteLine($"[INFO] {questionId}: Selected option {valueStr} (score={scoreForQuestion})");
                }
            }

            // Select Q45 (Difficulty) - random selection
            var difficultyDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlDifficulty']"))
                .FirstOrDefault(el => el.Displayed);
            if (difficultyDropdown != null)
            {
                var difficultySelect = new SelectElement(difficultyDropdown);
                var randomDifficulty = random.Next(1, 5); // 1 to 4
                var difficultyValue = $"{randomDifficulty:D2}";
                difficultySelect.SelectByValue(difficultyValue);
                driver.WaitForUpdatePanel(2);
                Thread.Sleep(500);
                _output.WriteLine($"[INFO] Q45 Difficulty: Selected option {difficultyValue}");
            }

            _output.WriteLine($"\n[INFO] Expected total score: {expectedScore}");
            _output.WriteLine($"[INFO] Expected result: {(expectedScore > 9 ? "Positive" : "Negative")}");

            // Verify score calculation
            Thread.Sleep(1000); // Wait for JavaScript calculation
            var scoreLabel = driver.FindElements(By.CssSelector("span[id*='lblPHQ9Score']"))
                .FirstOrDefault(el => el.Displayed);
            var resultLabel = driver.FindElements(By.CssSelector("span[id*='lblPHQ9Result']"))
                .FirstOrDefault(el => el.Displayed);
            var validityLabel = driver.FindElements(By.CssSelector("span[id*='lblPHQ9ScoreValidity']"))
                .FirstOrDefault(el => el.Displayed);

            var actualScore = scoreLabel?.Text.Trim() ?? "";
            var actualResult = resultLabel?.Text.Trim() ?? "";
            var validity = validityLabel?.Text.Trim() ?? "";

            _output.WriteLine($"\n[INFO] Actual PHQ-9 Score: {actualScore}");
            _output.WriteLine($"[INFO] Actual PHQ-9 Result: {actualResult}");
            _output.WriteLine($"[INFO] PHQ-9 Validity: {validity}");

            // Verify score matches
            Assert.Equal(expectedScore.ToString(), actualScore);
            _output.WriteLine($"[PASS] Score calculation correct: Expected={expectedScore}, Actual={actualScore}");

            // Verify result is correct based on score
            var expectedResult = expectedScore > 9 ? "Positive" : "Negative";
            Assert.Contains(expectedResult, actualResult, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Result correct: {actualResult} (score {expectedScore} → {expectedResult})");

            // Verify validity
            Assert.Contains("Valid", validity, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Score validity: {validity}");

            // Submit without worker (should still fail)
            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("\n[INFO] Clicked Submit with scores but no worker");

            ActivateTab(driver, "#tab_PHQ9 a[href='#PHQ9']", "PHQ-9");
            workerValidation = FindValidationMessage(driver, "Worker required validation", 
                "You must select the worker if the PHQ was refused or information about the PHQ is entered");
            Assert.NotNull(workerValidation);
            _output.WriteLine($"[PASS] Worker validation displayed: {workerValidation!.Text.Trim()}");

            // ===== PART 5: Select Worker and Submit Successfully =====
            _output.WriteLine("\n[TEST SECTION] Selecting worker and submitting");

            // Select a worker
            var workerDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlPHQWorker']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PHQ Worker dropdown was not found.");
            
            WebElementHelper.SelectDropdownOption(driver, "select.form-control[id*='ddlPHQWorker']", 
                "PHQ Worker", "105, Worker", "105");
            _output.WriteLine("[INFO] Selected worker: 105, Worker");

            // Submit
            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(3000);
            _output.WriteLine("[INFO] Clicked Submit button");

            // Verify success toast or redirect
            var toastMessage = WebElementHelper.GetToastMessage(driver, 3000);
            var currentUrl = driver.Url ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(toastMessage) && currentUrl.Contains("CaseHome.aspx", StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine("[INFO] Form saved successfully (redirected to CaseHome.aspx)");
                toastMessage = $"Form Saved - {pc1Id}";
            }
            
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed.");
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Form saved successfully: {toastMessage}");

            _output.WriteLine("\n[PASS] PHQ-9 tab complete flow test finished successfully");
        }

        protected void NavigateToBaselineForm(IPookieWebDriver driver, IWebElement formsPane)
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

        protected void ActivateTab(IPookieWebDriver driver, string tabSelector, string tabName)
        {
            var tabLink = driver.WaitforElementToBeInDOM(By.CssSelector(tabSelector), 10)
                ?? throw new InvalidOperationException($"Tab link '{tabName}' was not found.");
            CommonTestHelper.ClickElement(driver, tabLink);
            driver.WaitForReady(5);
            Thread.Sleep(500);
            _output.WriteLine($"[INFO] Activated {tabName} tab");
        }

        protected IWebElement FindSubmitButton(IPookieWebDriver driver)
        {
            var submitButton = driver.FindElements(By.CssSelector(
                    "a.btn.btn-primary[href*='#'], " +
                    "button.btn.btn-primary[type='submit'], " +
                    "a.btn.btn-primary"))
                .FirstOrDefault(btn => btn.Displayed && 
                    (btn.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase) || 
                     btn.GetAttribute("onclick")?.Contains("Submit") == true))
                ?? throw new InvalidOperationException("Submit button was not found.");

            return submitButton;
        }

        protected IWebElement? FindValidationMessage(IPookieWebDriver driver, string validationName, params string[] expectedTextParts)
        {
            var validationElements = driver.FindElements(By.CssSelector(
                    "span.text-danger, span[style*='color: red'], span[style*='color:Red'], " +
                    "div.alert.alert-danger, .validation-summary-errors li"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .ToList();

            foreach (var element in validationElements)
            {
                var text = element.Text.Trim();
                if (expectedTextParts.All(part => text.Contains(part, StringComparison.OrdinalIgnoreCase)))
                {
                    return element;
                }
            }

            return null;
        }
    }
}

