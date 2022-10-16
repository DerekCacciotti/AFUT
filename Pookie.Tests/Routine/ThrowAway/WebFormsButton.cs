using AFUT.Tests.Attributes;
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
    [Routine(Name = "Click a button")]
    public class WebFormsButton
    {
        private readonly IPookieWebDriver driver;
        private readonly IAppConfig config;

        public static Params GetParameters() => SetUp.For(new Params());

        public WebFormsButton(IPookieWebDriver driver, IAppConfig config)
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

        [RoutineStep(2, "Click button")]
        public void ClickButton(Params parms)
        {
            parms.HomePage.ClickButton();
            parms.Clicked = parms.HomePage.ButtonClicked;
        }

        public class Params
        {
            [RoutineOutput]
            internal bool Clicked { get; set; }

            internal HomePage HomePage { get; set; }
        }
    }
}