using AFUT.Tests.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests
{
    public class AppPage
    {
        private readonly IPookieWebDriver _driver;

        public AppPage(IPookieWebDriver driver)
        {
            _driver = driver;
        }
    }
}