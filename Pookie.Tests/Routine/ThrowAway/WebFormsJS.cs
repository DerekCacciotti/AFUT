using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Routine.ThrowAway
{
    [Routine(Name = "JS")]
    public class WebFormsJS
    {
        private readonly IPookieWebDriver driver;
        private readonly IAppConfig config;

        public static Params GetParameters() => SetUp.For(new Params());

        public WebFormsJS(IPookieWebDriver driver, IAppConfig config)
        {
            this.driver = driver;
            this.config = config;
        }

        [RoutineStep(1, "Load App")]
        public void LoadApp(Params parms)
        {
            var app = EntryPoint.OpenPage(driver, config);
            parms.HomePage = app.GetHomePage();
        }

        [RoutineStep(2, "Go to js Page")]
        public void GotoJS(Params parms)
        {
            var jsPage = parms.HomePage.GotoJSPage();
            parms.JSPage = jsPage;
        }

        [RoutineStep(3, "Alerts")]
        public void Alerts(Params parms)
        {
            parms.JSPage.DidAlertDisplay();
        }

        public class Params
        {
            internal HomePage HomePage { get; set; }
            internal JSPage JSPage { get; set; }
        }
    }
}