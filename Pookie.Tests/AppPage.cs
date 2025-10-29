using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
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

        public HomePage GetHomePage()
        {
            return new HomePage(_driver);
        }

        public SelectRolePage GetSelectRolePage()
        {
            return new SelectRolePage(_driver);
        }

        public AdminHomePage GetAdminHomePage()
        {
            return new AdminHomePage(_driver);
        }
    }
}