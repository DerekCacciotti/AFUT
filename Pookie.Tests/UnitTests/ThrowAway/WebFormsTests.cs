using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.UnitTests.ThrowAway
{
    public class WebFormsTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig config;
        private readonly IPookieDriverFactory driverFactory;

        public WebFormsTests(AppConfig config)
        {
            this.config = config;
            this.driverFactory = config.ServiceProvider.GetService<IPookieDriverFactory>();
        }

        [Fact]
        public void WebFormTry()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsLoad(driver, config);
            var parameters = Routine.ThrowAway.WebFormsLoad.GetParameters();
            routine.LoadApp(parameters);
            routine.Wait(parameters);
            Assert.True(parameters.Worked);
        }

        [Fact]
        public void WebFormsButtonClick()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsButton(driver, config);
            var parameters = Routine.ThrowAway.WebFormsButton.GetParameters();
            routine.LoadApp(parameters);
            routine.ClickButton(parameters);
            Assert.True(parameters.Clicked);
        }
    }
}