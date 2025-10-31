using System;
using AFUT.Tests.Driver;
using OpenQA.Selenium;

namespace AFUT.Tests.Pages
{
    [Page]
    public class CaseHomePage
    {
        private static readonly By CaseIdHiddenFieldSelector = By.CssSelector("input[id$='hfPC1ID']");
        private static readonly By CaseFormSelector = By.CssSelector("form[action*='CaseHome.aspx']");

        private readonly IPookieWebDriver _driver;
        private readonly IWebElement _caseIdField;

        public CaseHomePage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));

            _driver.WaitForReady(60);

            _caseIdField = _driver.WaitforElementToBeInDOM(CaseIdHiddenFieldSelector, 60)
                ?? throw new InvalidOperationException("Case home page hidden case identifier not found.");

            CaseId = _caseIdField.GetAttribute("value")?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(CaseId))
            {
                throw new InvalidOperationException("Case home page did not provide a case identifier.");
            }

            EnsureOnCaseHome();
        }

        public string CaseId { get; }

        public bool IsLoaded => !string.IsNullOrWhiteSpace(CaseId);

        private void EnsureOnCaseHome()
        {
            var form = _driver.WaitforElementToBeInDOM(CaseFormSelector, 30);
            if (form is null)
            {
                throw new InvalidOperationException("Case home page form was not found after navigation.");
            }
        }
    }
}

