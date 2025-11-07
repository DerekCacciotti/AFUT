using System;
using System.Collections.Generic;
using System.Linq;
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

        private const string KnownPc1Id = "AB12010361993";
        private const string KnownPc1FirstName = "Anonymized";
        private const string KnownPc1LastName = "Anonymized";
        private const string KnownTcDob = "060920";
        private const string KnownWorkerDisplayText = "3396, Worker";
        private const string KnownAlternateId = "Anonymized";

        public SearchCasesTests(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
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

        [Theory]
        [MemberData(nameof(SingleCriteriaSearchData))]
        public void Single_Field_Search_Returns_Expected_Case(SearchCasesCriteria criteria, bool expectsExactMatch, string scenario)
        {
            ExecuteSearchWithCriteria(criteria, parameters =>
            {
                Assert.True(parameters.SignedIn, $"User was not signed in when {scenario}.");
                Assert.True(parameters.RoleSelected, $"Role was not selected successfully when {scenario}.");
                Assert.True(parameters.SearchCasesPageLoaded, $"Search Cases page did not load when {scenario}.");
                Assert.True(parameters.SearchCompleted, $"Search did not complete when {scenario}.");

                var searchCasesPage = parameters.SearchCasesPage
                                      ?? throw new InvalidOperationException("Search Cases page reference was not available after submitting search.");

                Assert.False(searchCasesPage.IsNoRecordsMessageDisplayed(),
                    $"Expected search results when {scenario}, but the 'No records found.' message was displayed.");

                var results = searchCasesPage.GetResults().ToList();
                Assert.NotEmpty(results);

                if (expectsExactMatch && !string.IsNullOrWhiteSpace(parameters.Criteria?.Pc1Id))
                {
                    Assert.Contains(results, result =>
                        string.Equals(result.Pc1Id, parameters.Criteria.Pc1Id, StringComparison.OrdinalIgnoreCase));
                }
            });
        }

        [Fact]
        public void Cancel_Search_Returns_To_Home_Page()
        {
            using var driver = _driverFactory.CreateDriver();

            var routine = new SearchCasesSearchRoutine(driver, _config);
            var parameters = SearchCasesSearchRoutine.GetParameters();

            routine.LoadApplication(parameters);
            routine.EnsureSignedIn(parameters);
            routine.EnsureRoleSelected(parameters);
            routine.NavigateToSearchCases(parameters);

            Assert.True(parameters.SignedIn, "User was not signed in.");
            Assert.True(parameters.RoleSelected, "Role was not selected successfully.");
            Assert.True(parameters.SearchCasesPageLoaded, "Search Cases page did not load.");

            var searchCasesPage = parameters.SearchCasesPage
                                  ?? throw new InvalidOperationException("Search Cases page reference was not available before cancelling search.");

            searchCasesPage.CancelSearch();

            var homePage = new HomePage(driver);
            var currentUrl = driver.Url;

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after cancelling the search.");
            Assert.True(currentUrl.EndsWith("/Default.aspx", StringComparison.OrdinalIgnoreCase) ||
                        currentUrl.EndsWith("Default.aspx", StringComparison.OrdinalIgnoreCase),
                $"Expected to navigate to the home page after cancelling the search, but current URL is '{currentUrl}'.");
        }

        private void ExecuteSearchWithCriteria(SearchCasesCriteria criteria, Action<SearchCasesSearchRoutine.Params> assertion)
        {
            if (criteria is null)
            {
                throw new ArgumentNullException(nameof(criteria));
            }

            if (assertion is null)
            {
                throw new ArgumentNullException(nameof(assertion));
            }

            using var driver = _driverFactory.CreateDriver();

            var routine = new SearchCasesSearchRoutine(driver, _config);
            var parameters = SearchCasesSearchRoutine.GetParameters();
            parameters.Criteria = criteria;

            routine.LoadApplication(parameters);
            routine.EnsureSignedIn(parameters);
            routine.EnsureRoleSelected(parameters);
            routine.NavigateToSearchCases(parameters);
            routine.PopulateSearchCriteria(parameters);
            routine.SubmitSearch(parameters);

            assertion(parameters);
        }

        public static IEnumerable<object[]> SingleCriteriaSearchData()
        {
            yield return new object[]
            {
                new SearchCasesCriteria
                {
                    Pc1Id = KnownPc1Id,
                    WorkerDisplayText = KnownWorkerDisplayText
                },
                true,
                "searching by PC1 ID only"
            };

            yield return new object[]
            {
                new SearchCasesCriteria
                {
                    Pc1FirstName = KnownPc1FirstName,
                    WorkerDisplayText = KnownWorkerDisplayText
                },
                false,
                "searching by PC1 first name only"
            };

            yield return new object[]
            {
                new SearchCasesCriteria
                {
                    Pc1LastName = KnownPc1LastName,
                    WorkerDisplayText = KnownWorkerDisplayText
                },
                false,
                "searching by PC1 last name only"
            };

            yield return new object[]
            {
                new SearchCasesCriteria
                {
                    TcDob = KnownTcDob,
                    WorkerDisplayText = KnownWorkerDisplayText
                },
                false,
                "searching by TC date of birth only"
            };

            yield return new object[]
            {
                new SearchCasesCriteria
                {
                    WorkerDisplayText = KnownWorkerDisplayText
                },
                false,
                "searching by worker only"
            };

            yield return new object[]
            {
                new SearchCasesCriteria
                {
                    AlternateId = KnownAlternateId,
                    WorkerDisplayText = KnownWorkerDisplayText
                },
                false,
                "searching by alternate ID only"
            };
        }
    }
}


