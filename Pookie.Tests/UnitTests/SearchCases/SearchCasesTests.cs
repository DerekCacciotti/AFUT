using System;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using AFUT.Tests.Routine.SearchCases;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AFUT.Tests.UnitTests.SearchCases
{
    public class SearchCasesTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;

        public SearchCasesTests(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");
        }

        [Fact]
        public void Fill_All_Search_Fields_DisplaysMatchingResult()
        {
            using var driver = _driverFactory.CreateDriver();

            var routine = new SearchCasesSearchRoutine(driver, _config);
            var parameters = SearchCasesSearchRoutine.GetParameters();

            parameters.Criteria = new SearchCasesCriteria
            {
                Pc1Id = "AB12010361993",
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
            Assert.True(parameters.SearchMatched, "First search result PC1 ID did not match the searched PC1 ID.");
            Assert.True(string.Equals(parameters.Criteria.Pc1Id, parameters.FirstResultPc1Id, StringComparison.OrdinalIgnoreCase),
                $"Expected PC1 ID '{parameters.Criteria.Pc1Id}' but found '{parameters.FirstResultPc1Id}'.");
        }

        [Fact]
        public void Fill_All_Search_Fields_OpensMatchingCaseHome()
        {
            using var driver = _driverFactory.CreateDriver();

            var routine = new SearchCasesSearchRoutine(driver, _config);
            var parameters = SearchCasesSearchRoutine.GetParameters();

            parameters.Criteria = new SearchCasesCriteria
            {
                Pc1Id = "AB12010361993",
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
            Assert.True(caseHomePage.IsLoaded, "Case home page did not load successfully.");
            Assert.True(string.Equals(parameters.FirstResultPc1Id, caseHomePage.PC1Id, StringComparison.OrdinalIgnoreCase),
                $"Expected case home PC1 ID '{parameters.FirstResultPc1Id}' but found '{caseHomePage.PC1Id}'.");
            Assert.True(string.Equals(parameters.Criteria.Pc1Id, caseHomePage.PC1Id, StringComparison.OrdinalIgnoreCase),
                $"Expected case home PC1 ID '{parameters.Criteria.Pc1Id}' but found '{caseHomePage.PC1Id}'.");
        }

        [Fact]
        public void Search_With_No_Criteria_DisplaysNoRecordsFoundMessage()
        {
            using var driver = _driverFactory.CreateDriver();

            var routine = new SearchCasesSearchRoutine(driver, _config);
            var parameters = SearchCasesSearchRoutine.GetParameters();

            parameters.Criteria = new SearchCasesCriteria();

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
            Assert.Null(parameters.FirstResult);

            var searchCasesPage = parameters.SearchCasesPage
                                  ?? throw new InvalidOperationException("Search Cases page reference was not available after submitting search.");

            Assert.True(searchCasesPage.IsNoRecordsMessageDisplayed(), "Expected the 'No records found.' message to be displayed.");
        }
    }
}


