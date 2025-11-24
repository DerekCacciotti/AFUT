using System;
using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.HITS
{
    public class HITSTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;
        private const string TargetPc1Id = "EC01001408989";

        public HITSTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void CheckingHITSFormValidationAndSubmission()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, TargetPc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            // Navigate to HITS
            NavigateToHITS(driver, formsPane);

            var pc1Display = CommonTestHelper.FindPc1Display(driver, TargetPc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on HITS page.");
            Assert.Contains(TargetPc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);

            // Click New HITS button
            CreateNewHITSEntry(driver);

            // Step 1: Click submit without filling anything - should get validation errors
            _output.WriteLine("[INFO] Step 1: Submitting form without any data...");
            var validationText = SubmitAndCaptureValidation(driver);
            Assert.False(string.IsNullOrWhiteSpace(validationText));
            Assert.Contains("Question #1 is required", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Cannot retrieve workers", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Question 8 is required if any of questions 4-7 are not answered", validationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Expected validation errors received: {validationText}");

            // Step 2: Fill screen date as 10/12/16
            _output.WriteLine("[INFO] Step 2: Entering date 10/12/16...");
            var dateInput = FindElementInModalOrPage(
                driver,
                "input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtHITSDate, input[id$='txtHITSDate']",
                "HITS Date input",
                10);
            SetInputValue(driver, dateInput, "10/12/16", "HITS Date", triggerBlur: true);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1000);

            // Step 3: Select worker 105
            _output.WriteLine("[INFO] Step 3: Selecting worker 105...");
            SelectWorker(driver, "105");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 4: Answer question 8 with "PC1 does not want to disclose" (before filling 4-7)
            _output.WriteLine("[INFO] Step 4: Answering question 8 with 'PC1 does not want to disclose'...");
            SelectHITSQuestion(driver, "ddlHITSNotDoneReason", "1. PC1 does not want to disclose", "01");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 5: Submit - should only have date validation error
            _output.WriteLine("[INFO] Step 5: Submitting with question 8 answered and invalid date...");
            validationText = SubmitAndCaptureValidation(driver);
            Assert.False(string.IsNullOrWhiteSpace(validationText));
            Assert.Contains("Question #1 must be after the case start date", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Question 8 is required", validationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Only date validation error remains: {validationText}");

            // Step 6: Clear question 8 (reset to --Select--)
            _output.WriteLine("[INFO] Step 6: Clearing question 8 (resetting to --Select--)...");
            SelectHITSQuestion(driver, "ddlHITSNotDoneReason", "--Select--", "");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 7: Answer question 4 with "Not at all"
            _output.WriteLine("[INFO] Step 7: Answering question 4 with 'Not at all'...");
            SelectHITSQuestion(driver, "ddlHITSHurt", "1. Not at all", "01");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 8: Submit - should get date validation error and Q8 error
            _output.WriteLine("[INFO] Step 8: Submitting with invalid date and Q4 answered...");
            validationText = SubmitAndCaptureValidation(driver);
            Assert.False(string.IsNullOrWhiteSpace(validationText));
            Assert.Contains("Question #1 must be after the case start date", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Question 8 is required if any of questions 4-7 are not answered", validationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Expected validation errors received: {validationText}");

            // Step 9: Change date to 10/26/25
            _output.WriteLine("[INFO] Step 9: Changing date to 10/26/25...");
            dateInput = FindElementInModalOrPage(
                driver,
                "input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtHITSDate, input[id$='txtHITSDate']",
                "HITS Date input",
                10);
            SetInputValue(driver, dateInput, "10/26/25", "HITS Date", triggerBlur: true);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1000);

            // Step 10: Submit - date validation error should be gone
            _output.WriteLine("[INFO] Step 10: Submitting with valid date...");
            validationText = SubmitAndCaptureValidation(driver);
            Assert.False(string.IsNullOrWhiteSpace(validationText));
            Assert.DoesNotContain("Question #1 must be after the case start date", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Question 8 is required if any of questions 4-7 are not answered", validationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Date validation error is gone. Remaining: {validationText}");

            // Step 11: Fill question 5 with "Not at all"
            _output.WriteLine("[INFO] Step 11: Answering question 5 with 'Not at all'...");
            SelectHITSQuestion(driver, "ddlHITSInsult", "1. Not at all", "01");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Verify score is still N/A
            var scoreText = GetHITSScore(driver);
            Assert.True(scoreText.Contains("N/A", StringComparison.OrdinalIgnoreCase), 
                $"Expected N/A score after question 5, but got: {scoreText}");
            _output.WriteLine($"[INFO] HITS Score after question 5: {scoreText}");

            // Submit - same validation should remain
            _output.WriteLine("[INFO] Submitting after question 5...");
            validationText = SubmitAndCaptureValidation(driver);
            Assert.Contains("Question 8 is required if any of questions 4-7 are not answered", validationText, StringComparison.OrdinalIgnoreCase);

            // Step 12: Fill question 6 with "Not at all"
            _output.WriteLine("[INFO] Step 12: Answering question 6 with 'Not at all'...");
            SelectHITSQuestion(driver, "ddlHITSThreaten", "1. Not at all", "01");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Verify score is still N/A
            scoreText = GetHITSScore(driver);
            Assert.True(scoreText.Contains("N/A", StringComparison.OrdinalIgnoreCase), 
                $"Expected N/A score after question 6, but got: {scoreText}");
            _output.WriteLine($"[INFO] HITS Score after question 6: {scoreText}");

            // Submit - same validation should remain
            _output.WriteLine("[INFO] Submitting after question 6...");
            validationText = SubmitAndCaptureValidation(driver);
            Assert.Contains("Question 8 is required if any of questions 4-7 are not answered", validationText, StringComparison.OrdinalIgnoreCase);

            // Step 13: Fill question 7 with "Not at all"
            _output.WriteLine("[INFO] Step 13: Answering question 7 with 'Not at all'...");
            SelectHITSQuestion(driver, "ddlHITSScream", "1. Not at all", "01");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 14: Verify HITS Score = 4, Result = Negative, Score Validity = Valid (after all questions 4-7 answered)
            _output.WriteLine("[INFO] Step 14: Verifying HITS calculations...");
            scoreText = GetHITSScore(driver);
            Assert.Contains("4", scoreText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] HITS Total Score: {scoreText}");

            var resultText = GetHITSResult(driver);
            Assert.Contains("Negative", resultText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] HITS Result: {resultText}");

            var validityText = GetHITSScoreValidity(driver);
            Assert.Contains("Valid", validityText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] HITS Score Validity: {validityText}");

            // Step 15: Submit - should succeed now
            _output.WriteLine("[INFO] Step 15: Final submission...");
            SubmitHITSForm(driver);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);

            // Verify success toast message
            _output.WriteLine("[INFO] Verifying success toast message...");
            var toastMessage = GetToastMessage(driver);
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("HITS", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("successfully saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Success toast: {toastMessage}");

            // Step 16: Verify form appears in the table
            _output.WriteLine("[INFO] Step 16: Verifying form appears in the table...");
            var tableRows = driver.FindElements(By.CssSelector("table#tblHITSs tbody tr"));
            Assert.NotEmpty(tableRows);

            var latestRow = tableRows.FirstOrDefault();
            Assert.NotNull(latestRow);

            var dateCell = latestRow.FindElement(By.CssSelector("td:nth-child(3)"));
            Assert.Contains("10/26/2025", dateCell.Text, StringComparison.OrdinalIgnoreCase);

            var scoreCell = latestRow.FindElement(By.CssSelector("td:nth-child(4)"));
            Assert.Contains("4", scoreCell.Text);

            var positiveCell = latestRow.FindElement(By.CssSelector("td:nth-child(5)"));
            Assert.Contains("False", positiveCell.Text, StringComparison.OrdinalIgnoreCase);

            var invalidCell = latestRow.FindElement(By.CssSelector("td:nth-child(6)"));
            Assert.Contains("False", invalidCell.Text, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine("[INFO] ✓ HITS form successfully created and verified in the table!");
        }

        [Fact]
        public void CheckingHITSFormEditAndUpdateToPositive()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, TargetPc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            // Navigate to HITS
            NavigateToHITS(driver, formsPane);

            var pc1Display = CommonTestHelper.FindPc1Display(driver, TargetPc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on HITS page.");
            Assert.Contains(TargetPc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);

            // Step 1: Click Edit button on the first HITS form in the table
            _output.WriteLine("[INFO] Step 1: Clicking Edit button on existing HITS form...");
            EditExistingHITSEntry(driver);

            var pc1DisplayOnEdit = CommonTestHelper.FindPc1Display(driver, TargetPc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1DisplayOnEdit), "Unable to locate PC1 ID on HITS edit page.");

            // Step 2: Change question 4 to "Frequently"
            _output.WriteLine("[INFO] Step 2: Changing question 4 to 'Frequently'...");
            SelectHITSQuestion(driver, "ddlHITSHurt", "5. Frequently", "05");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 3: Change question 5 to "Frequently"
            _output.WriteLine("[INFO] Step 3: Changing question 5 to 'Frequently'...");
            SelectHITSQuestion(driver, "ddlHITSInsult", "5. Frequently", "05");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 4: Change question 6 to "Frequently"
            _output.WriteLine("[INFO] Step 4: Changing question 6 to 'Frequently'...");
            SelectHITSQuestion(driver, "ddlHITSThreaten", "5. Frequently", "05");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 5: Change question 7 to "Frequently"
            _output.WriteLine("[INFO] Step 5: Changing question 7 to 'Frequently'...");
            SelectHITSQuestion(driver, "ddlHITSScream", "5. Frequently", "05");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Step 6: Verify HITS Score = 20, Result = Positive
            _output.WriteLine("[INFO] Step 6: Verifying updated HITS calculations...");
            var scoreText = GetHITSScore(driver);
            Assert.Contains("20", scoreText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] HITS Total Score: {scoreText}");

            var resultText = GetHITSResult(driver);
            Assert.Contains("Positive", resultText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] HITS Result: {resultText}");

            // Step 7: Submit the updated form
            _output.WriteLine("[INFO] Step 7: Submitting updated form...");
            SubmitHITSForm(driver);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);

            // Verify success toast message
            _output.WriteLine("[INFO] Verifying success toast message...");
            var toastMessage = GetToastMessage(driver);
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("HITS", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("successfully saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Success toast: {toastMessage}");

            // Step 8: Verify updated form in the table
            _output.WriteLine("[INFO] Step 8: Verifying updated form in the table...");
            var tableRows = driver.FindElements(By.CssSelector("table#tblHITSs tbody tr"));
            Assert.NotEmpty(tableRows);

            var updatedRow = tableRows.FirstOrDefault();
            Assert.NotNull(updatedRow);

            var scoreCell = updatedRow.FindElement(By.CssSelector("td:nth-child(4)"));
            Assert.Contains("20", scoreCell.Text);
            _output.WriteLine($"[INFO] Table Total Score: {scoreCell.Text}");

            var positiveCell = updatedRow.FindElement(By.CssSelector("td:nth-child(5)"));
            Assert.Contains("True", positiveCell.Text, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Table Positive?: {positiveCell.Text}");

            _output.WriteLine("[INFO] ✓ HITS form successfully edited and updated to Positive!");
        }

        #region Helper Methods

        private void NavigateToHITS(IPookieWebDriver driver, IWebElement formsPane)
        {
            var hitsLink = formsPane.FindElements(By.CssSelector("a#ctl00_ContentPlaceHolder1_ucForms_lnkHITS, a[data-formtype='hi'].moreInfo, a.list-group-item[href*='HITSs.aspx']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("HITS link was not found inside the Forms tab.");

            CommonTestHelper.ClickElement(driver, hitsLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
        }

        private void CreateNewHITSEntry(IPookieWebDriver driver)
        {
            var newHITSButton = driver.FindElements(By.CssSelector("a#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_lnkNewHITS.btn.btn-default.pull-right, a[id$='lnkNewHITS'].btn, a.btn[href*='HITS.aspx']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("New HITS button was not found on the HITS page.");

            CommonTestHelper.ClickElement(driver, newHITSButton);
            driver.WaitForReady(30);
            driver.WaitForUpdatePanel(30);
            Thread.Sleep(1000);
        }

        private void EditExistingHITSEntry(IPookieWebDriver driver)
        {
            // Wait for the table to be present
            Thread.Sleep(1000);
            
            // Find the table first
            var table = driver.FindElements(By.CssSelector("table#tblHITSs, table[id*='tblHITS']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("HITS table was not found on the page.");

            // Find the Edit button within the table - must have 'edit-HITS' class
            var editButton = table.FindElements(By.CssSelector("a.edit-HITS, a[id*='lnkEditHITS']"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Edit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Edit HITS button was not found in the HITS table.");

            _output.WriteLine($"[INFO] Found Edit button with href: {editButton.GetAttribute("href")}");
            
            CommonTestHelper.ClickElement(driver, editButton);
            driver.WaitForReady(30);
            driver.WaitForUpdatePanel(30);
            Thread.Sleep(1000);
        }

        private void SubmitHITSForm(IPookieWebDriver driver)
        {
            var submitButton = driver.FindElements(By.CssSelector("a#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_SubmitHITS_LoginView1_btnSubmit, a[id*='btnSubmit'].btn.btn-primary, a.btn.btn-primary[title*='Save']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Submit button was not found on the HITS form.");

            CommonTestHelper.ClickElement(driver, submitButton);
        }

        private string SubmitAndCaptureValidation(IPookieWebDriver driver)
        {
            SubmitHITSForm(driver);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1000);

            var validationSummary = driver.FindElements(By.CssSelector("div.validation-summary-errors, div[id*='ValidationSummary'], ul.validation-summary-errors, div.alert.alert-danger"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

            return validationSummary?.Text ?? string.Empty;
        }

        private IWebElement FindElementInModalOrPage(IPookieWebDriver driver, string selector, string elementName, int timeoutSeconds)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(drv =>
            {
                var elements = drv.FindElements(By.CssSelector(selector));
                return elements.FirstOrDefault(el => el.Displayed);
            }) ?? throw new InvalidOperationException($"{elementName} was not found or not displayed.");
        }

        private void SetInputValue(IPookieWebDriver driver, IWebElement inputElement, string value, string fieldName, bool triggerBlur = false)
        {
            inputElement.Clear();
            inputElement.SendKeys(value);
            
            if (triggerBlur)
            {
                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].blur();", inputElement);
                Thread.Sleep(300);
            }
            
            _output.WriteLine($"[INFO] Set {fieldName} to: {value}");
        }

        private void SelectWorker(IPookieWebDriver driver, string workerValue)
        {
            var workerDropdown = FindElementInModalOrPage(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlWorker, select[id$='ddlWorker']",
                "Worker dropdown",
                10);

            var selectElement = new SelectElement(workerDropdown);
            selectElement.SelectByValue(workerValue);
            _output.WriteLine($"[INFO] Selected worker: {workerValue}");
        }

        private void SelectHITSQuestion(IPookieWebDriver driver, string dropdownId, string optionText, string optionValue)
        {
            var dropdown = FindElementInModalOrPage(
                driver,
                $"select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_{dropdownId}, select[id$='{dropdownId}']",
                $"{dropdownId} dropdown",
                10);

            var selectElement = new SelectElement(dropdown);
            selectElement.SelectByValue(optionValue);
            _output.WriteLine($"[INFO] Selected {dropdownId}: {optionText}");
        }

        private string GetHITSScore(IPookieWebDriver driver)
        {
            var scoreElement = driver.FindElements(By.CssSelector("span#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_lblHITSScore, span[id$='lblHITSScore']"))
                .FirstOrDefault(el => el.Displayed);

            return scoreElement?.Text?.Trim() ?? "N/A";
        }

        private string GetHITSResult(IPookieWebDriver driver)
        {
            var resultElement = driver.FindElements(By.CssSelector("span#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_lblHITSResult, span[id$='lblHITSResult']"))
                .FirstOrDefault(el => el.Displayed);

            return resultElement?.Text?.Trim() ?? "N/A";
        }

        private string GetHITSScoreValidity(IPookieWebDriver driver)
        {
            var validityElement = driver.FindElements(By.CssSelector("span#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_lblHITSScoreValidity, span[id$='lblHITSScoreValidity']"))
                .FirstOrDefault(el => el.Displayed);

            return validityElement?.Text?.Trim() ?? string.Empty;
        }

        private string GetToastMessage(IPookieWebDriver driver)
        {
            Thread.Sleep(1000); // Wait for toast to appear
            
            var toastElements = driver.FindElements(By.CssSelector("div.jq-toast-single, div[class*='toast'], div.alert.alert-success"));
            var toastElement = toastElements.FirstOrDefault(el => !string.IsNullOrWhiteSpace(el.Text));

            return toastElement?.Text?.Trim() ?? string.Empty;
        }

        #endregion
    }
}

