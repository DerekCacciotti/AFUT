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

            var expectedCaseId = firstCaseLink.Text?.Trim();

            firstCaseLink.Click();

            var caseHomePage = new CaseHomePage(_driver);

            if (!string.IsNullOrWhiteSpace(expectedCaseId) &&
                !string.Equals(caseHomePage.CaseId, expectedCaseId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected to navigate to case '{expectedCaseId}' but landed on '{caseHomePage.CaseId}'.");
            }

            return caseHomePage;
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

