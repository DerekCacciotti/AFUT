using System;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using AFUT.Tests.Routine.SearchCases;

namespace AFUT.Tests.UnitTests.CaseHome
{
    internal static class CaseHomeTestHelper
    {
        internal static CaseHomePage NavigateToCaseHome(IPookieWebDriver driver, AppConfig config)
        {
            if (driver is null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var routine = new SearchCasesSearchRoutine(driver, config);
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
            Assert.True(caseHomePage.IsLoaded, "Case home page did not load after navigating from results.");

            return caseHomePage;
        }
    }
}

