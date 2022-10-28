using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests
{
    // Use this class load various page as a entry point see the EntryPoint class

    public class AppPage
    {
        private readonly IPookieWebDriver _driver;

        public AppPage(IPookieWebDriver driver)
        {
            _driver = driver;
        }

        public HomePage GetHomePage()
        {
            return new HomePage(_driver);
        }
    }
}