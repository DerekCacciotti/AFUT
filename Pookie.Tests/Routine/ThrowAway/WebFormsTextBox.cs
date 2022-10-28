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
    [Routine(Name = "Setting a value of a TextBox")]
    public class WebFormsTextBox
    {
        private readonly IPookieWebDriver driver;
        private readonly IAppConfig config;

        public static Params GetParameters() => SetUp.For(new Params());

        public WebFormsTextBox(IPookieWebDriver driver, IAppConfig config)
        {
            this.config = config;
            this.driver = driver;
        }

        [RoutineStep(1, "Load App")]
        public void LoadApp(Params parms)
        {
            var app = EntryPoint.OpenPage(driver, config);
            parms.HomePage = app.GetHomePage();
        }

        [RoutineStep(2, "Set TextBox Value")]
        public void SetTextBox(Params parms)
        {
            parms.ValueSet = parms.HomePage.SetTextBox(parms.Value);
        }

        public class Params
        {
            internal string Value { get; set; }
            internal HomePage HomePage { get; set; }
            internal bool ValueSet { get; set; }
        }
    }
}