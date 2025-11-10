using System;
using AFUT.Tests.Driver;
using OpenQA.Selenium;

namespace AFUT.Tests.Pages
{
    [Page]
    public class AdminHomePage : IAppLandingPage
    {
        private static readonly By ImpersonateSectionSelector = By.CssSelector("[id$='divImpersonateWorker']");
        private readonly IPookieWebDriver _driver;
        private readonly IWebElement _impersonateSection;

        public AdminHomePage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _driver.WaitForReady(60);

            _impersonateSection = _driver.WaitforElementToBeInDOM(ImpersonateSectionSelector, 60)
                ?? throw new InvalidOperationException("Admin landing page did not render as expected.");
        }

        public bool IsLoaded => _impersonateSection?.Displayed == true;

        public SearchCasesPage OpenSearchCases()
        {
            _driver.WaitForReady();
            var navigationBar = new NavigationBar(_driver);
            return navigationBar.OpenSearchCasesPage();
        }
    }
}

