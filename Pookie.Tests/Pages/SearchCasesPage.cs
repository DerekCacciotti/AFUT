using System;
using System.Collections.Generic;
using System.Linq;
using AFUT.Tests.Driver;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace AFUT.Tests.Pages
{
    [Page]
    public class SearchCasesPage
    {
        private static readonly By SearchFormSelector = By.CssSelector("form[action*='SearchCases.aspx']");
        private static readonly By Pc1IdInputSelector = By.CssSelector("input[id$='txtPC1ID']");
        private static readonly By Pc1FirstNameInputSelector = By.CssSelector("input[id$='txtPC1FirstName']");
        private static readonly By Pc1LastNameInputSelector = By.CssSelector("input[id$='txtPC1LastName']");
        private static readonly By Pc1DobInputSelector = By.CssSelector("input[id$='txtPC1DOB']");
        private static readonly By PcPhoneInputSelector = By.CssSelector("input[id$='txtPCPhone']");
        private static readonly By TcFirstNameInputSelector = By.CssSelector("input[id$='txtTCFirstName']");
        private static readonly By TcLastNameInputSelector = By.CssSelector("input[id$='txtTCLastName']");
        private static readonly By TcDobInputSelector = By.CssSelector("input[id$='txtTCDOB']");
        private static readonly By WorkerDropdownSelector = By.CssSelector("select[id$='ddlWorker']");
        private static readonly By AllWorkersCheckboxSelector = By.CssSelector("input[id$='chkAllWorkers']");
        private static readonly By AlternateIdInputSelector = By.CssSelector("input[id$='txtAlternateID']");
        private static readonly By HvCasePkInputSelector = By.CssSelector("input[id$='txtHVCasePK']");
        private static readonly By SearchButtonSelector = By.CssSelector("[id$='btSearch']");
        private static readonly By CancelButtonSelector = By.CssSelector("a[id$='btnCancel']");
        private static readonly By ResultsGridSelector = By.CssSelector("table[id$='grResults']");
        private static readonly By ResultsRowSelector = By.CssSelector("tbody tr");
        private static readonly By NoRecordsMessageSelector = By.CssSelector("thead td");

        private readonly IPookieWebDriver _driver;
        private readonly IWebElement _searchForm;

        public SearchCasesPage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _driver.WaitForReady(30);

            _searchForm = _driver.WaitforElementToBeInDOM(SearchFormSelector, 30)
                          ?? throw new InvalidOperationException("Search Cases form was not found on the page.");

            _ = _searchForm.WaitforElementToBeInDOM(SearchButtonSelector, 30)
                ?? throw new InvalidOperationException("Search button was not found within the Search Cases form.");
        }

        public bool IsLoaded =>
            _searchForm.Displayed &&
            _searchForm.FindElements(SearchButtonSelector).Any();

        public IWebElement GetSearchButton() =>
            _searchForm.WaitforElementToBeInDOM(SearchButtonSelector, 5)
            ?? throw new InvalidOperationException("Search button is not available.");

        public IWebElement GetCancelButton() =>
            _searchForm.WaitforElementToBeInDOM(CancelButtonSelector, 5)
            ?? throw new InvalidOperationException("Cancel button is not available.");

        public void ApplyCriteria(SearchCasesCriteria criteria)
        {
            if (criteria is null)
            {
                throw new ArgumentNullException(nameof(criteria));
            }

            SetInputValue(Pc1IdInputSelector, criteria.Pc1Id);
            SetInputValue(Pc1FirstNameInputSelector, criteria.Pc1FirstName);
            SetInputValue(Pc1LastNameInputSelector, criteria.Pc1LastName);
            SetInputValue(Pc1DobInputSelector, criteria.Pc1Dob);
            SetInputValue(PcPhoneInputSelector, criteria.PcPhone);
            SetInputValue(TcFirstNameInputSelector, criteria.TcFirstName);
            SetInputValue(TcLastNameInputSelector, criteria.TcLastName);
            SetInputValue(TcDobInputSelector, criteria.TcDob);
            SetDropdownValue(WorkerDropdownSelector, criteria.WorkerDisplayText);
            SetCheckboxValue(AllWorkersCheckboxSelector, criteria.IncludeAllWorkers);
            SetInputValue(AlternateIdInputSelector, criteria.AlternateId);
            SetInputValue(HvCasePkInputSelector, criteria.HvCasePk);
        }

        public void SubmitSearch()
        {
            var searchButton = GetSearchButton();
            searchButton.Click();
            _driver.WaitForUpdatePanel(30);
            _driver.WaitForReady(30);
        }

        public void CancelSearch()
        {
            var cancelButton = GetCancelButton();
            cancelButton.Click();
            _driver.WaitForReady(30);
        }

        public IReadOnlyCollection<SearchCasesResultRow> GetResults()
        {
            var grid = _driver.WaitforElementToBeInDOM(ResultsGridSelector, 30);
            if (grid is null)
            {
                return Array.Empty<SearchCasesResultRow>();
            }

            var rows = grid.FindElements(ResultsRowSelector)
                .Where(row => row.FindElements(By.TagName("td")).Any())
                .Select(row => new SearchCasesResultRow(_driver, row))
                .ToList();

            return rows;
        }

        public SearchCasesResultRow? GetFirstResult() => GetResults().FirstOrDefault();

        public bool IsNoRecordsMessageDisplayed()
        {
            var grid = _driver.WaitforElementToBeInDOM(ResultsGridSelector, 10);
            if (grid is null)
            {
                return false;
            }

            var headerCell = grid.FindElements(NoRecordsMessageSelector).FirstOrDefault();
            if (headerCell is null)
            {
                return false;
            }

            var message = headerCell.Text?.Trim();
            return string.Equals(message, "No records found.", StringComparison.OrdinalIgnoreCase);
        }

        private void SetInputValue(By selector, string? value)
        {
            var element = _searchForm.WaitforElementToBeInDOM(selector, 10);
            if (element is null)
            {
                throw new InvalidOperationException($"Input element '{selector}' was not found on the Search Cases page.");
            }

            element.Clear();

            if (!string.IsNullOrEmpty(value))
            {
                element.SendKeys(value);
            }
        }

        private void SetDropdownValue(By selector, string? displayText)
        {
            if (string.IsNullOrWhiteSpace(displayText))
            {
                return;
            }

            var dropdown = _searchForm.WaitforElementToBeInDOM(selector, 10)
                           ?? throw new InvalidOperationException("Worker dropdown was not found on the Search Cases page.");

            var select = new SelectElement(dropdown);
            select.SelectByText(displayText.Trim());
        }

        private void SetCheckboxValue(By selector, bool? shouldBeChecked)
        {
            if (!shouldBeChecked.HasValue)
            {
                return;
            }

            var checkbox = _searchForm.WaitforElementToBeInDOM(selector, 10)
                           ?? throw new InvalidOperationException("All Workers checkbox was not found on the Search Cases page.");

            if (checkbox.Selected != shouldBeChecked.Value)
            {
                checkbox.Click();
            }
        }
    }

    public class SearchCasesCriteria
    {
        public string? Pc1Id { get; set; }
        public string? Pc1FirstName { get; set; }
        public string? Pc1LastName { get; set; }
        public string? Pc1Dob { get; set; }
        public string? PcPhone { get; set; }
        public string? TcFirstName { get; set; }
        public string? TcLastName { get; set; }
        public string? TcDob { get; set; }
        public string? WorkerDisplayText { get; set; }
        public bool? IncludeAllWorkers { get; set; }
        public string? AlternateId { get; set; }
        public string? HvCasePk { get; set; }
    }

    public class SearchCasesResultRow
    {
        private static readonly By Pc1IdCellSelector = By.CssSelector("td:nth-child(1)");
        private static readonly By Pc1LinkSelector = By.CssSelector("td:nth-child(1) a");

        private readonly IPookieWebDriver _driver;
        private readonly IWebElement _row;

        internal SearchCasesResultRow(IPookieWebDriver driver, IWebElement row)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _row = row ?? throw new ArgumentNullException(nameof(row));
        }

        public string? Pc1Id
        {
            get
            {
                var cell = _row.FindElements(Pc1IdCellSelector).FirstOrDefault();
                return cell?.Text?.Trim();
            }
        }

        public CaseHomePage OpenCaseHome()
        {
            var link = _row.FindElements(Pc1LinkSelector).FirstOrDefault()
                       ?? throw new InvalidOperationException("PC1 identifier link was not found in the selected search result row.");

            link.Click();
            _driver.WaitForReady(60);
            return new CaseHomePage(_driver);
        }
    }
}


