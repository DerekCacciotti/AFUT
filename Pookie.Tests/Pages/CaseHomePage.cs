using System;
using System.Linq;
using AFUT.Tests.Driver;
using OpenQA.Selenium;

namespace AFUT.Tests.Pages
{
    [Page]
    public class CaseHomePage
    {
        private static readonly By Pc1IdHiddenFieldSelector = By.CssSelector("input[id$='hfPC1ID']");
        private static readonly By CaseFormSelector = By.CssSelector("form[action*='CaseHome.aspx']");
        private static readonly By CaseIdDisplaySelector = By.CssSelector("span[id$='ucBasicInformation_lblCaseID']");
        private static readonly By CaseTabsSelector = By.CssSelector("#bsTabs");

        private readonly IPookieWebDriver _driver;
        private readonly IWebElement _pc1IdField;

        public CaseHomePage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));

            _driver.WaitForReady(60);

            _pc1IdField = _driver.WaitforElementToBeInDOM(Pc1IdHiddenFieldSelector, 60)
                ?? throw new InvalidOperationException("Case home page hidden case identifier not found.");

            PC1Id = _pc1IdField.GetAttribute("value")?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(PC1Id))
            {
                throw new InvalidOperationException("Case home page did not provide a case identifier.");
            }

            EnsureOnCaseHome();
        }

        public string PC1Id { get; }

        public bool IsLoaded => !string.IsNullOrWhiteSpace(PC1Id)
                                 && _driver.FindElements(CaseIdDisplaySelector).Any()
                                 && _driver.FindElements(CaseTabsSelector).Any();

        private void EnsureOnCaseHome()
        {
            var form = _driver.WaitforElementToBeInDOM(CaseFormSelector, 30);
            if (form is null)
            {
                throw new InvalidOperationException("Case home page form was not found after navigation.");
            }

            var caseIdDisplay = _driver.WaitforElementToBeInDOM(CaseIdDisplaySelector, 30)
                                  ?? throw new InvalidOperationException("Case home page case identifier label not found.");

            if (!string.Equals(caseIdDisplay.Text?.Trim(), PC1Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Case home page identifier label did not match the hidden PC1 ID value.");
            }

            var tabs = _driver.WaitforElementToBeInDOM(CaseTabsSelector, 30)
                       ?? throw new InvalidOperationException("Case home navigation tabs were not found.");

            if (!tabs.Displayed)
            {
                throw new InvalidOperationException("Case home navigation tabs are not visible.");
            }
        }
    }
}

