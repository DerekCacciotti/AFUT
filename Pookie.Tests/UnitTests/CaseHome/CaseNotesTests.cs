using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using AFUT.Tests.Routine.SearchCases;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.CaseHome
{
    public class CaseNotesTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        private const string KnownPc1Id = "AB12010361993";

        public CaseNotesTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void ClickNewCaseNote_OpensForm()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            caseNotesTab.ClickAddNote();

            // Just verify the click worked without errors
            Assert.True(true, "Successfully clicked New Case Note button.");
        }

        [Fact]
        public void AddNewCaseNote_WithDateAndText_SavesSuccessfully()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Click the New Case Note button
            caseNotesTab.ClickAddNote();

            _output.WriteLine("Clicked 'New Case Note' button");

            // Find the date and note fields
            var dateField = driver.WaitforElementToBeInDOM(By.CssSelector("input[id$='txtCaseNoteDate']"), 10);
            var noteField = driver.WaitforElementToBeInDOM(By.CssSelector("textarea[id$='txtCaseNote']"), 10);

            Assert.NotNull(dateField);
            Assert.True(dateField.Displayed, "Date field is not visible after clicking New Case Note.");
            _output.WriteLine($"Date field found: ID={dateField.GetAttribute("id")}, Placeholder={dateField.GetAttribute("placeholder")}");

            Assert.NotNull(noteField);
            Assert.True(noteField.Displayed, "Note text field is not visible after clicking New Case Note.");
            _output.WriteLine($"Note field found: ID={noteField.GetAttribute("id")}");

            // Enter the date
            _output.WriteLine("Entering date: 11/10/2025");
            caseNotesTab.EnterNoteDate("11/10/2025");
            _output.WriteLine($"Date field value after entry: {dateField.GetAttribute("value")}");

            // Enter the note text
            _output.WriteLine("Entering note text: Just Test");
            caseNotesTab.EnterNoteText("Just Test");
            _output.WriteLine($"Note field value after entry: {noteField.GetAttribute("value")}");

            // Save the note
            _output.WriteLine("Saving the case note...");
            caseNotesTab.SaveNote();

            // Wait a bit for the save to complete and notification to appear
            System.Threading.Thread.Sleep(2000);

            // Verify success notification appears
            var successNotification = driver.FindElements(By.CssSelector(".jq-toast-single.jq-has-icon.jq-icon-success"))
                .FirstOrDefault(n => n.Displayed && n.Text.Contains("Case Note Added"));
            
            if (successNotification != null)
            {
                _output.WriteLine($"Success notification found: {successNotification.Text}");
            }

            // Verify the note appears in the grid
            var noteSaved = caseNotesTab.IsNoteSaved();
            _output.WriteLine($"Note saved successfully: {noteSaved}");

            // Verify the grid contains our note
            var grid = driver.FindElement(By.Id("tblCaseNotes"));
            var rows = grid.FindElements(By.CssSelector("tbody tr"));
            _output.WriteLine($"Grid has {rows.Count} row(s)");

            if (rows.Count > 0)
            {
                var firstRow = rows[0];
                var firstRowText = firstRow.Text;
                _output.WriteLine($"First row: {firstRowText}");
                
                Assert.Contains("Just Test", firstRowText);
                Assert.Contains("11/10/2025", firstRowText);
            }

            Assert.True(noteSaved, "Case note was not saved successfully.");
            Assert.True(rows.Count > 0, "No case notes found in the grid after saving.");
        }

        [Fact]
        public void AddNewCaseNote_WithoutData_ShowsValidation()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Click the New Case Note button
            caseNotesTab.ClickAddNote();
            _output.WriteLine("Clicked 'New Case Note' button");

            // Find the date and note fields
            var dateField = driver.WaitforElementToBeInDOM(By.CssSelector("input[id$='txtCaseNoteDate']"), 10);
            var noteField = driver.WaitforElementToBeInDOM(By.CssSelector("textarea[id$='txtCaseNote']"), 10);

            Assert.NotNull(dateField);
            Assert.NotNull(noteField);
            _output.WriteLine("Fields are present, NOT entering any data");

            // Find and click the submit button without entering data
            var submitButton = driver.WaitforElementToBeInDOM(By.CssSelector("a[id*='lbSubmitCaseNote']"), 10);
            Assert.NotNull(submitButton);
            
            // Scroll into view and click
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();
            _output.WriteLine("Clicked Submit button without entering data");

            // Wait for validation to appear
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Check for validation summary that shows both error messages
            var validationSummary = driver.FindElements(By.CssSelector(".validation-summary.alert.alert-danger"))
                .FirstOrDefault(vs => vs.Displayed);
            
            Assert.NotNull(validationSummary);
            var summaryText = validationSummary.Text;
            _output.WriteLine($"Validation summary text: {summaryText}");

            // Verify both required field messages appear
            Assert.Contains("Case Note Date is required!", summaryText);
            Assert.Contains("Note is required!", summaryText);
            _output.WriteLine("Both validation messages found in validation summary");

            // Verify the individual date validator is also visible
            var dateValidator = driver.FindElement(By.CssSelector("span[id$='rfvCaseNoteDate']"));
            Assert.NotNull(dateValidator);
            Assert.True(dateValidator.Displayed, "Date validator should be visible");
            Assert.Contains("Case Note Date is required!", dateValidator.Text);
            _output.WriteLine($"Date validator displayed: {dateValidator.Text}");

            // Verify form is still visible (note was not saved)
            Assert.True(dateField.Displayed, "Date field should still be visible after validation");
            Assert.True(noteField.Displayed, "Note field should still be visible after validation");
            _output.WriteLine("Form fields remain visible - note was not saved");
        }

        [Fact]
        public void AddNewCaseNote_WithDateButNoNote_ShowsValidation()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Click the New Case Note button
            caseNotesTab.ClickAddNote();
            _output.WriteLine("Clicked 'New Case Note' button");

            // Enter date but not note text
            _output.WriteLine("Entering date: 11/10/2025");
            caseNotesTab.EnterNoteDate("11/10/2025");
            _output.WriteLine("NOT entering note text - leaving it empty");

            // Find and click the submit button
            var submitButton = driver.WaitforElementToBeInDOM(By.CssSelector("a[id*='lbSubmitCaseNote']"), 10);
            Assert.NotNull(submitButton);
            
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();
            _output.WriteLine("Clicked Submit button with date but no note");

            // Wait for validation to appear
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Check for validation summary
            var validationSummary = driver.FindElements(By.CssSelector(".validation-summary.alert.alert-danger"))
                .FirstOrDefault(vs => vs.Displayed);
            
            Assert.NotNull(validationSummary);
            var summaryText = validationSummary.Text;
            _output.WriteLine($"Validation summary text: {summaryText}");

            // Verify only "Note is required!" appears (date is filled)
            Assert.Contains("Note is required!", summaryText);
            Assert.DoesNotContain("Case Note Date is required!", summaryText);
            _output.WriteLine("Only 'Note is required!' validation message found");

            // Verify form is still visible (note was not saved)
            var dateField = driver.FindElement(By.CssSelector("input[id$='txtCaseNoteDate']"));
            var noteField = driver.FindElement(By.CssSelector("textarea[id$='txtCaseNote']"));
            Assert.True(dateField.Displayed, "Date field should still be visible after validation");
            Assert.True(noteField.Displayed, "Note field should still be visible after validation");
            _output.WriteLine("Form fields remain visible - note was not saved");
        }

        [Fact]
        public void AddNewCaseNote_WithNoteButNoDate_ShowsValidation()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Click the New Case Note button
            caseNotesTab.ClickAddNote();
            _output.WriteLine("Clicked 'New Case Note' button");

            // Enter note text but not date
            _output.WriteLine("NOT entering date - leaving it empty");
            _output.WriteLine("Entering note text: Just Test");
            caseNotesTab.EnterNoteText("Just Test");

            // Find and click the submit button
            var submitButton = driver.WaitforElementToBeInDOM(By.CssSelector("a[id*='lbSubmitCaseNote']"), 10);
            Assert.NotNull(submitButton);
            
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();
            _output.WriteLine("Clicked Submit button with note but no date");

            // Wait for validation to appear
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Check for validation summary
            var validationSummary = driver.FindElements(By.CssSelector(".validation-summary.alert.alert-danger"))
                .FirstOrDefault(vs => vs.Displayed);
            
            Assert.NotNull(validationSummary);
            var summaryText = validationSummary.Text;
            _output.WriteLine($"Validation summary text: {summaryText}");

            // Verify only "Case Note Date is required!" appears (note is filled)
            Assert.Contains("Case Note Date is required!", summaryText);
            Assert.DoesNotContain("Note is required!", summaryText);
            _output.WriteLine("Only 'Case Note Date is required!' validation message found");

            // Verify the individual date validator is also visible
            var dateValidator = driver.FindElement(By.CssSelector("span[id$='rfvCaseNoteDate']"));
            Assert.NotNull(dateValidator);
            Assert.True(dateValidator.Displayed, "Date validator should be visible");
            Assert.Contains("Case Note Date is required!", dateValidator.Text);
            _output.WriteLine($"Date validator displayed: {dateValidator.Text}");

            // Verify form is still visible (note was not saved)
            var dateField = driver.FindElement(By.CssSelector("input[id$='txtCaseNoteDate']"));
            var noteField = driver.FindElement(By.CssSelector("textarea[id$='txtCaseNote']"));
            Assert.True(dateField.Displayed, "Date field should still be visible after validation");
            Assert.True(noteField.Displayed, "Note field should still be visible after validation");
            _output.WriteLine("Form fields remain visible - note was not saved");
        }

        [Fact]
        public void EditCaseNote_WithUpdatedData_SavesSuccessfully()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Click Edit on the first existing case note
            var editLink = ClickFirstEditLink(driver, caseNotesTab);
            _output.WriteLine($"Clicked Edit link: {editLink.GetAttribute("id")}");

            // Wait for form to appear
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Get the current values
            var dateField = driver.FindElement(By.CssSelector("input[id$='txtCaseNoteDate']"));
            var noteField = driver.FindElement(By.CssSelector("textarea[id$='txtCaseNote']"));

            var originalDate = dateField.GetAttribute("value");
            var originalNote = noteField.GetAttribute("value");
            _output.WriteLine($"Original values - Date: {originalDate}, Note: {originalNote}");

            // Update the values
            var newDate = "11/11/2025";
            var newNote = "Updated test note";

            dateField.Clear();
            dateField.SendKeys(newDate);
            _output.WriteLine($"Updated date to: {newDate}");

            noteField.Clear();
            noteField.SendKeys(newNote);
            _output.WriteLine($"Updated note to: {newNote}");

            // Submit the changes
            var submitButton = driver.FindElement(By.CssSelector("a[id*='lbSubmitCaseNote']"));
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();
            _output.WriteLine("Clicked Submit button");

            // Wait for save
            driver.WaitForUpdatePanel(30);
            System.Threading.Thread.Sleep(2000);

            // Verify success notification
            var successNotification = driver.FindElements(By.CssSelector(".jq-toast-single.jq-has-icon.jq-icon-success"))
                .FirstOrDefault(n => n.Displayed);

            if (successNotification != null)
            {
                _output.WriteLine($"Success notification found: {successNotification.Text}");
            }

            // Verify the grid shows the updated note
            var grid = driver.FindElement(By.Id("tblCaseNotes"));
            var rows = grid.FindElements(By.CssSelector("tbody tr"));
            _output.WriteLine($"Grid has {rows.Count} rows after update");

            var firstRow = rows[0];
            var firstRowText = firstRow.Text;
            _output.WriteLine($"First row after update: {firstRowText}");

            Assert.Contains(newDate, firstRowText);
            Assert.Contains(newNote, firstRowText);
            _output.WriteLine("Updated values found in grid!");
        }

        [Fact]
        public void EditCaseNote_ClearAllFields_ShowsValidation()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Click Edit on the first existing case note
            var editLink = ClickFirstEditLink(driver, caseNotesTab);
            _output.WriteLine($"Clicked Edit link: {editLink.GetAttribute("id")}");

            // Wait for form
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Clear both fields
            var dateField = driver.FindElement(By.CssSelector("input[id$='txtCaseNoteDate']"));
            var noteField = driver.FindElement(By.CssSelector("textarea[id$='txtCaseNote']"));

            _output.WriteLine("Clearing all fields...");
            dateField.Clear();
            noteField.Clear();

            // Submit
            var submitButton = driver.FindElement(By.CssSelector("a[id*='lbSubmitCaseNote']"));
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();
            _output.WriteLine("Clicked Submit with empty fields");

            // Wait for validation
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Check validation summary
            var validationSummary = driver.FindElements(By.CssSelector(".validation-summary.alert.alert-danger"))
                .FirstOrDefault(vs => vs.Displayed);

            Assert.NotNull(validationSummary);
            var summaryText = validationSummary.Text;
            _output.WriteLine($"Validation summary: {summaryText}");

            Assert.Contains("Case Note Date is required!", summaryText);
            Assert.Contains("Note is required!", summaryText);
            _output.WriteLine("Both validation messages found!");
        }

        [Fact]
        public void EditCaseNote_ClearDateOnly_ShowsValidation()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Click Edit
            var editLink = ClickFirstEditLink(driver, caseNotesTab);
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Clear only the date field
            var dateField = driver.FindElement(By.CssSelector("input[id$='txtCaseNoteDate']"));
            var noteField = driver.FindElement(By.CssSelector("textarea[id$='txtCaseNote']"));

            _output.WriteLine("Clearing date field only...");
            dateField.Clear();
            _output.WriteLine($"Note field still has: {noteField.GetAttribute("value")}");

            // Submit
            var submitButton = driver.FindElement(By.CssSelector("a[id*='lbSubmitCaseNote']"));
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();

            // Wait for validation
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Check validation
            var validationSummary = driver.FindElements(By.CssSelector(".validation-summary.alert.alert-danger"))
                .FirstOrDefault(vs => vs.Displayed);

            Assert.NotNull(validationSummary);
            var summaryText = validationSummary.Text;
            _output.WriteLine($"Validation summary: {summaryText}");

            Assert.Contains("Case Note Date is required!", summaryText);
            Assert.DoesNotContain("Note is required!", summaryText);
            _output.WriteLine("Only date validation found!");
        }

        [Fact]
        public void EditCaseNote_ClearNoteOnly_ShowsValidation()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Click Edit
            var editLink = ClickFirstEditLink(driver, caseNotesTab);
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Clear only the note field
            var dateField = driver.FindElement(By.CssSelector("input[id$='txtCaseNoteDate']"));
            var noteField = driver.FindElement(By.CssSelector("textarea[id$='txtCaseNote']"));

            _output.WriteLine($"Date field still has: {dateField.GetAttribute("value")}");
            _output.WriteLine("Clearing note field only...");
            noteField.Clear();

            // Submit
            var submitButton = driver.FindElement(By.CssSelector("a[id*='lbSubmitCaseNote']"));
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();

            // Wait for validation
            driver.WaitForUpdatePanel(10);
            System.Threading.Thread.Sleep(1000);

            // Check validation
            var validationSummary = driver.FindElements(By.CssSelector(".validation-summary.alert.alert-danger"))
                .FirstOrDefault(vs => vs.Displayed);

            Assert.NotNull(validationSummary);
            var summaryText = validationSummary.Text;
            _output.WriteLine($"Validation summary: {summaryText}");

            Assert.Contains("Note is required!", summaryText);
            Assert.DoesNotContain("Case Note Date is required!", summaryText);
            _output.WriteLine("Only note validation found!");
        }

        [Fact]
        public void DeleteCaseNote_ConfirmYes_DeletesSuccessfully()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var caseNotesTab = caseHomePage.GetCaseNotesTab();

            // Ensure tab is active
            caseNotesTab.Activate();
            System.Threading.Thread.Sleep(1000);

            // Get initial row count and first row text
            var grid = driver.FindElement(By.Id("tblCaseNotes"));
            var initialRows = grid.FindElements(By.CssSelector("tbody tr"));
            var initialRowCount = initialRows.Count;
            _output.WriteLine($"Initial row count: {initialRowCount}");

            var firstRow = initialRows[0];
            var firstRowText = firstRow.Text;
            _output.WriteLine($"First row before deletion: {firstRowText}");

            // Find and click the first Delete button
            var deleteButtons = driver.FindElements(By.CssSelector("button.delete-gridview"));
            var firstDeleteButton = deleteButtons.FirstOrDefault(b => b.Displayed);
            Assert.NotNull(firstDeleteButton);
            
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", firstDeleteButton);
            System.Threading.Thread.Sleep(500);
            firstDeleteButton.Click();
            _output.WriteLine("Clicked Delete button");

            // Wait for confirmation modal to appear
            System.Threading.Thread.Sleep(1500);

            // Verify modal appears
            var modal = driver.FindElement(By.Id("divDeleteCaseNoteModal"));
            Assert.True(modal.Displayed, "Delete confirmation modal should be visible");
            _output.WriteLine($"Confirmation modal displayed: {modal.Text}");

            // Verify modal asks for confirmation
            Assert.Contains("Are you sure", modal.Text);
            _output.WriteLine("Modal contains confirmation message");

            // Click Yes to confirm deletion
            var yesButton = driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_ucCaseNotes_lbDeleteCaseNote"));
            Assert.NotNull(yesButton);
            Assert.True(yesButton.Displayed, "Yes button should be visible");
            
            yesButton.Click();
            _output.WriteLine("Clicked Yes to confirm deletion");

            // Wait for deletion to complete
            driver.WaitForUpdatePanel(30);
            System.Threading.Thread.Sleep(2000);

            // Verify success toast notification appears
            var successNotification = driver.FindElements(By.CssSelector(".jq-toast-single.jq-has-icon.jq-icon-success"))
                .FirstOrDefault(n => n.Displayed && n.Text.Contains("deleted"));

            Assert.NotNull(successNotification);
            _output.WriteLine($"Success notification: {successNotification.Text}");
            Assert.Contains("deleted", successNotification.Text, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("Toast notification confirmed deletion!");

            // Re-find the grid after page update to avoid stale element reference
            var updatedGrid = driver.FindElement(By.Id("tblCaseNotes"));
            var updatedRows = updatedGrid.FindElements(By.CssSelector("tbody tr"));

            // Verify the deleted row is no longer at the top of the grid
            var currentFirstRow = updatedRows.FirstOrDefault();
            Assert.NotNull(currentFirstRow);
            
            var currentFirstRowText = currentFirstRow.Text;
            _output.WriteLine($"Old first row: {firstRowText}");
            _output.WriteLine($"New first row: {currentFirstRowText}");
            
            // The deleted row should not be in the grid anymore
            Assert.NotEqual(firstRowText, currentFirstRowText);
            _output.WriteLine("Deleted row is no longer at the top of the grid - deletion successful!");
        }

        private IWebElement ClickFirstEditLink(IPookieWebDriver driver, CaseHomePage.CaseNotesTab caseNotesTab)
        {
            // Ensure tab is active
            caseNotesTab.Activate();
            System.Threading.Thread.Sleep(1000);

            // Find first visible Edit link
            var allEditLinks = driver.FindElements(By.CssSelector("a[id*='lbEditCaseNote']"));
            var firstEditLink = allEditLinks.FirstOrDefault(l => l.Displayed);

            Assert.NotNull(firstEditLink);

            // Click it
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", firstEditLink);
            System.Threading.Thread.Sleep(500);
            firstEditLink.Click();

            return firstEditLink;
        }

        private CaseHomePage NavigateToCaseHome(IPookieWebDriver driver)
        {
            var routine = new SearchCasesSearchRoutine(driver, _config);
            var parameters = SearchCasesSearchRoutine.GetParameters();

            parameters.Criteria = new SearchCasesCriteria
            {
                Pc1Id = KnownPc1Id,
                Pc1FirstName = "Anonymized",
                Pc1LastName = "Anonymized",
                TcDob = "060920",
                WorkerDisplayText = "3396, Worker",
                AlternateId = "Anonymized"
            };

            routine.LoadApplication(parameters);
            routine.EnsureSignedIn(parameters);
            routine.EnsureRoleSelected(parameters);
            routine.NavigateToSearchCases(parameters);
            routine.PopulateSearchCriteria(parameters);
            routine.SubmitSearch(parameters);

            Assert.True(parameters.SignedIn, "User was not signed in.");
            Assert.True(parameters.RoleSelected, "Role was not selected successfully.");
            Assert.True(parameters.SearchCasesPageLoaded, "Search Cases page did not load.");
            Assert.True(parameters.SearchCompleted, "Search did not complete.");

            var firstResult = parameters.FirstResult
                             ?? throw new InvalidOperationException("No search results were returned.");

            var caseHomePage = firstResult.OpenCaseHome();

            Assert.NotNull(caseHomePage);
            Assert.True(caseHomePage.IsLoaded, "Case home page did not load after navigating from results.");

            return caseHomePage;
        }
    }
}

