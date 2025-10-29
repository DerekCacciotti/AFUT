using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;

namespace AFUT.Tests.Routine.ThrowAway
{
    [Routine(Name = "Select Role")]
    public class WebFormsSelectRole
    {
        private readonly IPookieWebDriver driver;
        private readonly IAppConfig config;

        public static Params GetParameters()
        {
            var parameters = SetUp.For(new Params());

            if (string.IsNullOrWhiteSpace(parameters.ProgramName))
            {
                parameters.ProgramName = "Program 1";
            }

            if (string.IsNullOrWhiteSpace(parameters.RoleName))
            {
                parameters.RoleName = "DataEntry";
            }

            return parameters;
        }

        public WebFormsSelectRole(IPookieWebDriver driver, IAppConfig config)
        {
            this.driver = driver;
            this.config = config;
        }

        [RoutineStep(1, "Load App")]
        public void LoadApp(Params parms)
        {
            parms.App = EntryPoint.OpenPage(driver, config);
        }

        [RoutineStep(2, "Navigate to Select Role")]
        public void NavigateToRoleSelection(Params parms)
        {
            parms.SelectRolePage = parms.App.GetSelectRolePage();
        }

        [RoutineStep(3, "Select Role")]
        public void SelectRole(Params parms)
        {
            var landingPage = parms.SelectRolePage.SelectRole(parms.ProgramName, parms.RoleName);
            parms.LandingPage = landingPage;
            parms.HomePage = landingPage as HomePage;
            parms.AdminHomePage = landingPage as AdminHomePage;
            parms.RoleSelected = landingPage?.IsLoaded == true;
        }

        public class Params
        {
            internal AppPage App { get; set; }
            internal SelectRolePage SelectRolePage { get; set; }
            internal HomePage HomePage { get; set; }
            internal AdminHomePage AdminHomePage { get; set; }
            internal IAppLandingPage LandingPage { get; set; }

            public string ProgramName { get; set; }
            public string RoleName { get; set; }

            [RoutineOutput]
            public bool RoleSelected { get; set; }
        }
    }
}

