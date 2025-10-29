using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;

namespace AFUT.Tests.Routine.ThrowAway
{
    [Routine(Name = "Web Forms Login")]
    public class WebFormsLogin
    {
        private readonly IPookieWebDriver driver;
        private readonly IAppConfig config;

        public static Params GetParameters() => SetUp.For(new Params());

        public WebFormsLogin(IPookieWebDriver driver, IAppConfig config)
        {
            this.driver = driver;
            this.config = config;
        }

        [RoutineStep(1, "Navigate to login")]
        public void Navigate(Params parms)
        {
            driver.Navigate().GoToUrl(config.AppUrl);
            driver.WaitForReady();
        }

        [RoutineStep(2, "Submit credentials")]
        public void SignIn(Params parms)
        {
            var loginPage = new LoginPage(driver);
            loginPage.SignIn(config.UserName, config.Password);
            parms.SignedIn = loginPage.IsSignedIn();
        }

        public class Params
        {
            [RoutineOutput]
            public bool SignedIn { get; set; }
        }
    }
}

