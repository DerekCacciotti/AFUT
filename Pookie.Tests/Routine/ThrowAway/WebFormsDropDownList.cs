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
    [Routine(Name = "Dropdown List")]
    public class WebFormsDropDownList
    {
        private readonly IPookieWebDriver _driver;
        private readonly IAppConfig _config;

        public static Params GetParameters() => SetUp.For(new Params());

        public WebFormsDropDownList(IPookieWebDriver driver, IAppConfig config)
        {
            _driver = driver;
            _config = config;
        }

        [RoutineStep(1, "Load App")]
        public void LoadApp(Params parms)
        {
            var app = EntryPoint.OpenPage(_driver, _config);
            parms.HomePage = app.GetHomePage();
        }

        [RoutineStep(2, "Set Value")]
        public void SetValue(Params parms)
        {
            parms.Changed = parms.HomePage.SetDropdownList(parms.Value);
        }

        public class Params
        {
            internal string Value { get; set; }
            internal HomePage HomePage { get; set; }
            internal bool Changed { get; set; }
        }
    }
}