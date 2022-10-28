using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AFUT.Tests.Pages;

namespace AFUT.Tests.Routine.ThrowAway
{
    /// <summary>
    /// This class represents a action done a on a page. This called a routine.
    /// you must include the Routine Attribute and add the Routine Steps attribute on each method
    /// The nested class Params is required
    /// </summary>
    [Routine(Name = "Grids")]
    public class WebFormsGrids
    {
        private readonly IPookieWebDriver driver;
        private readonly IAppConfig config;

        public static Params GetParameters() => SetUp.For(new Params());

        public WebFormsGrids(IPookieWebDriver driver, IAppConfig config)
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

        [RoutineStep(2, "Go to Grids Page")]
        public void GoToGrids(Params parms)
        {
            var gridsPage = parms.HomePage.GotoGridsPage();
            parms.GridsPage = gridsPage;
        }

        [RoutineStep(3, "Click Select")]
        public void ClickSelect(Params parms)
        {
            parms.Selected = parms.GridsPage.ClickSelectInGrid();
        }

        public class Params
        {
            internal HomePage HomePage { get; set; }
            internal GridsPage GridsPage { get; set; }
            internal bool Selected { get; set; }
        }
    }
}