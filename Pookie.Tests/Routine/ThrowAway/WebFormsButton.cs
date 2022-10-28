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
    /// <summary>
    /// This class represents a action done a on a page. This called a routine.
    /// you must include the Routine Attribute and add the Routine Steps attribute on each method
    /// The nested class Params is required
    /// </summary>
    [Routine(Name = "Click a button")]
    public class WebFormsButton
    {
        private readonly IPookieWebDriver driver;
        private readonly IAppConfig config;

        // This method is used in the unit tests
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

        /// <summary>
        /// This used to bring and store values to each step
        /// </summary>
        public class Params
        {
            [RoutineOutput]
            internal bool Clicked { get; set; }

            internal HomePage HomePage { get; set; }
        }
    }
}