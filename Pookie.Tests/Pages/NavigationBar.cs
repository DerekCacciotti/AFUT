using System;
using System.Linq;
using System.Threading;
using AFUT.Tests.Driver;
using OpenQA.Selenium;

namespace AFUT.Tests.Pages
{
    public class NavigationBar
    {
        private static readonly By NavigationSelector = By.CssSelector(".navbar");
        private static readonly By SearchCasesListSelector = By.CssSelector("li[id$='liSearchCases']");
        private static readonly By SearchCasesToggleSelector = By.CssSelector("a.dropdown-toggle");
        private static readonly By SearchCasesLinkSelector = By.CssSelector("a[href*='SearchCases.aspx']");
        private static readonly By RecentCasesMenuSelector = By.CssSelector("ul[id$='mnuMRUCases']");
        private static readonly By CaseLinkSelector = By.CssSelector("li > a.caseslink");

        private readonly IPookieWebDriver _driver;
        private readonly IWebElement _navigationRoot;

        public NavigationBar(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _driver.WaitForReady(30);

            _navigationRoot = _driver.WaitforElementToBeInDOM(NavigationSelector, 30)
                ?? throw new InvalidOperationException("Navigation bar is not available on the current page.");
        }

        public CaseHomePage OpenFirstRecentCaseFromSearchCasesDropdown()
        {
            var searchCasesNode = _navigationRoot.WaitforElementToBeInDOM(SearchCasesListSelector, 10)
                ?? throw new InvalidOperationException("Search Cases menu item was not found in the navigation bar.");

            var toggle = searchCasesNode.FindElements(SearchCasesToggleSelector).FirstOrDefault()
                ?? throw new InvalidOperationException("Search Cases dropdown toggle was not found.");

            toggle.Click();
            _driver.WaitForReady(5);

            var dropdownMenu = searchCasesNode.WaitforElementToBeInDOM(RecentCasesMenuSelector, 10)
                ?? throw new InvalidOperationException("Search Cases dropdown menu did not appear.");

            WaitUntilDisplayed(dropdownMenu, TimeSpan.FromSeconds(5));

            var firstCaseLink = dropdownMenu.FindElements(CaseLinkSelector).FirstOrDefault()
                ?? throw new InvalidOperationException("Search Cases dropdown does not contain any cases.");

            var expectedPc1Id = firstCaseLink.Text?.Trim();

            firstCaseLink.Click();

            var caseHomePage = new CaseHomePage(_driver);

            if (!string.IsNullOrWhiteSpace(expectedPc1Id) &&
                !string.Equals(caseHomePage.PC1Id, expectedPc1Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected to navigate to case '{expectedPc1Id}' but landed on '{caseHomePage.PC1Id}'.");
            }

            return caseHomePage;
        }

        public SearchCasesPage OpenSearchCasesPage()
        {
            var searchCasesNode = _navigationRoot.WaitforElementToBeInDOM(SearchCasesListSelector, 10)
                ?? throw new InvalidOperationException("Search Cases menu item was not found in the navigation bar.");

            var directLink = searchCasesNode.FindElements(SearchCasesLinkSelector).FirstOrDefault();

            if (directLink is null)
            {
                var toggle = searchCasesNode.FindElements(SearchCasesToggleSelector).FirstOrDefault()
                    ?? throw new InvalidOperationException("Search Cases dropdown toggle was not found.");

                toggle.Click();
                _driver.WaitForUpdatePanel(30);
                _driver.WaitForReady(30);

                directLink = searchCasesNode.FindElements(SearchCasesLinkSelector).FirstOrDefault()
                    ?? throw new InvalidOperationException("Search Cases link was not found after expanding the menu.");
            }

            directLink.Click();
            _driver.WaitForUpdatePanel(30);
            _driver.WaitForReady(30);
            return new SearchCasesPage(_driver);
        }

        private static void WaitUntilDisplayed(IWebElement element, TimeSpan timeout)
        {
            var end = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow <= end)
            {
                if (element.Displayed)
                {
                    return;
                }

                Thread.Sleep(100);
            }

            throw new TimeoutException("Element did not become visible within the allotted time.");
        }
    }
}

