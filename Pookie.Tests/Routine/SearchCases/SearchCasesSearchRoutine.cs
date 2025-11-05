using System;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using OpenQA.Selenium;

namespace AFUT.Tests.Routine.SearchCases
{
    [Routine(Name = "Search Cases - Fill All Fields")]
    public class SearchCasesSearchRoutine
    {
        private readonly IPookieWebDriver _driver;
        private readonly IAppConfig _config;

        public static Params GetParameters()
        {
            var parameters = SetUp.For(new Params());
            parameters.Criteria ??= new SearchCasesCriteria();

            return parameters;
        }

        public SearchCasesSearchRoutine(IPookieWebDriver driver, IAppConfig config)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        [RoutineStep(1, "Load application")]
        public void LoadApplication(Params parms)
        {
            parms.App = EntryPoint.OpenPage(_driver, _config);
        }

        [RoutineStep(2, "Sign in if required")]
        public void EnsureSignedIn(Params parms)
        {
            if (parms is null)
            {
                throw new ArgumentNullException(nameof(parms));
            }

            if (!IsLoginPage())
            {
                parms.SignedIn = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(_config.UserName))
            {
                throw new InvalidOperationException("Configuration username is not set for sign in.");
            }

            var loginPage = new LoginPage(_driver);
            loginPage.SignIn(_config.UserName, _config.Password);

            if (!loginPage.IsSignedIn())
            {
                throw new InvalidOperationException("Failed to sign in to the application.");
            }

            parms.SignedIn = true;
        }

        [RoutineStep(3, "Ensure role selected")]
        public void EnsureRoleSelected(Params parms)
        {
            if (parms is null)
            {
                throw new ArgumentNullException(nameof(parms));
            }

            HomePage? homePage = null;

            try
            {
                var selectRolePage = new SelectRolePage(_driver);
                parms.SelectRolePage = selectRolePage;

                IAppLandingPage landingPage;

                if (!string.IsNullOrWhiteSpace(parms.ProgramName) && !string.IsNullOrWhiteSpace(parms.RoleName))
                {
                    try
                    {
                        landingPage = selectRolePage.SelectRole(parms.ProgramName, parms.RoleName);
                    }
                    catch (InvalidOperationException)
                    {
                        landingPage = selectRolePage.SelectFirstAvailableRole();
                    }
                }
                else
                {
                    landingPage = selectRolePage.SelectFirstAvailableRole();
                }

                homePage = landingPage as HomePage
                           ?? throw new InvalidOperationException("Selecting role did not navigate to the expected home page.");

                parms.RoleSelected = landingPage?.IsLoaded == true;
            }
            catch (InvalidOperationException)
            {
                homePage = TryGetHomePage();
                if (homePage is not null)
                {
                    parms.RoleSelected = true;
                }
                else
                {
                    throw new InvalidOperationException("Unable to locate role selection or home page after sign in.");
                }
            }

            parms.HomePage = homePage;
        }

        [RoutineStep(4, "Navigate to Search Cases")]
        public void NavigateToSearchCases(Params parms)
        {
            SearchCasesPage searchCasesPage = null;

            try
            {
                if (parms.HomePage is not null && parms.HomePage.IsLoaded)
                {
                    searchCasesPage = parms.HomePage.OpenSearchCases();
                }
                else
                {
                    var navigationBar = new NavigationBar(_driver);
                    searchCasesPage = navigationBar.OpenSearchCasesPage();
                }
            }
            catch (InvalidOperationException)
            {
                var baseUri = new Uri(_config.AppUrl, UriKind.Absolute);
                var searchUri = new Uri(baseUri, "/Pages/SearchCases.aspx");

                _driver.Navigate().GoToUrl(searchUri);
                _driver.WaitForReady(30);
                searchCasesPage = new SearchCasesPage(_driver);
            }

            parms.SearchCasesPage = searchCasesPage;
            parms.SearchCasesPageLoaded = searchCasesPage.IsLoaded;
        }

        [RoutineStep(5, "Populate search criteria")]
        public void PopulateSearchCriteria(Params parms)
        {
            parms.SearchCasesPage.ApplyCriteria(parms.Criteria);
        }

        [RoutineStep(6, "Submit search")]
        public void SubmitSearch(Params parms)
        {
            parms.SearchCasesPage.SubmitSearch();
            parms.SearchCompleted = true;

            var firstResult = parms.SearchCasesPage.GetFirstResult();
            parms.FirstResult = firstResult;
            parms.FirstResultPc1Id = firstResult?.Pc1Id;

            if (!string.IsNullOrWhiteSpace(parms.Criteria.Pc1Id) && firstResult is not null)
            {
                parms.SearchMatched = string.Equals(firstResult.Pc1Id, parms.Criteria.Pc1Id, StringComparison.OrdinalIgnoreCase);
            }
        }

        private HomePage? TryGetHomePage()
        {
            try
            {
                var homePage = new HomePage(_driver);
                return homePage.IsLoaded ? homePage : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private bool IsLoginPage()
        {
            try
            {
                return _driver.FindElements(By.Id("Login1_LoginButton")).Any();
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public class Params
        {
            internal AppPage? App { get; set; }
            internal SelectRolePage? SelectRolePage { get; set; }
            internal HomePage? HomePage { get; set; }
            internal SearchCasesPage? SearchCasesPage { get; set; }
            internal SearchCasesResultRow? FirstResult { get; set; }

            public string? ProgramName { get; set; }
            public string? RoleName { get; set; }
            public SearchCasesCriteria? Criteria { get; set; }

            [RoutineOutput]
            public bool SignedIn { get; set; }

            [RoutineOutput]
            public bool RoleSelected { get; set; }

            [RoutineOutput]
            public bool SearchCasesPageLoaded { get; set; }

            [RoutineOutput]
            public bool SearchCompleted { get; set; }

            [RoutineOutput]
            public bool SearchMatched { get; set; }

            [RoutineOutput]
            public string? FirstResultPc1Id { get; set; }
        }
    }
}


