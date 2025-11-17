using System;
using System.Collections.Generic;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.ScreenForms
{
    public class ScreenFormsTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public ScreenFormsTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        private void LoginAndNavigateToReferrals(IPookieWebDriver driver)
        {
            _output.WriteLine($"Navigating to application URL: {_config.AppUrl}");
            driver.Navigate().GoToUrl(_config.AppUrl);
            driver.WaitForReady(30);

            _output.WriteLine($"Signing in with user: {_config.UserName}");
            var loginPage = new LoginPage(driver);
            loginPage.SignIn(_config.UserName, _config.Password);

            var isSignedIn = loginPage.IsSignedIn();
            Assert.True(isSignedIn, "User was not signed in successfully.");
            _output.WriteLine("[PASS] Successfully signed in");

            _output.WriteLine("Selecting DataEntry role...");
            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");

            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded, "Landing page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully selected DataEntry role");

            _output.WriteLine("Navigating to Referrals page...");
            var referralsLink = driver.FindElements(OpenQA.Selenium.By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);

            Assert.NotNull(referralsLink);
            referralsLink.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);

            _output.WriteLine("[PASS] Successfully navigated to Referrals page");
            _output.WriteLine($"Current URL: {driver.Url}");
        }

        private OpenQA.Selenium.IWebElement FindReferralsWaitingScreenTable(IPookieWebDriver driver)
        {
            var waitingTable = driver.FindElements(OpenQA.Selenium.By.CssSelector("table[id*='grReferralsWaitingScreen']"))
                .FirstOrDefault(el => el.Displayed);

            if (waitingTable != null)
            {
                _output.WriteLine("[INFO] Found waiting screen table by ID contains 'grReferralsWaitingScreen'");
                return waitingTable;
            }

            waitingTable = driver.FindElements(OpenQA.Selenium.By.CssSelector(".table.table-condensed.table-responsive.dataTable"))
                .FirstOrDefault(el => el.Displayed && LooksLikeWaitingScreenTable(el));

            if (waitingTable != null)
            {
                _output.WriteLine("[INFO] Found waiting screen table via fallback selector");
                return waitingTable;
            }

            throw new InvalidOperationException("Unable to locate the Referrals Waiting Screen table.");
        }

        private static bool LooksLikeWaitingScreenTable(OpenQA.Selenium.IWebElement table)
        {
            var id = table.GetAttribute("id") ?? string.Empty;
            var className = table.GetAttribute("class") ?? string.Empty;

            if (id.Contains("WaitingScreen", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (className.Contains("Referrals", StringComparison.OrdinalIgnoreCase) &&
                className.Contains("Waiting", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return table.Text?.Contains("Create Screen Form", StringComparison.OrdinalIgnoreCase) == true;
        }

        private OpenQA.Selenium.IWebElement FindCreateScreenLink(OpenQA.Selenium.IWebElement tableRow)
        {
            if (tableRow == null)
            {
                throw new ArgumentNullException(nameof(tableRow));
            }

            var link = tableRow.FindElements(OpenQA.Selenium.By.CssSelector("a[id*='lnkCreateScreen'], a.btn.btn-default"))
                .FirstOrDefault(a =>
                {
                    var text = a.Text?.Trim() ?? string.Empty;
                    return a.Displayed &&
                           text.IndexOf("Create Screen Form", StringComparison.OrdinalIgnoreCase) >= 0;
                });

            if (link != null)
            {
                _output.WriteLine($"[INFO] Found 'Create Screen Form' link: id='{link.GetAttribute("id")}'");
                return link;
            }

            throw new InvalidOperationException("Unable to locate the 'Create Screen Form' link within the row.");
        }

        private void LogScreenFormPageContents(IPookieWebDriver driver)
        {
            _output.WriteLine("\n========================================");
            _output.WriteLine("SCREEN FORM PAGE DETAILS");
            _output.WriteLine("========================================");

            var headings = driver.FindElements(OpenQA.Selenium.By.CssSelector("h1, h2, h3, .page-header, .panel-title"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .Distinct()
                .Take(3)
                .ToList();

            if (headings.Any())
            {
                _output.WriteLine("Headings:");
                foreach (var heading in headings)
                {
                    _output.WriteLine($"  - {heading}");
                }
            }
            else
            {
                _output.WriteLine("Headings: (none found)");
            }

            var summarySelectors = new[]
            {
                ".panel-body p",
                ".panel-body span",
                ".card-body p",
                ".card-body span",
                ".form-group",
                ".list-group-item",
                ".details-row"
            };

            var summaryTexts = summarySelectors
                .SelectMany(selector => driver.FindElements(OpenQA.Selenium.By.CssSelector(selector)))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .Distinct()
                .Take(8)
                .ToList();

            if (summaryTexts.Any())
            {
                _output.WriteLine("\nSummary snippets:");
                foreach (var text in summaryTexts)
                {
                    var clipped = text.Length > 200 ? text[..200] + "..." : text;
                    _output.WriteLine($"  • {clipped}");
                }
            }
            else
            {
                _output.WriteLine("\nSummary snippets: (none found)");
            }

            var dataTable = driver.FindElements(OpenQA.Selenium.By.CssSelector("table.table, table.dataTable"))
                .FirstOrDefault(tbl => tbl.Displayed);

            if (dataTable != null)
            {
                _output.WriteLine("\nFirst visible table contents:");

                var headers = dataTable.FindElements(OpenQA.Selenium.By.CssSelector("thead th"))
                    .Select(th => th.Text?.Trim() ?? string.Empty)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToList();

                if (headers.Any())
                {
                    _output.WriteLine($"  Headers: {string.Join(" | ", headers)}");
                }

                var dataRows = dataTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"))
                    .Where(row => row.Displayed && !row.Text.Contains("No data available", StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();

                if (dataRows.Any())
                {
                    var rowIndex = 1;
                    foreach (var row in dataRows)
                    {
                        var cellTexts = row.FindElements(OpenQA.Selenium.By.CssSelector("th, td"))
                            .Select(td => td.Text?.Trim() ?? string.Empty)
                            .Where(text => !string.IsNullOrWhiteSpace(text))
                            .ToList();

                        _output.WriteLine($"  Row {rowIndex++}: {string.Join(" | ", cellTexts)}");
                    }
                }
                else
                {
                    _output.WriteLine("  Table has no data rows to display.");
                }
            }
            else
            {
                _output.WriteLine("\nNo visible tables found on the screen form page.");
            }
        }

        private void NavigateToScreenFormPage(IPookieWebDriver driver)
        {
            LoginAndNavigateToReferrals(driver);

            var waitingTable = FindReferralsWaitingScreenTable(driver);
            var firstRowWithCreateLink = waitingTable
                .FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"))
                .FirstOrDefault(row => row.Displayed &&
                                       row.FindElements(OpenQA.Selenium.By.CssSelector("a[id*='lnkCreateScreen']"))
                                           .Any(a => a.Displayed));

            Assert.NotNull(firstRowWithCreateLink);
            _output.WriteLine("[PASS] Found table row with 'Create Screen Form' link");

            var createScreenLink = FindCreateScreenLink(firstRowWithCreateLink);
            var linkHref = createScreenLink.GetAttribute("href") ?? string.Empty;
            _output.WriteLine($"[INFO] Link href: {linkHref}");

            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", createScreenLink);
            System.Threading.Thread.Sleep(300);

            if (createScreenLink.Displayed)
            {
                createScreenLink.Click();
            }
            else
            {
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", createScreenLink);
            }

            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(1500);

            var currentUrl = driver.Url;
            var pageTitle = driver.Title;

            _output.WriteLine($"[INFO] Navigated to URL: {currentUrl}");
            _output.WriteLine($"[INFO] Page title: {pageTitle}");

            Assert.Contains("HVScreen.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ReferralFK", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] 'Create Screen Form' link opened HVScreen page successfully");
        }

        private OpenQA.Selenium.IWebElement? TryFindTabLink(IPookieWebDriver driver, string tabKey)
        {
            var selectors = new[]
            {
                $"a#{tabKey}",
                $"a[href='#{tabKey}']",
                $"li#{tabKey} a",
                $"a[id='{tabKey}']",
                $"a[id*='{tabKey}']"
            };

            foreach (var selector in selectors)
            {
                var tabLink = driver.FindElements(OpenQA.Selenium.By.CssSelector(selector))
                    .FirstOrDefault(el => el.Displayed);

                if (tabLink != null)
                {
                    return tabLink;
                }
            }

            return null;
        }

        private void SwitchToScreenFormTab(IPookieWebDriver driver, string tabKey, string tabDescription)
        {
            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var tabLink = TryFindTabLink(driver, tabKey);
                if (tabLink == null)
                {
                    if (attempt == maxAttempts)
                    {
                        throw new InvalidOperationException($"Unable to locate tab link for '{tabDescription}'.");
                    }

                    System.Threading.Thread.Sleep(300);
                    continue;
                }

                _output.WriteLine($"[INFO] Switching to '{tabDescription}' tab (attempt {attempt})");
                var js = (OpenQA.Selenium.IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", tabLink);
                js.ExecuteScript("window.scrollBy(0, -200);");
                System.Threading.Thread.Sleep(200);

                try
                {
                    tabLink.Click();
                    driver.WaitForReady(10);
                    System.Threading.Thread.Sleep(300);
                    return;
                }
                catch (OpenQA.Selenium.ElementClickInterceptedException)
                {
                    _output.WriteLine("[WARN] Tab click intercepted, invoking JavaScript click");
                    js.ExecuteScript("arguments[0].click();", tabLink);
                    driver.WaitForReady(10);
                    System.Threading.Thread.Sleep(300);
                    return;
                }
                catch (OpenQA.Selenium.StaleElementReferenceException)
                {
                    _output.WriteLine("[WARN] Tab element went stale while clicking, retrying...");
                }
            }
        }

        private OpenQA.Selenium.IWebElement FindScreenFormSubmitButton(IPookieWebDriver driver, int timeoutSeconds = 5)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now < endTime)
            {
                var submitButton = driver.FindElements(OpenQA.Selenium.By.CssSelector("a[id*='btnSubmit'], button[id*='btnSubmit'], input[id*='btnSubmit']"))
                    .FirstOrDefault(el => el.Displayed && el.Enabled);

                if (submitButton != null)
                {
                    return submitButton;
                }

                System.Threading.Thread.Sleep(250);
            }

            throw new InvalidOperationException("Unable to locate the screen form Submit button.");
        }

        private void ClickScreenFormSubmit(IPookieWebDriver driver)
        {
            var submitButton = FindScreenFormSubmitButton(driver);
            _output.WriteLine("[INFO] Clicking Screen Form Submit button");
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", submitButton);
            System.Threading.Thread.Sleep(300);
            try
            {
                submitButton.Click();
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException)
            {
                _output.WriteLine("[WARN] Submit button click intercepted, invoking JavaScript click");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", submitButton);
            }
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(500);
        }

        private HashSet<string> GetScreenFormValidationMessages(IPookieWebDriver driver)
        {
            var selectors = new[]
            {
                ".validation-summary-errors li",
                ".alert",
                ".alert-danger",
                ".text-danger",
                "span[style*='color: red']",
                "[id*='rv']",
                "[id*='rfv']"
            };

            var messages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var selector in selectors)
            {
                var elements = driver.FindElements(OpenQA.Selenium.By.CssSelector(selector));
                foreach (var element in elements)
                {
                    if (!element.Displayed)
                    {
                        continue;
                    }

                    var text = element.Text ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrWhiteSpace(line));

                    foreach (var line in lines)
                    {
                        messages.Add(line);
                    }
                }
            }

            _output.WriteLine($"\n[INFO] Validation messages ({messages.Count}):");
            foreach (var message in messages)
            {
                _output.WriteLine($"  - {message}");
            }

            return messages;
        }

        private string FillScreeningDate(IPookieWebDriver driver, OpenQA.Selenium.IJavaScriptExecutor js)
        {
            var dateField = driver.FindElement(OpenQA.Selenium.By.CssSelector("input[id$='txtScreenDate']"));
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -120);", dateField);
            System.Threading.Thread.Sleep(200);
            dateField.Click();
            System.Threading.Thread.Sleep(100);
            dateField.Clear();

            var screeningDate = "11/17/25";
            dateField.SendKeys(screeningDate);
            js.ExecuteScript("arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", dateField);
            _output.WriteLine($"[PASS] Entered Screening Date: {screeningDate}");

            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(500);

            return screeningDate;
        }

        private string FillExpectedDueDate(IPookieWebDriver driver, OpenQA.Selenium.IJavaScriptExecutor js)
        {
            var dueDateField = driver.FindElement(OpenQA.Selenium.By.CssSelector("input[id$='txtEDC']"));
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -120);", dueDateField);
            System.Threading.Thread.Sleep(200);
            dueDateField.Click();
            System.Threading.Thread.Sleep(100);
            dueDateField.Clear();

            var dueDate = "11/01/25";
            dueDateField.SendKeys(dueDate);
            js.ExecuteScript("arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", dueDateField);
            _output.WriteLine($"[PASS] Entered Expected Due Date: {dueDate}");

            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(500);

            return dueDate;
        }

        private void SelectReferralMadeOption(IPookieWebDriver driver, OpenQA.Selenium.IJavaScriptExecutor js, string value)
        {
            var referralDropdown = driver.FindElement(OpenQA.Selenium.By.CssSelector("select[id$='ddlReferralMade']"));
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -120);", referralDropdown);
            System.Threading.Thread.Sleep(200);

            var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(referralDropdown);
            try
            {
                selectElement.SelectByValue(value);
            }
            catch (OpenQA.Selenium.ElementNotInteractableException)
            {
                _output.WriteLine("[WARN] Referral Made dropdown not interactable, selecting via JavaScript");
                js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", referralDropdown, value);
            }

            _output.WriteLine($"[PASS] Selected Referral Made option value {value}");
            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(500);
        }

        private System.Collections.Generic.List<string> WaitForSuccessToast(IPookieWebDriver driver, int timeoutSeconds = 15)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now < endTime)
            {
                // Look specifically for jq-toast-single elements (the actual toast notifications)
                var toastElements = driver.FindElements(OpenQA.Selenium.By.CssSelector(".jq-toast-single"));
                
                foreach (var toast in toastElements)
                {
                    try
                    {
                        // Check if toast has success icon
                        if (toast.GetAttribute("class").Contains("jq-icon-success"))
                        {
                            var heading = "";
                            var body = "";
                            
                            try
                            {
                                var headingElement = toast.FindElement(OpenQA.Selenium.By.CssSelector(".jq-toast-heading"));
                                heading = headingElement.Text.Trim();
                            }
                            catch { }
                            
                            // Get full text and remove heading to get body
                            body = toast.Text.Trim();
                            if (!string.IsNullOrEmpty(heading))
                            {
                                body = body.Replace(heading, "").Trim();
                                // Remove the close button 'x' if present
                                body = body.TrimStart('×').Trim();
                            }
                            
                            _output.WriteLine($"[INFO] Success toast detected!");
                            _output.WriteLine($"[INFO] Toast heading: '{heading}'");
                            _output.WriteLine($"[INFO] Toast body: '{body}'");
                            
                            return new System.Collections.Generic.List<string> { heading, body };
                        }
                    }
                    catch (OpenQA.Selenium.StaleElementReferenceException)
                    {
                        continue;
                    }
                }

                System.Threading.Thread.Sleep(500);
            }

            _output.WriteLine($"[WARN] No toast notification detected within {timeoutSeconds} seconds.");
            
            // For debugging can be removed, but would be useful in case something changes
            try
            {
                var allAlerts = driver.FindElements(OpenQA.Selenium.By.CssSelector("[role='alert'], .alert, .jq-toast-single"));
                if (allAlerts.Any())
                {
                    _output.WriteLine($"[DEBUG] Found {allAlerts.Count} alert-like elements:");
                    foreach (var alert in allAlerts)
                    {
                        _output.WriteLine($"  - class='{alert.GetAttribute("class")}', text='{alert.Text}'");
                    }
                }
            }
            catch { }

            throw new TimeoutException("Timed out waiting for success toast notification.");
        }

        [Fact]
        public void ReferralsWaiting_ClickCreateScreenForm_NavigatesToHvScreen()
        {
            using var driver = _driverFactory.CreateDriver();

            NavigateToScreenFormPage(driver);
            LogScreenFormPageContents(driver);
        }

        [Fact]
        public void ScreenForm_CheckValidationMessages_UpdateAfterFieldsFilled()
        {
            using var driver = _driverFactory.CreateDriver();

            NavigateToScreenFormPage(driver);
            var js = (OpenQA.Selenium.IJavaScriptExecutor)driver;

            SwitchToScreenFormTab(driver, "risk", "Demographic Criteria");
            ClickScreenFormSubmit(driver);
            var initialMessages = GetScreenFormValidationMessages(driver);

            var expectedInitial = new[]
            {
                "Date of Screening is required.",
                "Q3 Required",
                "Primary Language spoken in the home is required.",
                "Question 15 is required",
                "Question 16 is required",
                "Question 17 is required"
            };

            foreach (var expected in expectedInitial)
            {
                Assert.Contains(expected, initialMessages);
            }
            _output.WriteLine("[PASS] Initial validation messages displayed as expected");

            SwitchToScreenFormTab(driver, "main", "Screen Information");
            FillScreeningDate(driver, js);

            SwitchToScreenFormTab(driver, "risk", "Demographic Criteria");
            ClickScreenFormSubmit(driver);
            var messagesAfterDate = GetScreenFormValidationMessages(driver);
            Assert.DoesNotContain("Date of Screening is required.", messagesAfterDate);
            Assert.Contains("Q3 Required", messagesAfterDate);
            Assert.Contains("Primary Language spoken in the home is required.", messagesAfterDate);
            _output.WriteLine("[PASS] Date validation removed while other validations remain");

            var primaryLanguageDropdown = driver.FindElement(OpenQA.Selenium.By.CssSelector("select[id$='ddlPrimaryLanguage']"));
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -120);", primaryLanguageDropdown);
            System.Threading.Thread.Sleep(200);
            var primaryLanguageSelect = new OpenQA.Selenium.Support.UI.SelectElement(primaryLanguageDropdown);
            try
            {
                primaryLanguageSelect.SelectByValue("01");
            }
            catch (OpenQA.Selenium.ElementNotInteractableException)
            {
                _output.WriteLine("[WARN] Primary Language dropdown not interactable, selecting via JavaScript");
                js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', {bubbles: true}));", primaryLanguageDropdown, "01");
            }
            _output.WriteLine("[PASS] Selected Primary Language (1. English)");

            ClickScreenFormSubmit(driver);
            var messagesAfterPrimary = GetScreenFormValidationMessages(driver);
            Assert.DoesNotContain("Primary Language spoken in the home is required.", messagesAfterPrimary);
            Assert.Contains("Q3 Required", messagesAfterPrimary);
            _output.WriteLine("[PASS] Primary Language validation removed while Q3 validation remains");

            SwitchToScreenFormTab(driver, "main", "Screen Information");
            var q3Dropdown = driver.FindElement(OpenQA.Selenium.By.CssSelector("select[id$='ddlRelation2TC']"));
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -120);", q3Dropdown);
            System.Threading.Thread.Sleep(200);
            var q3Select = new OpenQA.Selenium.Support.UI.SelectElement(q3Dropdown);
            try
            {
                q3Select.SelectByValue("01");
            }
            catch (OpenQA.Selenium.ElementNotInteractableException)
            {
                _output.WriteLine("[WARN] Q3 dropdown not interactable, selecting via JavaScript");
                js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', {bubbles: true}));", q3Dropdown, "01");
            }
            _output.WriteLine("[PASS] Selected Q3 (1. Mother)");

            SwitchToScreenFormTab(driver, "risk", "Demographic Criteria");
            ClickScreenFormSubmit(driver);
            var messagesAfterQ3 = GetScreenFormValidationMessages(driver);
            Assert.DoesNotContain("Q3 Required", messagesAfterQ3);
            Assert.Contains("Question 15 is required", messagesAfterQ3);
            Assert.Contains("Question 16 is required", messagesAfterQ3);
            Assert.Contains("Question 17 is required", messagesAfterQ3);
            _output.WriteLine("[PASS] Q3 validation removed while remaining validations persist");

            OpenQA.Selenium.IWebElement FindDropdownBySuffixes(params string[] suffixes)
            {
                foreach (var suffix in suffixes)
                {
                    var selector = $"select[id$='{suffix}']";
                    var element = driver.FindElements(OpenQA.Selenium.By.CssSelector(selector))
                        .FirstOrDefault();
                    if (element != null)
                    {
                        return element;
                    }
                }

                throw new InvalidOperationException($"Unable to locate dropdown with suffixes: {string.Join(", ", suffixes)}");
            }

            void SelectRiskDropdown(string[] suffixes, string description, string valueToSelect, string expectedRemovedMessage)
            {
                var dropdown = FindDropdownBySuffixes(suffixes);
                js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -120);", dropdown);
                System.Threading.Thread.Sleep(200);
                var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(dropdown);
                try
                {
                    selectElement.SelectByValue(valueToSelect);
                }
                catch (OpenQA.Selenium.ElementNotInteractableException)
                {
                    _output.WriteLine($"[WARN] {description} dropdown not interactable, selecting via JavaScript");
                    js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', {bubbles: true}));", dropdown, valueToSelect);
                }
                _output.WriteLine($"[PASS] Selected {description}: value {valueToSelect}");

                ClickScreenFormSubmit(driver);
                var currentMessages = GetScreenFormValidationMessages(driver);
                Assert.DoesNotContain(expectedRemovedMessage, currentMessages);
                _output.WriteLine($"[PASS] Validation '{expectedRemovedMessage}' cleared after selecting {description}");
            }

            SelectRiskDropdown(new[] { "ddlRiskNotMarried" }, "Risk (Not Married)", "0", "Question 15 is required");
            SelectRiskDropdown(new[] { "ddlRiskNoPrenatalCare" }, "Risk (No Prenatal Care)", "1", "Question 16 is required");
            SelectRiskDropdown(new[] { "ddlRiskPoor" }, "Risk (Poverty)", "9", "Question 17 is required");

            SwitchToScreenFormTab(driver, "main", "Screen Information");
            FillExpectedDueDate(driver, js);

            SwitchToScreenFormTab(driver, "risk", "Demographic Criteria");
            SelectReferralMadeOption(driver, js, "2");

            _output.WriteLine("[INFO] Performing final submit to save the screening form...");
            ClickScreenFormSubmit(driver);
            _output.WriteLine("[INFO] Waiting for success toast notification...");
            var toastMessages = WaitForSuccessToast(driver);
            foreach (var message in toastMessages)
            {
                _output.WriteLine($"[INFO] Toast message captured: '{message}'");
            }
            Assert.Contains(toastMessages, msg => msg.StartsWith("Screen Form Saved", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(toastMessages, msg => msg.Contains("has been saved successfully", StringComparison.OrdinalIgnoreCase));
            _output.WriteLine("[PASS] Success toast displayed:");
            foreach (var toast in toastMessages)
            {
                _output.WriteLine($"  - {toast}");
            }
        }
    }
}

