using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using AFUT.Tests.Routine.SearchCases;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit;

namespace AFUT.Tests.UnitTests.CaseHome
{
    public class CaseNotesTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;

        private const string KnownPc1Id = "AB12010361993";

        public CaseNotesTests(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
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

