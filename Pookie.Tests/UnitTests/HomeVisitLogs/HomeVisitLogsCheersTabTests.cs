using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.UnitTests.Attributes;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.HomeVisitLogs
{
    public class HomeVisitLogsCheersTabTests : HomeVisitLogsTestBase
    {
        public HomeVisitLogsCheersTabTests(AppConfig config, ITestOutputHelper output)
            : base(config, output)
        {
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(4)]
        public void CheersTabDisplaysKeySections(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting CHEERS tab test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);
            OpenExistingHomeVisitLog(driver);

            var cheersTabLink = driver.WaitforElementToBeInDOM(By.CssSelector("a#lnkcheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab link was not found.");
            cheersTabLink.Click();
            driver.WaitForReady(5);
            Thread.Sleep(300);

            var cheersPane = driver.WaitforElementToBeInDOM(By.CssSelector("div#cheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab content was not found.");

            var cheersHeader = cheersPane.FindElements(By.CssSelector(".panel-body span"))
                .FirstOrDefault();
            var headerText = cheersHeader?.Text?.Trim() ?? string.Empty;
            Assert.Contains("CHEERS", headerText, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine("[PASS] CHEERS tab content visible via edit flow.");
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(5)]
        public void CheersAutoTextPrefillsAllTextAreas(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting CHEERS auto-text test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);
            OpenExistingHomeVisitLog(driver);

            var cheersTabLink = driver.WaitforElementToBeInDOM(By.CssSelector("a#lnkcheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab link was not found.");
            cheersTabLink.Click();
            driver.WaitForReady(5);

            var cheersPane = driver.WaitforElementToBeInDOM(By.CssSelector("div#cheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab content was not found.");

            var autoTextCheckbox = cheersPane.FindElement(By.CssSelector("input[id$='chkCCIAutoText']"));
            if (!autoTextCheckbox.Selected)
            {
                autoTextCheckbox.Click();
                driver.WaitForUpdatePanel(10);
                driver.WaitForReady(10);
                Thread.Sleep(300);
            }

            string[] textAreaSelectors =
            {
                "textarea[id$='txtCHEERSCues']",
                "textarea[id$='txtCHEERSHolding']",
                "textarea[id$='txtCHEERSExpression']",
                "textarea[id$='txtCHEERSRhythmReciprocity']",
                "textarea[id$='txtCHEERSSmiles']"
            };

            foreach (var selector in textAreaSelectors)
            {
                var textarea = cheersPane.FindElement(By.CssSelector(selector));
                var value = textarea.GetAttribute("value") ?? textarea.Text;
                Assert.False(string.IsNullOrWhiteSpace(value),
                    $"Expected CHEERS textarea '{selector}' to be pre-filled when auto-text is selected.");
            }

            var autoTextOptions = cheersPane.FindElements(By.CssSelector("table[id$='cblCheersAutoTextOptions'] input[type='checkbox']"));
            Assert.NotEmpty(autoTextOptions);

            for (var i = 0; i < autoTextOptions.Count; i++)
            {
                var option = autoTextOptions[i];
                option.Click();
                driver.WaitForUpdatePanel(5);
                driver.WaitForReady(5);
                Thread.Sleep(200);

                foreach (var selector in textAreaSelectors)
                {
                    var textarea = cheersPane.FindElement(By.CssSelector(selector));
                    var value = textarea.GetAttribute("value") ?? textarea.Text;
                    Assert.False(string.IsNullOrWhiteSpace(value),
                        $"Textarea '{selector}' should remain populated after toggling auto-text option {i}.");
                }

                option.Click();
                driver.WaitForUpdatePanel(5);
                driver.WaitForReady(5);
                Thread.Sleep(200);
            }

            _output.WriteLine("[PASS] CHEERS auto-text populated all text areas.");
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(6)]
        public void CheersDropdownSelectionsAppendToTextAreas(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting CHEERS dropdown test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);
            OpenExistingHomeVisitLog(driver);

            var cheersTabLink = driver.WaitforElementToBeInDOM(By.CssSelector("a#lnkcheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab link was not found.");
            cheersTabLink.Click();
            driver.WaitForReady(5);

            var cheersPane = driver.WaitforElementToBeInDOM(By.CssSelector("div#cheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab content was not found.");

            var autoTextCheckbox = cheersPane.FindElement(By.CssSelector("input[id$='chkCCIAutoText']"));
            if (!autoTextCheckbox.Selected)
            {
                autoTextCheckbox.Click();
                driver.WaitForUpdatePanel(5);
                driver.WaitForReady(5);
                Thread.Sleep(200);
            }

            var dropdownSelectors = new[]
            {
                ("select[id$='ddlCHEERSCues']", "textarea[id$='txtCHEERSCues']"),
                ("select[id$='ddlCHEERSHolding']", "textarea[id$='txtCHEERSHolding']"),
                ("select[id$='ddlCHEERSExpression']", "textarea[id$='txtCHEERSExpression']"),
                ("select[id$='ddlCHEERSEmpathy']", "textarea[id$='txtCHEERSEmpathy']"),
                ("select[id$='ddlCHEERSRhythmReciprocity']", "textarea[id$='txtCHEERSRhythmReciprocity']"),
                ("select[id$='ddlCHEERSSmiles']", "textarea[id$='txtCHEERSSmiles']")
            };

            foreach (var (dropdownSelector, textareaSelector) in dropdownSelectors)
            {
                var dropdown = cheersPane.FindElement(By.CssSelector(dropdownSelector));
                var textarea = cheersPane.FindElement(By.CssSelector(textareaSelector));

                var options = new SelectElement(dropdown).Options.Where(o => !string.IsNullOrWhiteSpace(o.Text) && !o.Text.Contains("--", StringComparison.OrdinalIgnoreCase)).ToList();
                Assert.NotEmpty(options);

                foreach (var option in options)
                {
                    WebElementHelper.SelectDropdownOption(driver, dropdown, dropdownSelector, option.Text.Trim(), option.GetAttribute("value"));
                    driver.WaitForReady(1);
                    Thread.Sleep(150);

                    var value = textarea.GetAttribute("value") ?? textarea.Text ?? string.Empty;
                    Assert.Contains(option.Text.Trim(), value, StringComparison.OrdinalIgnoreCase);
                }
            }

            _output.WriteLine("[PASS] CHEERS dropdown selections appended expected text.");
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(7)]
        public void ReflectiveStrategyCheckboxesRevealTextAreas(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting Reflective Strategies test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);
            OpenExistingHomeVisitLog(driver);

            var cheersTabLink = driver.WaitforElementToBeInDOM(By.CssSelector("a#lnkcheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab link was not found.");
            cheersTabLink.Click();
            driver.WaitForReady(5);

            var cheersPane = driver.WaitforElementToBeInDOM(By.CssSelector("div#cheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab content was not found.");

            // Ensure dropdown + textarea content is pre-filled before interacting with reflective strategies
            var autoTextCheckbox = cheersPane.FindElement(By.CssSelector("input[id$='chkCCIAutoText']"));
            if (!autoTextCheckbox.Selected)
            {
                autoTextCheckbox.Click();
                driver.WaitForUpdatePanel(5);
                driver.WaitForReady(5);
                Thread.Sleep(200);
            }

            var dropdownSelectors = new[]
            {
                ("select[id$='ddlCHEERSCues']", "textarea[id$='txtCHEERSCues']"),
                ("select[id$='ddlCHEERSHolding']", "textarea[id$='txtCHEERSHolding']"),
                ("select[id$='ddlCHEERSExpression']", "textarea[id$='txtCHEERSExpression']"),
                ("select[id$='ddlCHEERSEmpathy']", "textarea[id$='txtCHEERSEmpathy']"),
                ("select[id$='ddlCHEERSRhythmReciprocity']", "textarea[id$='txtCHEERSRhythmReciprocity']"),
                ("select[id$='ddlCHEERSSmiles']", "textarea[id$='txtCHEERSSmiles']")
            };

            foreach (var (dropdownSelector, textareaSelector) in dropdownSelectors)
            {
                var dropdown = cheersPane.FindElement(By.CssSelector(dropdownSelector));
                var textarea = cheersPane.FindElement(By.CssSelector(textareaSelector));

                var options = new SelectElement(dropdown).Options.Where(o => !string.IsNullOrWhiteSpace(o.Text) && !o.Text.Contains("--", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var option in options)
                {
                    WebElementHelper.SelectDropdownOption(driver, dropdown, dropdownSelector, option.Text.Trim(), option.GetAttribute("value"));
                    driver.WaitForReady(1);
                    Thread.Sleep(150);
                    var value = textarea.GetAttribute("value") ?? textarea.Text ?? string.Empty;
                    if (!value.Contains(option.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        WebElementHelper.SetInputValue(driver, textarea, $"{option.Text} details recorded.", $"CHEERS textarea {textareaSelector}", triggerBlur: true);
                    }
                }
            }

            var reflectiveCheckboxes = new[]
            {
                ("input[id$='chkRSATP']", "#divRSATP", "#divRSATP textarea"),
                ("input[id$='chkRSSATP']", "#divRSSATP", "#divRSSATP textarea"),
                ("input[id$='chkRSFFF']", "#divRSFFF", "#divRSFFF textarea"),
                ("input[id$='chkRSEW']", "#divRSEW", "#divRSEW textarea"),
                ("input[id$='chkRSNormalizing']", "#divRSNormalizing", "#divRSNormalizing textarea"),
                ("input[id$='chkRSSFT']", "#divRSSFT", "#divRSSFT textarea")
            };

            var strategyNotes = new[]
            {
                "Noted ATP strengths",
                "S-ATP follow-up",
                "Feel Name & Tame notes",
                "Explore and Wonder summary",
                "Normalizing notes",
                "Solution-focused Talk outcome"
            };

            for (var i = 0; i < reflectiveCheckboxes.Length; i++)
            {
                var (checkboxSelector, containerSelector, textareaSelector) = reflectiveCheckboxes[i];
                var strategyNote = strategyNotes[i];

                var checkbox = cheersPane.FindElement(By.CssSelector(checkboxSelector));
                checkbox.Click();
                driver.WaitForUpdatePanel(5);
                driver.WaitForReady(5);
                Thread.Sleep(200);

                var container = WaitForVisibleElement(driver, containerSelector, 5)
                    ?? throw new InvalidOperationException($"Container '{containerSelector}' was not visible.");
                var textarea = container.FindElement(By.CssSelector("textarea"));
                WebElementHelper.SetInputValue(driver, textarea, strategyNote, $"Strategy note {i + 1}", triggerBlur: true);

                checkbox.Click();
                driver.WaitForUpdatePanel(5);
                driver.WaitForReady(5);
                Thread.Sleep(200);
            }

            _output.WriteLine("[PASS] Reflective strategy checkboxes revealed and accepted notes.");
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(8)]
        public void CheersTabCanBeSavedAfterAllInputs(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting CHEERS save test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);
            OpenExistingHomeVisitLog(driver);

            var cheersTabLink = driver.WaitforElementToBeInDOM(By.CssSelector("a#lnkcheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab link was not found.");
            cheersTabLink.Click();
            driver.WaitForReady(5);

            var cheersPane = driver.WaitforElementToBeInDOM(By.CssSelector("div#cheers"), 10)
                ?? throw new InvalidOperationException("CHEERS tab content was not found.");

            // Leverage earlier routines to populate textareas and reflective strategies
            FillCheersDropdowns(driver, cheersPane);
            FillReflectiveStrategies(driver, cheersPane);

            ClickSubmit(driver);
            driver.WaitForReady(10);
            Thread.Sleep(500);
            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.Contains("Partially Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("saved", toastMessage, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine("[PASS] CHEERS tab saved with Partially Saved toast.");
        }

        private static void FillCheersDropdowns(IPookieWebDriver driver, IWebElement cheersPane)
        {
            var autoTextCheckbox = cheersPane.FindElement(By.CssSelector("input[id$='chkCCIAutoText']"));
            if (!autoTextCheckbox.Selected)
            {
                autoTextCheckbox.Click();
                driver.WaitForUpdatePanel(5);
                driver.WaitForReady(5);
                Thread.Sleep(200);
            }

            var dropdownSelectors = new[]
            {
                ("select[id$='ddlCHEERSCues']", "textarea[id$='txtCHEERSCues']"),
                ("select[id$='ddlCHEERSHolding']", "textarea[id$='txtCHEERSHolding']"),
                ("select[id$='ddlCHEERSExpression']", "textarea[id$='txtCHEERSExpression']"),
                ("select[id$='ddlCHEERSEmpathy']", "textarea[id$='txtCHEERSEmpathy']"),
                ("select[id$='ddlCHEERSRhythmReciprocity']", "textarea[id$='txtCHEERSRhythmReciprocity']"),
                ("select[id$='ddlCHEERSSmiles']", "textarea[id$='txtCHEERSSmiles']")
            };

            foreach (var (dropdownSelector, textareaSelector) in dropdownSelectors)
            {
                var dropdown = cheersPane.FindElement(By.CssSelector(dropdownSelector));
                var textarea = cheersPane.FindElement(By.CssSelector(textareaSelector));

                var options = new SelectElement(dropdown).Options.Where(o => !string.IsNullOrWhiteSpace(o.Text) && !o.Text.Contains("--", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var option in options.Take(1))
                {
                    WebElementHelper.SelectDropdownOption(driver, dropdown, dropdownSelector, option.Text.Trim(), option.GetAttribute("value"));
                    driver.WaitForReady(1);
                    Thread.Sleep(150);
                    WebElementHelper.SetInputValue(driver, textarea, $"{option.Text} note saved.", $"CHEERS textarea {textareaSelector}", triggerBlur: true);
                }
            }
        }

        private static void FillReflectiveStrategies(IPookieWebDriver driver, IWebElement cheersPane)
        {
            var reflectiveCheckboxes = new[]
            {
                ("input[id$='chkRSATP']", "#divRSATP", "#divRSATP textarea", "ATP reflections saved."),
                ("input[id$='chkRSSATP']", "#divRSSATP", "#divRSSATP textarea", "S-ATP reflections saved."),
                ("input[id$='chkRSFFF']", "#divRSFFF", "#divRSFFF textarea", "Feel Name & Tame saved."),
                ("input[id$='chkRSEW']", "#divRSEW", "#divRSEW textarea", "Explore & Wonder saved."),
                ("input[id$='chkRSNormalizing']", "#divRSNormalizing", "#divRSNormalizing textarea", "Normalizing saved."),
                ("input[id$='chkRSSFT']", "#divRSSFT", "#divRSSFT textarea", "Solution-focused Talk saved.")
            };

            foreach (var (checkboxSelector, containerSelector, textareaSelector, note) in reflectiveCheckboxes)
            {
                var checkbox = cheersPane.FindElement(By.CssSelector(checkboxSelector));
                if (!checkbox.Selected)
                {
                    checkbox.Click();
                    driver.WaitForUpdatePanel(5);
                    driver.WaitForReady(5);
                    Thread.Sleep(200);
                }

                var container = WaitForVisibleElement(driver, containerSelector, 5)
                    ?? throw new InvalidOperationException($"Container '{containerSelector}' was not visible.");
                var textarea = container.FindElement(By.CssSelector("textarea"));
                WebElementHelper.SetInputValue(driver, textarea, note, $"Strategy note {checkboxSelector}", triggerBlur: true);
            }
        }

        private static IWebElement? WaitForVisibleElement(IPookieWebDriver driver, string selector, int timeoutSeconds)
        {
            var end = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now <= end)
            {
                var element = driver.FindElements(By.CssSelector(selector)).FirstOrDefault();
                if (element != null && element.Displayed)
                {
                    return element;
                }

                Thread.Sleep(100);
            }

            return null;
        }
    }
}

