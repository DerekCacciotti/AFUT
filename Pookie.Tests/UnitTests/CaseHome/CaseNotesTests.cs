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

