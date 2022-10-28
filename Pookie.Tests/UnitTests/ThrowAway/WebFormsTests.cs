using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

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

        [Fact(Skip = "Example")]
        public void WebFormTry()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsLoad(driver, config);
            var parameters = Routine.ThrowAway.WebFormsLoad.GetParameters();
            routine.LoadApp(parameters);
            routine.Wait(parameters);
            Assert.True(parameters.Worked);
        }

        [Fact(Skip = "Example")]
        public void WebFormsButtonClick()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsButton(driver, config);
            var parameters = Routine.ThrowAway.WebFormsButton.GetParameters();
            routine.LoadApp(parameters);
            routine.ClickButton(parameters);
            Assert.True(parameters.Clicked);
        }

        [Fact(Skip = "Example")]
        public void WebFormsDropDown()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsDropDownList(driver, config);
            var parameters = Routine.ThrowAway.WebFormsDropDownList.GetParameters();
            parameters.Value = "Pantera";
            routine.LoadApp(parameters);
            routine.SetValue(parameters);
            Assert.True(parameters.Changed);
        }

        [Fact(Skip = "Example")]
        public void WebFormsTextBox()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsTextBox(driver, config);
            var parameters = Routine.ThrowAway.WebFormsTextBox.GetParameters();
            parameters.Value = "Asking Alexandria";
            routine.LoadApp(parameters);
            routine.SetTextBox(parameters);
            Assert.True(parameters.ValueSet);
        }

        [Fact(Skip = "Example")]
        public void WebFormsHTMLButton()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsNormalControls(driver, config);
            var parameters = Routine.ThrowAway.WebFormsNormalControls.GetParameters();
            routine.LoadApp(parameters);
            routine.ClickHTMLButton(parameters);
        }

        [Fact(Skip = "Example")]
        public void GridsPageTest()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsGrids(driver, config);
            var parameters = Routine.ThrowAway.WebFormsGrids.GetParameters();
            routine.LoadApp(parameters);
            routine.GoToGrids(parameters);
            routine.ClickSelect(parameters);
            Assert.True(parameters.Selected);
        }

        [Fact(Skip = "Example")]
        public void JSTest()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsJS(driver, config);
            var parameters = Routine.ThrowAway.WebFormsJS.GetParameters();
            routine.LoadApp(parameters);
            routine.GotoJS(parameters);
            routine.Alerts(parameters);
        }
    }
}