using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AFUT.Tests.Driver;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace AFUT.Tests.Pages
{
    [Page]
    public class CaseHomePage
    {
        private static readonly By Pc1IdHiddenFieldSelector = By.CssSelector("input[id$='hfPC1ID']");
        private static readonly By CaseFormSelector = By.CssSelector("form[action*='CaseHome.aspx']");
        private static readonly By CaseIdDisplaySelector = By.CssSelector("span[id$='ucBasicInformation_lblCaseID']");
        private static readonly By CaseTabsSelector = By.CssSelector("#bsTabs");
        private static readonly By TabLinkSelector = By.CssSelector("#bsTabs ul.nav li > a[data-toggle='tab']");

        private static readonly string[] BuiltInTabDisplayNames = new[]
        {
            "Basic Information",
            "Case Filters",
            "Forms",
            "Case Notes",
            "Case Documents",
            "Medical Providers",
            "Family Goal Plans/Transition Plans",
            "Funding Sources",
            "Alerts/Notifications"
        };

        private static readonly IReadOnlyList<string> BuiltInTabDisplayNamesReadOnly = Array.AsReadOnly(BuiltInTabDisplayNames);
        private static IReadOnlyList<string> _defaultTabDisplayNames = BuiltInTabDisplayNamesReadOnly;

        public static IReadOnlyList<string> DefaultTabDisplayNames => _defaultTabDisplayNames;

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

        public IReadOnlyList<CaseHomeTab> GetTabs()
        {
            var tabAnchors = _driver.FindElements(TabLinkSelector)
                                    .Where(anchor => anchor.Displayed)
                                    .ToList();

            if (tabAnchors.Count == 0)
            {
                throw new InvalidOperationException("No navigation tabs were found on the case home page.");
            }

            var tabs = new List<CaseHomeTab>();

            foreach (var anchor in tabAnchors)
            {
                var anchorId = anchor.GetAttribute("id")?.Trim();

                if (string.IsNullOrWhiteSpace(anchorId))
                {
                    continue;
                }

                var paneId = ExtractPaneId(anchor);

                if (string.IsNullOrWhiteSpace(paneId))
                {
                    continue;
                }

                _ = _driver.WaitforElementToBeInDOM(By.Id(paneId), 5)
                    ?? throw new InvalidOperationException($"Tab pane '{paneId}' was not found on the case home page.");

                var primaryLabel = anchor.FindElements(By.CssSelector("span")).FirstOrDefault();
                var labelSource = primaryLabel?.Text;

                if (string.IsNullOrWhiteSpace(labelSource))
                {
                    labelSource = anchor.Text;
                }

                var displayName = NormalizeTabLabel(labelSource);

                tabs.Add(new CaseHomeTab(_driver, anchorId, paneId, string.IsNullOrWhiteSpace(displayName)
                    ? anchorId
                    : displayName));
            }

            if (tabs.Count == 0)
            {
                throw new InvalidOperationException("Unable to resolve any addressable tabs on the case home page.");
            }

            return tabs.AsReadOnly();
        }

        public static void ConfigureDefaultTabs(IEnumerable<string> tabDisplayNames)
        {
            if (tabDisplayNames is null)
            {
                _defaultTabDisplayNames = BuiltInTabDisplayNamesReadOnly;
                return;
            }

            var sanitized = tabDisplayNames
                .Select(name => name?.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _defaultTabDisplayNames = sanitized.Length == 0
                ? BuiltInTabDisplayNamesReadOnly
                : Array.AsReadOnly(sanitized);
        }

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

        private static string ExtractPaneId(IWebElement anchor)
        {
            if (anchor is null)
            {
                return string.Empty;
            }

            var targets = new[]
            {
                anchor.GetAttribute("href"),
                anchor.GetAttribute("data-target")
            };

            foreach (var target in targets)
            {
                var paneId = NormalizeFragment(target);
                if (!string.IsNullOrWhiteSpace(paneId))
                {
                    return paneId;
                }
            }

            return string.Empty;
        }

        private static string NormalizeFragment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();

            var hashIndex = trimmed.LastIndexOf('#');
            if (hashIndex >= 0)
            {
                if (hashIndex == trimmed.Length - 1)
                {
                    return string.Empty;
                }

                trimmed = trimmed[(hashIndex + 1)..];
            }

            return trimmed.Trim();
        }

        private static string NormalizeTabLabel(string? rawLabel)
        {
            if (string.IsNullOrWhiteSpace(rawLabel))
            {
                return string.Empty;
            }

            var cleaned = rawLabel
                .Replace("\r", "\n")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim() ?? string.Empty;

            cleaned = Regex.Replace(cleaned, "\\s+", " ");
            cleaned = Regex.Replace(cleaned, "\\s+\\d(\\s+\\d)*$", string.Empty).Trim();

            return cleaned;
        }

        public CaseNotesTab GetCaseNotesTab()
        {
            var tabs = GetTabs();
            var caseNotesTab = tabs.FirstOrDefault(tab =>
                string.Equals(tab.DisplayName, "Case Notes", StringComparison.OrdinalIgnoreCase));

            if (caseNotesTab is null)
            {
                throw new InvalidOperationException("Case Notes tab was not found on the case home page.");
            }

            return new CaseNotesTab(_driver, caseNotesTab);
        }

        public class CaseHomeTab
        {
            private readonly IPookieWebDriver _driver;
            private readonly string _anchorId;
            private readonly string _paneId;

            internal CaseHomeTab(IPookieWebDriver driver, string anchorId, string paneId, string displayName)
            {
                _driver = driver ?? throw new ArgumentNullException(nameof(driver));
                DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
                _anchorId = anchorId ?? throw new ArgumentNullException(nameof(anchorId));
                _paneId = paneId ?? throw new ArgumentNullException(nameof(paneId));
            }

            public string DisplayName { get; }

            public string PaneId => _paneId;

            public bool IsActive
            {
                get
                {
                    var listItem = GetListItem();
                    var pane = GetPane();
                    return HasClass(listItem, "active") && HasClass(pane, "active");
                }
            }

            public bool IsContentDisplayed
            {
                get
                {
                    var pane = GetPane();
                    return HasClass(pane, "active") && pane.Displayed;
                }
            }

            public void Activate(int timeoutSeconds = 10)
            {
                if (IsActive)
                {
                    return;
                }

                var clickable = _driver.WaitUntilElementCanBeClicked(By.Id(_anchorId), timeoutSeconds)
                                ?? throw new InvalidOperationException($"Tab '{DisplayName}' was not clickable.");

                _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", clickable);

                clickable.Click();
                _driver.WaitForReady(timeoutSeconds);

                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(_ => IsActive);
            }

            private IWebElement GetAnchor()
            {
                return _driver.WaitforElementToBeInDOM(By.Id(_anchorId), 5)
                       ?? throw new InvalidOperationException($"Tab anchor '{_anchorId}' was not found on the case home page.");
            }

            private IWebElement GetListItem()
            {
                var anchor = GetAnchor();

                try
                {
                    return anchor.GetParent();
                }
                catch (NoSuchElementException ex)
                {
                    throw new InvalidOperationException($"Tab item for '{DisplayName}' was not found.", ex);
                }
            }

            private IWebElement GetPane()
            {
                return _driver.WaitforElementToBeInDOM(By.Id(_paneId), 5)
                       ?? throw new InvalidOperationException($"Content pane '{_paneId}' for tab '{DisplayName}' was not found.");
            }

            private static bool HasClass(IWebElement element, string className)
            {
                if (element is null)
                {
                    return false;
                }

                var classAttribute = element.GetAttribute("class");
                if (string.IsNullOrWhiteSpace(classAttribute))
                {
                    return false;
                }

                var classes = classAttribute.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return classes.Any(c => string.Equals(c, className, StringComparison.OrdinalIgnoreCase));
            }
        }

        public class CaseNotesTab
        {
            private static readonly By AddNoteLinkSelector = By.CssSelector("a[id$='lbNewCaseNote']");
            private static readonly By CaseNoteTextAreaSelector = By.CssSelector("textarea[id$='txtCaseNote']");
            private static readonly By CaseNoteDateInputSelector = By.CssSelector("input[id$='txtCaseNoteDate']");
            private static readonly By SaveNoteButtonSelector = By.CssSelector("a[id$='lbSaveCaseNote']");
            private static readonly By CancelNoteButtonSelector = By.CssSelector("a[id$='lbCancelCaseNote']");
            private static readonly By CaseNotesUpdatePanelSelector = By.CssSelector("[id$='upCaseNotes']");
            private static readonly By CaseNotesGridSelector = By.CssSelector("table[id$='gvCaseNotes']");
            private static readonly By ValidationMessageSelector = By.CssSelector("[id$='rfvCaseNote'], [id$='rfvCaseNoteDate'], .text-danger, span[style*='color:Red']");

            private readonly IPookieWebDriver _driver;
            private readonly CaseHomeTab _tab;

            internal CaseNotesTab(IPookieWebDriver driver, CaseHomeTab tab)
            {
                _driver = driver ?? throw new ArgumentNullException(nameof(driver));
                _tab = tab ?? throw new ArgumentNullException(nameof(tab));
            }

            public void Activate()
            {
                _tab.Activate();
            }

            public bool IsActive => _tab.IsActive;

            public void ClickAddNote()
            {
                EnsureTabActive();

                var addLink = _driver.WaitforElementToBeInDOM(AddNoteLinkSelector, 10)
                    ?? throw new InvalidOperationException("New Case Note link was not found.");

                // Scroll the element into view to avoid navbar overlap
                _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", addLink);
                System.Threading.Thread.Sleep(500); // Brief pause after scroll

                addLink.Click();
                _driver.WaitForUpdatePanel(30);
                _driver.WaitForReady(10);
            }

            public void EnterNoteText(string? noteText)
            {
                var textArea = _driver.WaitforElementToBeInDOM(CaseNoteTextAreaSelector, 10)
                    ?? throw new InvalidOperationException("Case note text area was not found.");

                textArea.Clear();

                if (!string.IsNullOrWhiteSpace(noteText))
                {
                    textArea.SendKeys(noteText);
                }
            }

            public void EnterNoteDate(string? noteDate)
            {
                var dateInput = _driver.WaitforElementToBeInDOM(CaseNoteDateInputSelector, 10)
                    ?? throw new InvalidOperationException("Case note date input was not found.");

                dateInput.Clear();

                if (!string.IsNullOrWhiteSpace(noteDate))
                {
                    dateInput.SendKeys(noteDate);
                }
            }

            public void SaveNote()
            {
                var saveButton = _driver.WaitforElementToBeInDOM(SaveNoteButtonSelector, 10)
                    ?? throw new InvalidOperationException("Save Case Note button was not found or not clickable.");

                // Scroll the element into view to avoid navbar overlap
                _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", saveButton);
                System.Threading.Thread.Sleep(500); // Brief pause after scroll

                saveButton.Click();
                _driver.WaitForUpdatePanel(30);
                _driver.WaitForReady(30);
            }

            public void CancelNote()
            {
                var cancelButton = _driver.WaitUntilElementCanBeClicked(CancelNoteButtonSelector, 10)
                    ?? throw new InvalidOperationException("Cancel button was not found or not clickable.");

                cancelButton.Click();
                _driver.WaitForUpdatePanel(30);
                _driver.WaitForReady(10);
            }

            public void AddNewCaseNote(string? noteDate, string? noteText)
            {
                EnsureTabActive();
                ClickAddNote();
                EnterNoteDate(noteDate);
                EnterNoteText(noteText);
                SaveNote();
            }

            public bool IsNoteSaved()
            {
                _driver.WaitForReady(5);

                var grid = _driver.WaitforElementToBeInDOM(CaseNotesGridSelector, 10);
                if (grid is null)
                {
                    return false;
                }

                var rows = grid.FindElements(By.CssSelector("tbody tr"));
                return rows.Any();
            }

            public bool HasValidationError()
            {
                var validationMessages = _driver.FindElements(ValidationMessageSelector);
                return validationMessages.Any(msg => msg.Displayed && !string.IsNullOrWhiteSpace(msg.Text));
            }

            private void EnsureTabActive()
            {
                if (!_tab.IsActive)
                {
                    _tab.Activate();
                }
            }
        }
    }
}

