using AFUT.Tests.Driver;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Pages
{
    [Page]
    public class JSPage
    {
        private readonly IPookieWebDriver driver;

        public JSPage(IPookieWebDriver driver)
        {
            this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public void DidAlertDisplay()
        {
            driver.WaitForReady();
            driver.WaitForBrowserAlert();
        }
    }
}