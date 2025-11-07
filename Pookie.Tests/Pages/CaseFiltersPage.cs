using System;
using System.Collections.Generic;
using System.Linq;
using AFUT.Tests.Driver;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace AFUT.Tests.Pages
{
    [Page]
    public class CaseFiltersPage
    {
        private static readonly By CaseFiltersFormSelector = By.CssSelector("form[action*='CaseFilters.aspx']");
        private static readonly By CaseFiltersTableSelector = By.CssSelector("table");
        private static readonly By SubmitButtonSelector = By.Id("ctl00_ContentPlaceHolder1_btnSubmit");
        private static readonly By CancelButtonSelector = By.Id("ctl00_ContentPlaceHolder1_btnCancel");

        private readonly IPookieWebDriver _driver;
        private readonly IWebElement _form;
        private readonly IWebElement _table;

        public CaseFiltersPage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));

            _driver.WaitForReady(30);

            _form = _driver.WaitforElementToBeInDOM(CaseFiltersFormSelector, 30)
                     ?? throw new InvalidOperationException("Case filters form was not found.");

            _table = _form.WaitforElementToBeInDOM(CaseFiltersTableSelector, 30)
                     ?? throw new InvalidOperationException("Case filters table was not found.");
        }

        public IReadOnlyList<CaseFilterField> GetFilters()
        {
            var rows = _table.FindElements(By.CssSelector("tbody tr"));
            if (rows.Count == 0)
            {
                rows = _table.FindElements(By.CssSelector("tr"));
            }

            var filters = new List<CaseFilterField>();

            foreach (var row in rows)
            {
                if (!row.TryGetElement(By.CssSelector("input[type='hidden'][id^='CaseFilterPK']"), out var filterPkElement))
                {
                    continue;
                }

                var pkId = filterPkElement.GetAttribute("id")?.Trim();
                if (string.IsNullOrWhiteSpace(pkId) || !pkId.StartsWith("CaseFilterPK", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var indexSuffix = pkId.Substring("CaseFilterPK".Length);
                if (string.IsNullOrWhiteSpace(indexSuffix))
                {
                    continue;
                }

                var label = row.FindElements(By.TagName("label")).FirstOrDefault();
                var name = label?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var typeElement = row.FindElements(By.Id($"FilterType{indexSuffix}")).FirstOrDefault();
                var typeCode = typeElement?.GetAttribute("value")?.Trim() ?? string.Empty;

                IWebElement? control = row.FindElements(By.Id($"fld{indexSuffix}")).FirstOrDefault();
                control ??= row.FindElements(By.CssSelector($"select[name='fld{indexSuffix}']"))?.FirstOrDefault();
                control ??= row.FindElements(By.CssSelector($"input[name='fld{indexSuffix}']"))?.FirstOrDefault();

                if (control is null)
                {
                    continue;
                }

                var controlId = control.GetAttribute("id")?.Trim();
                if (string.IsNullOrWhiteSpace(controlId))
                {
                    continue;
                }

                filters.Add(new CaseFilterField(_driver, name.Trim(), typeCode, controlId));
            }

            if (filters.Count == 0)
            {
                throw new InvalidOperationException("No editable case filters were found on the case filters page.");
            }

            return filters.AsReadOnly();
        }

        public void Submit(int timeoutSeconds = 30)
        {
            var button = _driver.WaitUntilElementCanBeClicked(SubmitButtonSelector, timeoutSeconds)
                         ?? throw new InvalidOperationException("Submit button was not available on the case filters page.");

            _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", button);
            button.Click();

            TryWaitForPostBack(timeoutSeconds);
        }

        public void Cancel(int timeoutSeconds = 30)
        {
            var cancelButton = _driver.WaitUntilElementCanBeClicked(CancelButtonSelector, timeoutSeconds)
                               ?? throw new InvalidOperationException("Cancel button was not available on the case filters page.");

            _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", cancelButton);
            cancelButton.Click();

            TryWaitForPostBack(timeoutSeconds);
        }

        private void TryWaitForPostBack(int timeoutSeconds)
        {
            try
            {
                _driver.WaitForUpdatePanel(timeoutSeconds);
            }
            catch
            {
                // Ignore if update panel is not present.
            }

            _driver.WaitForReady(timeoutSeconds);
        }

        public class CaseFilterField
        {
            private readonly IPookieWebDriver _driver;
            private readonly string _controlId;

            internal CaseFilterField(IPookieWebDriver driver, string name, string typeCode, string controlId)
            {
                _driver = driver ?? throw new ArgumentNullException(nameof(driver));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                TypeCode = typeCode ?? string.Empty;
                _controlId = controlId ?? throw new ArgumentNullException(nameof(controlId));
            }

            public string Name { get; }

            public string TypeCode { get; }

            public bool IsEnabled
            {
                get
                {
                    var element = ResolveElement();
                    return element.Enabled && element.GetAttribute("disabled") is null;
                }
            }

            public bool IsDropdown => string.Equals(ResolveElement().TagName, "select", StringComparison.OrdinalIgnoreCase);

            public bool IsTextInput
            {
                get
                {
                    var element = ResolveElement();
                    return string.Equals(element.TagName, "input", StringComparison.OrdinalIgnoreCase)
                           && string.Equals(element.GetAttribute("type"), "text", StringComparison.OrdinalIgnoreCase);
                }
            }

            public string GetSelectedValue()
            {
                var element = ResolveElement();
                if (string.Equals(element.TagName, "select", StringComparison.OrdinalIgnoreCase))
                {
                    var select = new SelectElement(element);
                    return select.SelectedOption.GetAttribute("value")?.Trim() ?? string.Empty;
                }

                return element.GetAttribute("value")?.Trim() ?? string.Empty;
            }

            public string GetSelectedDisplayText()
            {
                var element = ResolveElement();
                if (string.Equals(element.TagName, "select", StringComparison.OrdinalIgnoreCase))
                {
                    var select = new SelectElement(element);
                    return select.SelectedOption.Text?.Trim() ?? string.Empty;
                }

                return element.GetAttribute("value")?.Trim() ?? string.Empty;
            }

            public IReadOnlyList<DropdownOption> GetDropdownOptions()
            {
                if (!IsDropdown)
                {
                    return Array.Empty<DropdownOption>();
                }

                var select = new SelectElement(ResolveElement());
                var options = select.Options
                    .Select(option => new DropdownOption(
                        option.GetAttribute("value")?.Trim() ?? string.Empty,
                        option.Text?.Trim() ?? string.Empty))
                    .ToList();

                return options.AsReadOnly();
            }

            public void SetDropdownValue(string value)
            {
                if (!IsDropdown)
                {
                    throw new InvalidOperationException($"Filter '{Name}' does not support dropdown interactions.");
                }

                var element = ResolveElement();
                _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", element);

                var select = new SelectElement(element);
                select.SelectByValue(value ?? string.Empty);
            }

            public void SetTextValue(string value)
            {
                if (!IsTextInput)
                {
                    throw new InvalidOperationException($"Filter '{Name}' does not support text input interactions.");
                }

                var element = ResolveElement();
                _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", element);

                element.Clear();

                if (!string.IsNullOrEmpty(value))
                {
                    element.SendKeys(value);
                }
            }

            private IWebElement ResolveElement()
            {
                var element = _driver.WaitforElementToBeInDOM(By.Id(_controlId), 10);
                if (element is null)
                {
                    throw new InvalidOperationException($"Control '{_controlId}' for filter '{Name}' was not found.");
                }

                return element;
            }

            public record DropdownOption(string Value, string Text);
        }
    }
}

