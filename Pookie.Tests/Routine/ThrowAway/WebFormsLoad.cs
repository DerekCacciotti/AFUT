using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Routine.ThrowAway
{
    [Routine(Name = "Web Forms Test")]
    public class WebFormsLoad
    {
        private readonly IPookieWebDriver driver;
        private readonly IAppConfig config;

        public static Params GetParameters() => SetUp.For(new Params());

        public WebFormsLoad(IPookieWebDriver driver, IAppConfig config)
        {
            this.driver = driver;
            this.config = config;
        }

        [RoutineStep(1, "Load App")]
        public void LoadApp(Params parms)
        {
            var app = EntryPoint.OpenPage(driver, config);
            parms.Worked = driver.Url.Contains("localhost");
        }

        public void Wait(Params parms)
        {
            driver.WaitForReady(30);
        }

        public class Params
        {
            [RoutineOutput]
            internal bool Worked { get; set; }
        }
    }
}