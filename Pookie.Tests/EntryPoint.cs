using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests
{
    public static class EntryPoint
    {
        public static AppPage OpenPage(IPookieWebDriver driver, IAppConfig config)
        {
            if (!string.IsNullOrEmpty(config.UserName))
            {
                driver.Login(config.UserName, config.Password, config.AppUrl);
                driver.WaitForReady();
            }
            else
            {
                driver.Navigate().GoToUrl(config.AppUrl);
                driver.WaitForReady();
            }
            return new AppPage(driver);
        }
    }
}