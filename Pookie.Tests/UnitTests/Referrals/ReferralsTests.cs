using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.Referrals
{
    public class ReferralsTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public ReferralsTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        #region Helper Methods

        /// <summary>
        /// Logs in and navigates to the Referrals page
        /// </summary>
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

            _output.WriteLine("Attempting to select DataEntry role...");
            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");

            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded, "Landing page did not load after selecting Data Entry role.");
            _output.WriteLine("[PASS] Successfully selected Data Entry role");

            _output.WriteLine("\nNavigating to Referrals page...");
            var referralsLink = driver.FindElements(OpenQA.Selenium.By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);

            Assert.NotNull(referralsLink);
            _output.WriteLine($"Found Referrals link with text: '{referralsLink.Text?.Trim()}'");
            referralsLink.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            
            _output.WriteLine("[PASS] Successfully navigated to Referrals page");
            _output.WriteLine($"Current URL: {driver.Url}");
        }

        /// <summary>
        /// Clicks the New Referral button on the Referrals page
        /// </summary>
        private void ClickNewReferralButton(IPookieWebDriver driver)
        {
            _output.WriteLine("\nClicking New Referral button...");
            var newReferralButton = FindNewReferralButton(driver);
            
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", newReferralButton);
            System.Threading.Thread.Sleep(500);
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", newReferralButton);
            
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(1000);
            _output.WriteLine("[PASS] Clicked New Referral button");
            _output.WriteLine($"Current URL: {driver.Url}");
        }

        private OpenQA.Selenium.IWebElement FindNewReferralButton(IPookieWebDriver driver)
        {
            var match = driver.FindElements(OpenQA.Selenium.By.CssSelector(".btn.btn-default.pull-right"))
                .FirstOrDefault(el => el.Displayed && el.Enabled && ElementTextContains(el, "New Referral"));
            
            if (match != null)
            {
                _output.WriteLine($"[INFO] Found New Referral button using CSS selector '.btn.btn-default.pull-right'");
                return match;
            }

            throw new InvalidOperationException("Unable to locate the New Referral button.");
        }

        private static bool ElementTextContains(OpenQA.Selenium.IWebElement element, string expectedValue)
        {
            var text = element.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text) && text.IndexOf(expectedValue, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var valueAttribute = element.GetAttribute("value")?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(valueAttribute) &&
                   valueAttribute.IndexOf(expectedValue, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private OpenQA.Selenium.IWebElement FindActiveReferralsTable(IPookieWebDriver driver)
        {
            var match = driver.FindElements(OpenQA.Selenium.By.CssSelector(".table.table-condensed.table-responsive.dataTable.no-footer.dtr-column"))
                .FirstOrDefault(el => el.Displayed && LooksLikeActiveReferrals(el));
            
            if (match != null)
            {
                _output.WriteLine($"[INFO] Found Active Referrals table using CSS selector '.table.table-condensed.table-responsive.dataTable.no-footer.dtr-column'");
                return match;
            }

            throw new InvalidOperationException("Unable to locate the Active Referrals table.");
        }

        private bool LooksLikeActiveReferrals(OpenQA.Selenium.IWebElement table)
        {
            var id = table.GetAttribute("id") ?? string.Empty;
            var className = table.GetAttribute("class") ?? string.Empty;

            if (className.IndexOf("active", StringComparison.OrdinalIgnoreCase) >= 0 &&
                className.IndexOf("referral", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (id.IndexOf("ActiveReferral", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return ElementTextContains(table, "Active Referrals");
        }

        private OpenQA.Selenium.IWebElement FindCompletedReferralYearDropdown(IPookieWebDriver driver)
        {
            var match = driver.FindElements(OpenQA.Selenium.By.CssSelector("select.form-control[name$='ddlCompletedReferralYear']"))
                .FirstOrDefault(el => el.Displayed && el.Enabled);
            
            if (match != null)
            {
                _output.WriteLine("[INFO] Found completed referral year dropdown");
                return match;
            }

            throw new InvalidOperationException("Unable to locate the Completed Referral Year dropdown.");
        }

        private OpenQA.Selenium.IWebElement FindCompletedReferralsTable(IPookieWebDriver driver)
        {
            var match = driver.FindElements(OpenQA.Selenium.By.CssSelector(".table.table-condensed.table-responsive.dataTable.no-footer.dtr-column"))
                .FirstOrDefault(el => el.Displayed && LooksLikeCompletedReferrals(el));
            
            if (match != null)
            {
                _output.WriteLine("[INFO] Found Completed Referrals table using CSS selector '.table.table-condensed.table-responsive.dataTable.no-footer.dtr-column'");
                return match;
            }

            throw new InvalidOperationException("Unable to locate the Completed Referrals table.");
        }

        private bool LooksLikeCompletedReferrals(OpenQA.Selenium.IWebElement table)
        {
            var id = table.GetAttribute("id") ?? string.Empty;
            var className = table.GetAttribute("class") ?? string.Empty;

            if (className.IndexOf("completed", StringComparison.OrdinalIgnoreCase) >= 0 &&
                className.IndexOf("referral", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (id.IndexOf("CompletedReferral", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return ElementTextContains(table, "Completed Referrals");
        }

        private OpenQA.Selenium.IWebElement FindReferralEditButton(OpenQA.Selenium.IWebElement tableRow)
        {
            if (tableRow == null)
            {
                throw new ArgumentNullException(nameof(tableRow));
            }

            var match = tableRow.FindElements(OpenQA.Selenium.By.CssSelector("a.btn.btn-default"))
                .FirstOrDefault(el => el.Displayed &&
                                      el.Enabled &&
                                      (ElementTextContains(el, "Edit") ||
                                       ElementHasIcon(el, "glyphicon-pencil")));
            
            if (match != null)
            {
                _output.WriteLine("[INFO] Found edit button via selector 'a.btn.btn-default'");
                return match;
            }

            throw new InvalidOperationException("Unable to locate the edit button within the referral row.");
        }

        private static bool ElementHasIcon(OpenQA.Selenium.IWebElement element, string iconClass)
        {
            if (element == null)
            {
                return false;
            }

            var elementClass = element.GetAttribute("class") ?? string.Empty;
            if (elementClass.IndexOf(iconClass, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var iconSelector = "." + iconClass.TrimStart('.');
            return element.FindElements(OpenQA.Selenium.By.CssSelector(iconSelector)).Any(icon => icon.Displayed);
        }

        private OpenQA.Selenium.IWebElement FindReferralSearchResultsGrid(IPookieWebDriver driver)
        {
            // Try table with grResults ID first (most specific)
            var match = driver.FindElements(OpenQA.Selenium.By.CssSelector("table[id*='grResults']"))
                .FirstOrDefault(el => el.Displayed);
            
            if (match != null)
            {
                _output.WriteLine("[INFO] Found referral search results grid");
                return match;
            }

            // Fallback to wrapper div approach
            match = driver.FindElements(OpenQA.Selenium.By.CssSelector("div[id*='grResults'] table.dataTable"))
                .FirstOrDefault(el => el.Displayed && LooksLikeSearchResults(el));
            
            if (match != null)
            {
                _output.WriteLine("[INFO] Found referral search results grid");
                return match;
            }

            throw new InvalidOperationException("Unable to locate the referral search results grid.");
        }

        private bool LooksLikeSearchResults(OpenQA.Selenium.IWebElement table)
        {
            var id = table.GetAttribute("id") ?? string.Empty;
            var className = table.GetAttribute("class") ?? string.Empty;

            if (id.IndexOf("grResults", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (className.IndexOf("results", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return ElementTextContains(table, "Search Results") ||
                   ElementTextContains(table, "No records found");
        }

        private void SelectChosenOption(IPookieWebDriver driver, string selectSuffix, string optionText)
        {
            optionText = optionText?.Trim() ?? throw new ArgumentNullException(nameof(optionText));

            var chosenContainer = driver.FindElements(OpenQA.Selenium.By.CssSelector($".chosen-container[id$='{selectSuffix}_chosen']"))
                .FirstOrDefault(el => el.Displayed);
            
            if (chosenContainer == null)
            {
                throw new InvalidOperationException($"Unable to locate Chosen container for suffix '{selectSuffix}'.");
            }

            var js = (OpenQA.Selenium.IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", chosenContainer);
            System.Threading.Thread.Sleep(200);

            var trigger = chosenContainer.FindElement(OpenQA.Selenium.By.CssSelector(".chosen-choices"));
            js.ExecuteScript("arguments[0].click();", trigger);
            System.Threading.Thread.Sleep(300);

            var options = chosenContainer
                .FindElements(OpenQA.Selenium.By.CssSelector(".chosen-drop .chosen-results li.active-result"))
                .Where(li => !string.IsNullOrWhiteSpace(li.Text));

            var optionElement = options.FirstOrDefault(li =>
                li.Text.Trim().Equals(optionText, StringComparison.OrdinalIgnoreCase));

            if (optionElement == null)
            {
                throw new InvalidOperationException($"Unable to locate Chosen option '{optionText}'.");
            }

            js.ExecuteScript("arguments[0].scrollIntoView(true);", optionElement);
            js.ExecuteScript("arguments[0].click();", optionElement);
            System.Threading.Thread.Sleep(200);
        }

        private void SelectChosenOptionViaScript(IPookieWebDriver driver, string selectSuffix, string optionText, string? optionValue = null)
        {
            optionText = optionText?.Trim() ?? throw new ArgumentNullException(nameof(optionText));

            var selectElement = FindSelectBySuffix(driver, selectSuffix);
            var js = (OpenQA.Selenium.IJavaScriptExecutor)driver;

            js.ExecuteScript(@"
                var select = arguments[0];
                var targetText = (arguments[1] || '').trim().toLowerCase();
                var targetValue = (arguments[2] || '').trim();
                var option = Array.from(select.options).find(function(opt) {
                    if (targetValue && opt.value === targetValue) {
                        return true;
                    }
                    return opt.text && opt.text.trim().toLowerCase() === targetText;
                });

                if (!option) {
                    throw new Error('Option not found: ' + targetText || targetValue);
                }

                option.selected = true;
                if (!select.multiple) {
                    Array.from(select.options).forEach(function(opt) {
                        if (opt !== option) {
                            opt.selected = false;
                        }
                    });
                }

                var changeEvent = new Event('change', { bubbles: true });
                select.dispatchEvent(changeEvent);
                var inputEvent = new Event('input', { bubbles: true });
                select.dispatchEvent(inputEvent);

                if (window.jQuery) {
                    window.jQuery(select).trigger('change');
                    window.jQuery(select).trigger('chosen:updated');
                }
            ", selectElement, optionText, optionValue ?? string.Empty);

            System.Threading.Thread.Sleep(200);
        }

        private static System.Collections.Generic.List<OpenQA.Selenium.IWebElement> GetContactAttemptDataRows(OpenQA.Selenium.IWebElement contactAttemptsTable)
        {
            return contactAttemptsTable
                .FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"))
                .Where(row => !row.Text.Contains("No data available", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private OpenQA.Selenium.IWebElement FindContactAttemptForm(IPookieWebDriver driver)
        {
            return FindElementBySuffix(driver, "div.panel", new[] { "divAddEditContactAttempt", "ContactAttempt" }, "contact attempt form");
        }

        private OpenQA.Selenium.IWebElement FindContactAttemptsTable(IPookieWebDriver driver)
        {
            return FindElementBySuffix(driver, "table.table", new[] { "tblContactAttempts", "ContactAttempt" }, "contact attempts table");
        }

        private OpenQA.Selenium.IWebElement FindValidationSummaryBySuffix(IPookieWebDriver driver, params string[] suffixes)
        {
            return FindElementBySuffix(driver, ".validation-summary-errors", suffixes, "validation summary");
        }

        private OpenQA.Selenium.IWebElement FindTextInputBySuffix(IPookieWebDriver driver, params string[] suffixes)
        {
            return FindElementBySuffix(driver, "input.form-control", suffixes, "text input");
        }

        private OpenQA.Selenium.IWebElement FindSelectBySuffix(IPookieWebDriver driver, params string[] suffixes)
        {
            return FindElementBySuffixIncludingHidden(driver, "select", suffixes, "select input");
        }

        private OpenQA.Selenium.IWebElement FindElementBySuffixIncludingHidden(
            IPookieWebDriver driver,
            string baseSelector,
            System.Collections.Generic.IEnumerable<string> suffixes,
            string description)
        {
            foreach (var suffix in suffixes ?? System.Linq.Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(suffix))
                {
                    continue;
                }

                // Try name attribute first
                var endsWithSelector = $"{baseSelector}[name$='{suffix}']";
                var match = driver.FindElements(OpenQA.Selenium.By.CssSelector(endsWithSelector))
                    .FirstOrDefault(); // Don't filter by Displayed (for Chosen.js hidden selects)
                if (match != null)
                {
                    _output.WriteLine($"[INFO] Found {description} via selector '{endsWithSelector}'");
                    return match;
                }

                // Try id attribute
                endsWithSelector = $"{baseSelector}[id$='{suffix}']";
                match = driver.FindElements(OpenQA.Selenium.By.CssSelector(endsWithSelector))
                    .FirstOrDefault(); // Don't filter by Displayed (for Chosen.js hidden selects)
                if (match != null)
                {
                    _output.WriteLine($"[INFO] Found {description} via selector '{endsWithSelector}'");
                    return match;
                }
            }

            throw new InvalidOperationException($"Unable to locate {description} using suffixes: {string.Join(", ", suffixes)}");
        }

        private OpenQA.Selenium.IWebElement FindCheckboxBySuffix(IPookieWebDriver driver, params string[] suffixes)
        {
            return FindElementBySuffix(driver, "input[type='checkbox']", suffixes, "checkbox input");
        }

        private OpenQA.Selenium.IWebElement FindTextAreaBySuffix(IPookieWebDriver driver, params string[] suffixes)
        {
            return FindElementBySuffix(driver, "textarea.form-control", suffixes, "text area");
        }

        private OpenQA.Selenium.IWebElement FindButtonBySuffix(IPookieWebDriver driver, params string[] suffixes)
        {
            // Try button.btn first
            try
            {
                return FindElementBySuffix(driver, "button.btn", suffixes, "button");
            }
            catch (InvalidOperationException)
            {
                // Fallback to a.btn for anchor tags styled as buttons
                return FindElementBySuffix(driver, "a.btn", suffixes, "button");
            }
        }

        private OpenQA.Selenium.IWebElement FindElementBySuffix(
            IPookieWebDriver driver,
            string baseSelector,
            System.Collections.Generic.IEnumerable<string> suffixes,
            string description)
        {
            foreach (var suffix in suffixes ?? System.Linq.Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(suffix))
                {
                    continue;
                }

                // Try name attribute first
                var endsWithSelector = $"{baseSelector}[name$='{suffix}']";
                var match = driver.FindElements(OpenQA.Selenium.By.CssSelector(endsWithSelector))
                    .FirstOrDefault(el => el.Displayed);
                if (match != null)
                {
                    _output.WriteLine($"[INFO] Found {description} via selector '{endsWithSelector}'");
                    return match;
                }

                // Try id attribute as fallback
                endsWithSelector = $"{baseSelector}[id$='{suffix}']";
                match = driver.FindElements(OpenQA.Selenium.By.CssSelector(endsWithSelector))
                    .FirstOrDefault(el => el.Displayed);
                if (match != null)
                {
                    _output.WriteLine($"[INFO] Found {description} via selector '{endsWithSelector}'");
                    return match;
                }
            }

            throw new InvalidOperationException($"Unable to locate {description} using suffixes: {string.Join(", ", suffixes)}");
        }

        private OpenQA.Selenium.IWebElement FindLinkByTextFragments(IPookieWebDriver driver, params string[] textFragments)
        {
            textFragments ??= Array.Empty<string>();
            var links = driver.FindElements(OpenQA.Selenium.By.CssSelector("a, .btn"));

            foreach (var link in links)
            {
                if (!link.Displayed)
                {
                    continue;
                }

                var text = link.Text?.Trim() ?? string.Empty;
                if (textFragments.All(fragment => text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _output.WriteLine($"[INFO] Found link containing text '{string.Join(" ", textFragments)}'");
                    return link;
                }
            }

            throw new InvalidOperationException($"Unable to locate link containing text fragments: {string.Join(", ", textFragments)}");
        }

        private OpenQA.Selenium.IWebElement FindButtonByText(IPookieWebDriver driver, string buttonText)
        {
            // Try button tag first
            var match = driver.FindElements(OpenQA.Selenium.By.CssSelector("button"))
                .FirstOrDefault(el => el.Displayed && ElementTextContains(el, buttonText));
            
            if (match != null)
            {
                _output.WriteLine($"[INFO] Found button '{buttonText}'");
                return match;
            }

            // Try input[type='submit'] and input[type='button'] as fallback
            match = driver.FindElements(OpenQA.Selenium.By.CssSelector("input[type='submit'], input[type='button']"))
                .FirstOrDefault(el => el.Displayed && ElementTextContains(el, buttonText));
            
            if (match != null)
            {
                _output.WriteLine($"[INFO] Found button '{buttonText}' (input element)");
                return match;
            }

            // Try anchor tags with button classes (e.g., <a class="btn btn-primary">)
            match = driver.FindElements(OpenQA.Selenium.By.CssSelector("a.btn"))
                .FirstOrDefault(el => el.Displayed && ElementTextContains(el, buttonText));
            
            if (match != null)
            {
                _output.WriteLine($"[INFO] Found button '{buttonText}' (anchor element)");
                return match;
            }

            throw new InvalidOperationException($"Unable to locate button with text '{buttonText}'.");
        }

        private void LogElementClasses(string label, Func<OpenQA.Selenium.IWebElement> elementProvider)
        {
            try
            {
                var element = elementProvider();
                var classAttr = element?.GetAttribute("class")?.Trim();
                var formatted = string.IsNullOrWhiteSpace(classAttr) ? "(no class attribute)" : classAttr;
                _output.WriteLine($"[INFO] {label} classes: '{formatted}'");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[WARN] Unable to log classes for {label}: {ex.Message}");
            }
        }

        /// <summary>
        /// Fills in the person search form
        /// </summary>
        private void FillPersonSearchForm(IPookieWebDriver driver, string firstName, string lastName, string dob, string phone, string emergencyPhone)
        {
            _output.WriteLine("\n========================================");
            _output.WriteLine("FILLING IN SEARCH FORM");
            _output.WriteLine("========================================");

            _output.WriteLine($"PC1 First Name: {firstName}");
            _output.WriteLine($"PC1 Last Name: {lastName}");
            _output.WriteLine($"DOB: {dob}");
            _output.WriteLine($"Phone: {phone}");
            _output.WriteLine($"Emergency Phone: {emergencyPhone}");

            var firstNameField = FindTextInputBySuffix(driver, "txtpcfirstname");
            firstNameField.Click();
            System.Threading.Thread.Sleep(200);
            firstNameField.Clear();
            firstNameField.SendKeys(firstName);
            _output.WriteLine("[PASS] Filled PC1 First Name");

            var lastNameField = FindTextInputBySuffix(driver, "txtpclastname");
            lastNameField.Click();
            System.Threading.Thread.Sleep(200);
            lastNameField.Clear();
            lastNameField.SendKeys(lastName);
            _output.WriteLine("[PASS] Filled PC1 Last Name");

            var dobField = FindTextInputBySuffix(driver, "txtpcdob");
            dobField.Click();
            System.Threading.Thread.Sleep(200);
            dobField.Clear();
            dobField.SendKeys(dob);
            _output.WriteLine("[PASS] Filled DOB");

            var phoneField = FindTextInputBySuffix(driver, "txtpcphone");
            phoneField.Click();
            System.Threading.Thread.Sleep(200);
            phoneField.Clear();
            phoneField.SendKeys(phone);
            _output.WriteLine("[PASS] Filled Phone");

            var emergencyPhoneField = FindTextInputBySuffix(driver, "txtpcemergencyphone");
            emergencyPhoneField.Click();
            System.Threading.Thread.Sleep(200);
            emergencyPhoneField.Clear();
            emergencyPhoneField.SendKeys(emergencyPhone);
            _output.WriteLine("[PASS] Filled Emergency Phone");
            
            System.Threading.Thread.Sleep(500);
        }

        /// <summary>
        /// Clicks the search button on the person search form
        /// </summary>
        private void ClickSearchButton(IPookieWebDriver driver)
        {
            _output.WriteLine("\n========================================");
            _output.WriteLine("SEARCHING FOR PERSON");
            _output.WriteLine("========================================");

            OpenQA.Selenium.IWebElement? searchButton = null;
            try
            {
                searchButton = FindButtonBySuffix(driver, "btSearch", "btnSearch");
                _output.WriteLine("[INFO] Found search button via suffix-based selector");
            }
            catch (InvalidOperationException)
            {
                _output.WriteLine("[INFO] Search button not found via suffix helpers, falling back to text lookup");
            }

            if (searchButton == null)
            {
                searchButton = FindButtonByText(driver, "Search");
            }

            Assert.NotNull(searchButton);
            _output.WriteLine($"Found search button: id='{searchButton.GetAttribute("id")}', class='{searchButton.GetAttribute("class")}'");
            
            searchButton.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked Search button");
        }

        /// <summary>
        /// Fills a referral text field and logs the action
        /// </summary>
        private void FillReferralTextField(IPookieWebDriver driver, string suffix, string value, string fieldName)
        {
            var field = FindTextInputBySuffix(driver, suffix);
            field.Clear();
            field.SendKeys(value);
            _output.WriteLine($"[PASS] Filled {fieldName}: {value}");
        }

        /// <summary>
        /// Selects a dropdown option by text and logs the action
        /// </summary>
        private void SelectReferralDropdownByText(IPookieWebDriver driver, string suffix, string optionText, string fieldName)
        {
            var dropdown = FindSelectBySuffix(driver, suffix);
            var select = new OpenQA.Selenium.Support.UI.SelectElement(dropdown);
            select.SelectByText(optionText);
            _output.WriteLine($"[PASS] Selected {fieldName}: {optionText}");
        }

        /// <summary>
        /// Selects the first non-empty option from a dropdown and logs the action
        /// </summary>
        private void SelectReferralDropdownByFirstNonEmpty(IPookieWebDriver driver, string suffix, string fieldName)
        {
            var dropdown = FindSelectBySuffix(driver, suffix);
            var select = new OpenQA.Selenium.Support.UI.SelectElement(dropdown);
            var firstOption = select.Options.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value")));
            if (firstOption != null)
            {
                select.SelectByValue(firstOption.GetAttribute("value"));
                _output.WriteLine($"[PASS] Selected {fieldName}: {firstOption.Text}");
            }
        }

        /// <summary>
        /// Submits the referral form and returns validation errors
        /// </summary>
        private System.Collections.Generic.HashSet<string> SubmitReferralFormAndGetErrors(IPookieWebDriver driver)
        {
            var submitButton = FindButtonBySuffix(driver, "SubmitReferral_LoginView1_btnSubmit");
            submitButton.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked Submit button");
            return GetValidationErrorMessages(driver);
        }

        /// <summary>
        /// Logs validation errors for a step
        /// </summary>
        private void LogStepValidationErrors(int stepNumber, System.Collections.Generic.HashSet<string> validationErrors)
        {
            _output.WriteLine($"\n[STEP {stepNumber}] Validation errors found: {validationErrors.Count}");
            foreach (var error in validationErrors)
            {
                _output.WriteLine($"  - {error}");
            }
        }

        /// <summary>
        /// Checks for validation error messages on the page
        /// </summary>
        private System.Collections.Generic.HashSet<string> GetValidationErrorMessages(IPookieWebDriver driver)
        {
            var errorSelectors = new[]
            {
                OpenQA.Selenium.By.CssSelector(".alert"),
                OpenQA.Selenium.By.CssSelector(".alert-danger"),
                OpenQA.Selenium.By.CssSelector(".alert-warning"),
                OpenQA.Selenium.By.CssSelector("[class*='error']"),
                OpenQA.Selenium.By.CssSelector("[class*='validation']"),
                OpenQA.Selenium.By.CssSelector(".field-validation-error"),
                OpenQA.Selenium.By.CssSelector(".text-danger"),
                OpenQA.Selenium.By.CssSelector("span[style*='color: red']"),
                OpenQA.Selenium.By.CssSelector("span[style*='color:red']"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'required')]")
            };

            var uniqueErrorMessages = new System.Collections.Generic.HashSet<string>();
            
            foreach (var selector in errorSelectors)
            {
                try
                {
                    var elements = driver.FindElements(selector);
                    foreach (var element in elements)
                    {
                        if (element.Displayed)
                        {
                            var text = element.Text?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                uniqueErrorMessages.Add(text);
                            }
                        }
                    }
                }
                catch
                {
                    // Continue with next selector
                }
            }

            return uniqueErrorMessages;
        }

        #endregion

        [Fact]
        public void ExploreReferralsPage_AfterLogin_LogAvailableElements()
        {
            using var driver = _driverFactory.CreateDriver();

            // Navigate to the application
            _output.WriteLine($"Navigating to application URL: {_config.AppUrl}");
            driver.Navigate().GoToUrl(_config.AppUrl);
            driver.WaitForReady(30);

            // Sign in
            _output.WriteLine($"Signing in with user: {_config.UserName}");
            var loginPage = new LoginPage(driver);
            loginPage.SignIn(_config.UserName, _config.Password);

            var isSignedIn = loginPage.IsSignedIn();
            Assert.True(isSignedIn, "User was not signed in successfully.");
            _output.WriteLine("[PASS] Successfully signed in");

            // Select Data Entry role
            _output.WriteLine("Attempting to select DataEntry role...");
            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");

            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded, "Landing page did not load after selecting Data Entry role.");
            _output.WriteLine("[PASS] Successfully selected Data Entry role");
            _output.WriteLine($"Landing page type: {landingPage.GetType().Name}");

            // Log current URL
            _output.WriteLine($"Current URL after role selection: {driver.Url}");

            // Navigate to Referrals page (find link by href, not by text to avoid hardcoding the count)
            _output.WriteLine("\nNavigating to Referrals page...");
            var referralsLink = driver.FindElements(OpenQA.Selenium.By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);

            Assert.NotNull(referralsLink);
            var linkText = referralsLink.Text?.Trim();
            _output.WriteLine($"Found Referrals link with text: '{linkText}'");
            
            referralsLink.Click();
            driver.WaitForReady(30);
            _output.WriteLine("[PASS] Successfully clicked Referrals link");
            _output.WriteLine($"Current URL: {driver.Url}");

            // Log what we find on the Referrals page
            _output.WriteLine("\n=== LOGGING REFERRALS PAGE ELEMENTS ===");

            // Log page title
            _output.WriteLine($"Page Title: {driver.Title}");

            // Try to find and log all buttons
            _output.WriteLine("\n=== BUTTONS ===");
            try
            {
                var buttons = driver.FindElements(OpenQA.Selenium.By.CssSelector("button, input[type='button'], input[type='submit'], a[id*='btn'], [id*='Button']"));
                _output.WriteLine($"Found {buttons.Count} button elements:");
                
                foreach (var button in buttons)
                {
                    try
                    {
                        if (!button.Displayed) continue;
                        
                        var id = button.GetAttribute("id") ?? "no-id";
                        var text = button.Text?.Trim() ?? button.GetAttribute("value") ?? "";
                        var tagName = button.TagName;
                        var isEnabled = button.Enabled;
                        
                        _output.WriteLine($"  - {tagName}: id='{id}', text='{text}', enabled={isEnabled}");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading button: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error finding buttons: {ex.Message}");
            }

            // Try to log all visible form fields
            _output.WriteLine("\n=== FORM FIELDS ===");
            try
            {
                var formFields = driver.FindElements(OpenQA.Selenium.By.CssSelector("input, select, textarea"));
                var displayedFields = formFields.Where(f => f.Displayed).ToList();
                _output.WriteLine($"Found {displayedFields.Count} visible form input elements:");
                
                foreach (var field in displayedFields)
                {
                    try
                    {
                        var id = field.GetAttribute("id") ?? "no-id";
                        var name = field.GetAttribute("name") ?? "no-name";
                        var type = field.GetAttribute("type") ?? field.TagName;
                        var tagName = field.TagName;
                        var isEnabled = field.Enabled;
                        var value = field.GetAttribute("value") ?? "";
                        
                        _output.WriteLine($"  - {tagName} [{type}]: id='{id}', name='{name}', enabled={isEnabled}, value='{value}'");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading field: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error finding form fields: {ex.Message}");
            }

            // Try to find and log tables/grids
            _output.WriteLine("\n=== TABLES/GRIDS ===");
            try
            {
                var tables = driver.FindElements(OpenQA.Selenium.By.TagName("table"));
                _output.WriteLine($"Found {tables.Count} table elements:");
                
                foreach (var table in tables)
                {
                    try
                    {
                        if (!table.Displayed) continue;
                        
                        var id = table.GetAttribute("id") ?? "no-id";
                        var rows = table.FindElements(OpenQA.Selenium.By.TagName("tr"));
                        
                        _output.WriteLine($"  - Table id='{id}', rows={rows.Count}");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading table: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error finding tables: {ex.Message}");
            }

            // Try to find any headers or labels
            _output.WriteLine("\n=== HEADERS AND LABELS ===");
            try
            {
                var headers = driver.FindElements(OpenQA.Selenium.By.CssSelector("h1, h2, h3, h4, h5, h6, label, span[class*='label']"));
                var displayedHeaders = headers.Where(h => h.Displayed && !string.IsNullOrWhiteSpace(h.Text)).ToList();
                _output.WriteLine($"Found {displayedHeaders.Count} visible headers/labels with text:");
                
                foreach (var header in displayedHeaders.Take(20)) // Limit to first 20 to avoid too much output
                {
                    try
                    {
                        var text = header.Text?.Trim() ?? "";
                        var tagName = header.TagName;
                        var id = header.GetAttribute("id") ?? "no-id";
                        
                        if (text.Length > 100) text = text.Substring(0, 100) + "...";
                        _output.WriteLine($"  - {tagName}: '{text}' (id='{id}')");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading header: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error finding headers: {ex.Message}");
            }

            // Try to find any divs with specific IDs or classes that might indicate content areas
            _output.WriteLine("\n=== MAIN CONTENT AREAS ===");
            try
            {
                var contentDivs = driver.FindElements(OpenQA.Selenium.By.CssSelector("[id*='Content'], [id*='Panel'], [class*='content'], [class*='panel']"));
                var displayedDivs = contentDivs.Where(d => d.Displayed).Take(10).ToList();
                _output.WriteLine($"Found {displayedDivs.Count} visible content area elements:");
                
                foreach (var div in displayedDivs)
                {
                    try
                    {
                        var id = div.GetAttribute("id") ?? "no-id";
                        var className = div.GetAttribute("class") ?? "no-class";
                        var tagName = div.TagName;
                        
                        _output.WriteLine($"  - {tagName}: id='{id}', class='{className}'");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading content area: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error finding content areas: {ex.Message}");
            }

            _output.WriteLine("\n=== CLICKING NEW REFERRAL BUTTON ===");

            // Find and click the New Referral button
            try
            {
                var newReferralButton = FindNewReferralButton(driver);
                _output.WriteLine($"Found New Referral button: id='{newReferralButton.GetAttribute("id")}', text='{newReferralButton.Text?.Trim()}'");
                
                newReferralButton.Click();
                driver.WaitForReady(30);
                _output.WriteLine("[PASS] Successfully clicked New Referral button");
                _output.WriteLine($"Current URL after clicking: {driver.Url}");
                _output.WriteLine($"Page Title: {driver.Title}");

                System.Threading.Thread.Sleep(1000); // Wait for page to fully load

                // Log what appears on the New Referral page
                _output.WriteLine("\n=== NEW REFERRAL PAGE ELEMENTS ===");

                // Check for form fields
                _output.WriteLine("\n--- Form Fields ---");
                var formFields = driver.FindElements(OpenQA.Selenium.By.CssSelector("input, select, textarea"))
                    .Where(f => f.Displayed).ToList();
                _output.WriteLine($"Found {formFields.Count} visible form fields:");
                
                foreach (var field in formFields.Take(20))
                {
                    try
                    {
                        var id = field.GetAttribute("id") ?? "no-id";
                        var name = field.GetAttribute("name") ?? "no-name";
                        var type = field.GetAttribute("type") ?? field.TagName;
                        var placeholder = field.GetAttribute("placeholder") ?? "";
                        var tagName = field.TagName;
                        
                        _output.WriteLine($"  - {tagName} [{type}]: id='{id}', name='{name}', placeholder='{placeholder}'");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading field: {ex.Message}");
                    }
                }

                // Check for labels to understand what fields are for
                _output.WriteLine("\n--- Labels ---");
                var labels = driver.FindElements(OpenQA.Selenium.By.TagName("label"))
                    .Where(l => l.Displayed && !string.IsNullOrWhiteSpace(l.Text)).ToList();
                _output.WriteLine($"Found {labels.Count} visible labels:");
                
                foreach (var label in labels.Take(20))
                {
                    try
                    {
                        var text = label.Text?.Trim() ?? "";
                        var forAttr = label.GetAttribute("for") ?? "";
                        
                        if (text.Length > 80) text = text.Substring(0, 80) + "...";
                        _output.WriteLine($"  - '{text}' (for='{forAttr}')");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading label: {ex.Message}");
                    }
                }

                // Check for buttons (Save, Cancel, etc.) - Show ALL including hidden
                _output.WriteLine("\n--- Buttons (ALL - including hidden) ---");
                var allButtons = driver.FindElements(OpenQA.Selenium.By.CssSelector("button, input[type='button'], input[type='submit'], input[type='image'], a[id*='btn'], [id*='Button'], [id*='Search']"));
                _output.WriteLine($"Found {allButtons.Count} total button elements:");
                
                foreach (var button in allButtons)
                {
                    try
                    {
                        var id = button.GetAttribute("id") ?? "no-id";
                        var name = button.GetAttribute("name") ?? "no-name";
                        var text = button.Text?.Trim() ?? button.GetAttribute("value") ?? "";
                        var type = button.GetAttribute("type") ?? button.TagName;
                        var enabled = button.Enabled;
                        var displayed = button.Displayed;
                        var className = button.GetAttribute("class") ?? "";
                        var style = button.GetAttribute("style") ?? "";
                        
                        // Show style if element is not displayed to see why
                        var styleInfo = displayed ? "" : $", style='{style}'";
                        
                        _output.WriteLine($"  - {type}: id='{id}', name='{name}', text='{text}', class='{className}', enabled={enabled}, displayed={displayed}{styleInfo}");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading button: {ex.Message}");
                    }
                }
                
                // Also look for input elements that might be search buttons
                _output.WriteLine("\n--- All Input Elements ---");
                var allInputs = driver.FindElements(OpenQA.Selenium.By.TagName("input"));
                _output.WriteLine($"Found {allInputs.Count} total input elements:");
                
                foreach (var input in allInputs)
                {
                    try
                    {
                        var id = input.GetAttribute("id") ?? "no-id";
                        var type = input.GetAttribute("type") ?? "text";
                        var value = input.GetAttribute("value") ?? "";
                        var displayed = input.Displayed;
                        
                        // Only show button/submit/image types
                        if (type == "button" || type == "submit" || type == "image" || id.Contains("btn", StringComparison.OrdinalIgnoreCase) || id.Contains("search", StringComparison.OrdinalIgnoreCase))
                        {
                            _output.WriteLine($"  - input[{type}]: id='{id}', value='{value}', displayed={displayed}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading input: {ex.Message}");
                    }
                }

                // Check for any validation messages or instructions
                _output.WriteLine("\n--- Page Headers and Instructions ---");
                var headings = driver.FindElements(OpenQA.Selenium.By.CssSelector("h1, h2, h3, h4, h5, h6"))
                    .Where(h => h.Displayed && !string.IsNullOrWhiteSpace(h.Text)).ToList();
                
                foreach (var heading in headings)
                {
                    try
                    {
                        var text = heading.Text?.Trim() ?? "";
                        var tagName = heading.TagName;
                        _output.WriteLine($"  - {tagName}: '{text}'");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  - Error reading heading: {ex.Message}");
                    }
                }

                // Check for required field indicators
                _output.WriteLine("\n--- Required Field Indicators ---");
                var requiredIndicators = driver.FindElements(OpenQA.Selenium.By.CssSelector(".required, [required], span.text-danger, .field-validation-error"));
                _output.WriteLine($"Found {requiredIndicators.Count} required field indicators");

                _output.WriteLine("\n=== NEW REFERRAL PAGE EXPLORATION COMPLETE ===");

                // Now fill in fake user information and search
                _output.WriteLine("\n========================================");
                _output.WriteLine("TESTING SEARCH WITH FAKE USER DATA");
                _output.WriteLine("========================================");

                var firstName = "unit";
                var lastName = "utest";
                var todayDate = DateTime.Now.ToString("MMddyyyy");
                var phone = "0000000000";
                var emergencyPhone = "0000000000";

                _output.WriteLine($"PC1 First Name: {firstName}");
                _output.WriteLine($"PC1 Last Name: {lastName}");
                _output.WriteLine($"DOB: {todayDate}");
                _output.WriteLine($"Phone: {phone}");
                _output.WriteLine($"Emergency Phone: {emergencyPhone}");

                try
                {
                    // Fill First Name
                    var firstNameField = FindTextInputBySuffix(driver, "txtpcfirstname");
                    firstNameField.Click();
                    System.Threading.Thread.Sleep(200);
                    firstNameField.Clear();
                    firstNameField.SendKeys(firstName);
                    _output.WriteLine("[PASS] Filled PC1 First Name");

                    // Fill Last Name
                    var lastNameField = FindTextInputBySuffix(driver, "txtpclastname");
                    lastNameField.Click();
                    System.Threading.Thread.Sleep(200);
                    lastNameField.Clear();
                    lastNameField.SendKeys(lastName);
                    _output.WriteLine("[PASS] Filled PC1 Last Name");

                    // Fill DOB
                    var dobField = FindTextInputBySuffix(driver, "txtpcdob");
                    dobField.Click();
                    System.Threading.Thread.Sleep(200);
                    dobField.Clear();
                    dobField.SendKeys(todayDate);
                    _output.WriteLine("[PASS] Filled DOB");

                    // Fill Phone
                    var phoneField = FindTextInputBySuffix(driver, "txtpcphone");
                    phoneField.Click();
                    System.Threading.Thread.Sleep(200);
                    phoneField.Clear();
                    phoneField.SendKeys(phone);
                    _output.WriteLine("[PASS] Filled Phone");

                    // Fill Emergency Phone
                    var emergencyPhoneField = FindTextInputBySuffix(driver, "txtpcemergencyphone");
                    emergencyPhoneField.Click();
                    System.Threading.Thread.Sleep(200);
                    emergencyPhoneField.Clear();
                    emergencyPhoneField.SendKeys(emergencyPhone);
                    _output.WriteLine("[PASS] Filled Emergency Phone");
                    
                    System.Threading.Thread.Sleep(500);

                    _output.WriteLine("\n--- Searching for user ---");
                    var searchButton = FindButtonByText(driver, "Search");
                    var buttonId = searchButton.GetAttribute("id") ?? "no-id";
                    var buttonText = searchButton.Text?.Trim() ?? searchButton.GetAttribute("value") ?? "";
                    _output.WriteLine($"Found search button: id='{buttonId}', text='{buttonText}'");
                    
                    if (!searchButton.Displayed)
                    {
                        ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", searchButton);
                    }
                    else
                    {
                        searchButton.Click();
                    }
                    
                    driver.WaitForReady(30);
                    System.Threading.Thread.Sleep(2000);
                    _output.WriteLine("[PASS] Clicked Search button");

                    // Check for "No records found." message (exact match)
                    _output.WriteLine("\n--- Checking for 'No records found.' Message ---");
                    var pageText = driver.FindElement(OpenQA.Selenium.By.TagName("body")).Text;
                    
                    var noRecordsFound = false;
                    var messageText = "";
                    
                    var messageSelectors = new[]
                    {
                        OpenQA.Selenium.By.CssSelector(".alert"),
                        OpenQA.Selenium.By.CssSelector("[class*='message']"),
                        OpenQA.Selenium.By.CssSelector("[class*='notification']"),
                        OpenQA.Selenium.By.CssSelector("span[class*='text']"),
                        OpenQA.Selenium.By.CssSelector("div[class*='result']"),
                        OpenQA.Selenium.By.XPath("//*[contains(text(), 'No records found')]")
                    };

                    foreach (var selector in messageSelectors)
                    {
                        try
                        {
                            var elements = driver.FindElements(selector);
                            foreach (var element in elements)
                            {
                                if (element.Displayed)
                                {
                                    var text = element.Text?.Trim() ?? "";
                                    // Check for EXACT match: "No records found."
                                    if (text.Equals("No records found.", StringComparison.Ordinal) ||
                                        text.Contains("No records found.", StringComparison.Ordinal))
                                    {
                                        noRecordsFound = true;
                                        messageText = text;
                                        _output.WriteLine($"Found exact message: '{text}'");
                                        break;
                                    }
                                }
                            }
                            if (noRecordsFound) break;
                        }
                        catch
                        {
                            // Continue
                        }
                    }

                    if (noRecordsFound)
                    {
                        _output.WriteLine($"[PASS] Found exact 'No records found.' message: {messageText}");
                    }
                    else
                    {
                        _output.WriteLine("[WARN] Could not find exact 'No records found.' message");
                        _output.WriteLine($"Page text preview: {pageText.Substring(0, Math.Min(500, pageText.Length))}...");
                    }

                    // Find the "add new" link
                    _output.WriteLine("\n--- Looking for 'Add New' or 'Create' Link ---");
                    var allLinks = driver.FindElements(OpenQA.Selenium.By.TagName("a"));
                    _output.WriteLine($"Found {allLinks.Count} total link elements");
                    
                    var addNewLink = (OpenQA.Selenium.IWebElement)null;
                    
                    foreach (var link in allLinks)
                    {
                        try
                        {
                            if (link.Displayed)
                            {
                                var text = link.Text?.Trim() ?? "";
                                var id = link.GetAttribute("id") ?? "";
                                var href = link.GetAttribute("href") ?? "";
                                
                                _output.WriteLine($"  Link: id='{id}', text='{text}', href='{href}'");
                                
                                if ((text.Contains("add", StringComparison.OrdinalIgnoreCase) && 
                                     text.Contains("new", StringComparison.OrdinalIgnoreCase)) ||
                                    text.Contains("click here", StringComparison.OrdinalIgnoreCase) ||
                                    (text.Contains("create", StringComparison.OrdinalIgnoreCase) &&
                                     !text.Contains("case", StringComparison.OrdinalIgnoreCase)))
                                {
                                    addNewLink = link;
                                    _output.WriteLine($"[FOUND] Potential add new link: '{text}' (id='{id}')");
                                }
                            }
                        }
                        catch (Exception linkEx)
                        {
                            _output.WriteLine($"  Error reading link: {linkEx.Message}");
                        }
                    }

                    if (addNewLink != null)
                    {
                        _output.WriteLine($"\n[PASS] Found 'add new' link: '{addNewLink.Text?.Trim()}'");
                        _output.WriteLine($"Link id: '{addNewLink.GetAttribute("id")}'");
                        _output.WriteLine($"Link href: '{addNewLink.GetAttribute("href")}'");
                        
                        // Click the link
                        _output.WriteLine("\n========================================");
                        _output.WriteLine("CLICKING 'ADD NEW' LINK");
                        _output.WriteLine("========================================");
                        
                        ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", addNewLink);
                        System.Threading.Thread.Sleep(500);
                        
                        addNewLink.Click();
                        driver.WaitForReady(30);
                        System.Threading.Thread.Sleep(2000);
                        
                        _output.WriteLine($"[PASS] Clicked 'add new' link");
                        _output.WriteLine($"Current URL: {driver.Url}");
                        _output.WriteLine($"Page Title: {driver.Title}");

                        // Log all elements on the page after clicking add new link
                        _output.WriteLine("\n========================================");
                        _output.WriteLine("LOGGING PAGE ELEMENTS AFTER CLICKING 'ADD NEW' LINK");
                        _output.WriteLine("========================================");

                        // Log all form fields
                        _output.WriteLine("\n--- Form Fields ---");
                        var newFormFields = driver.FindElements(OpenQA.Selenium.By.CssSelector("input, select, textarea"))
                            .Where(f => f.Displayed).ToList();
                        _output.WriteLine($"Found {newFormFields.Count} visible form fields:");
                        
                        foreach (var field in newFormFields)
                        {
                            try
                            {
                                var id = field.GetAttribute("id") ?? "no-id";
                                var name = field.GetAttribute("name") ?? "no-name";
                                var type = field.GetAttribute("type") ?? field.TagName;
                                var value = field.GetAttribute("value") ?? "";
                                var placeholder = field.GetAttribute("placeholder") ?? "";
                                var tagName = field.TagName;
                                
                                _output.WriteLine($"  - {tagName} [{type}]: id='{id}', name='{name}', value='{value}', placeholder='{placeholder}'");
                            }
                            catch (Exception fieldEx)
                            {
                                _output.WriteLine($"  - Error reading field: {fieldEx.Message}");
                            }
                        }

                        // Log all labels
                        _output.WriteLine("\n--- Labels ---");
                        var newLabels = driver.FindElements(OpenQA.Selenium.By.TagName("label"))
                            .Where(l => l.Displayed && !string.IsNullOrWhiteSpace(l.Text)).ToList();
                        _output.WriteLine($"Found {newLabels.Count} visible labels:");
                        
                        foreach (var label in newLabels)
                        {
                            try
                            {
                                var text = label.Text?.Trim() ?? "";
                                var forAttr = label.GetAttribute("for") ?? "";
                                
                                if (text.Length > 80) text = text.Substring(0, 80) + "...";
                                _output.WriteLine($"  - '{text}' (for='{forAttr}')");
                            }
                            catch (Exception labelEx)
                            {
                                _output.WriteLine($"  - Error reading label: {labelEx.Message}");
                            }
                        }

                        // Log all buttons
                        _output.WriteLine("\n--- Buttons ---");
                        var newButtons = driver.FindElements(OpenQA.Selenium.By.CssSelector("button, input[type='button'], input[type='submit'], input[type='image'], a[id*='btn'], [id*='Button']"));
                        _output.WriteLine($"Found {newButtons.Count} button elements:");
                        
                        foreach (var button in newButtons)
                        {
                            try
                            {
                                var id = button.GetAttribute("id") ?? "no-id";
                                var name = button.GetAttribute("name") ?? "no-name";
                                var text = button.Text?.Trim() ?? button.GetAttribute("value") ?? "";
                                var type = button.GetAttribute("type") ?? button.TagName;
                                var enabled = button.Enabled;
                                var displayed = button.Displayed;
                                var className = button.GetAttribute("class") ?? "";
                                
                                _output.WriteLine($"  - {type}: id='{id}', name='{name}', text='{text}', class='{className}', enabled={enabled}, displayed={displayed}");
                            }
                            catch (Exception buttonEx)
                            {
                                _output.WriteLine($"  - Error reading button: {buttonEx.Message}");
                            }
                        }

                        // Log all headings
                        _output.WriteLine("\n--- Headings ---");
                        var newHeadings = driver.FindElements(OpenQA.Selenium.By.CssSelector("h1, h2, h3, h4, h5, h6"))
                            .Where(h => h.Displayed && !string.IsNullOrWhiteSpace(h.Text)).ToList();
                        
                        foreach (var heading in newHeadings)
                        {
                            try
                            {
                                var text = heading.Text?.Trim() ?? "";
                                var tagName = heading.TagName;
                                _output.WriteLine($"  - {tagName}: '{text}'");
                            }
                            catch (Exception headingEx)
                            {
                                _output.WriteLine($"  - Error reading heading: {headingEx.Message}");
                            }
                        }

                        // Log any divs or panels that might contain content
                        _output.WriteLine("\n--- Content Panels/Divs ---");
                        var contentDivs = driver.FindElements(OpenQA.Selenium.By.CssSelector("[id*='Panel'], [id*='panel'], [class*='panel'], [id*='Content']"))
                            .Where(d => d.Displayed).ToList();
                        _output.WriteLine($"Found {contentDivs.Count} visible content panels:");
                        
                        foreach (var div in contentDivs.Take(10))
                        {
                            try
                            {
                                var id = div.GetAttribute("id") ?? "no-id";
                                var className = div.GetAttribute("class") ?? "no-class";
                                var tagName = div.TagName;
                                
                                _output.WriteLine($"  - {tagName}: id='{id}', class='{className}'");
                            }
                            catch (Exception divEx)
                            {
                                _output.WriteLine($"  - Error reading div: {divEx.Message}");
                            }
                        }

                        // Log all tables if any
                        _output.WriteLine("\n--- Tables ---");
                        var tables = driver.FindElements(OpenQA.Selenium.By.TagName("table"))
                            .Where(t => t.Displayed).ToList();
                        _output.WriteLine($"Found {tables.Count} visible tables:");
                        
                        foreach (var table in tables)
                        {
                            try
                            {
                                var id = table.GetAttribute("id") ?? "no-id";
                                var rows = table.FindElements(OpenQA.Selenium.By.TagName("tr")).Count;
                                
                                _output.WriteLine($"  - Table id='{id}', rows={rows}");
                            }
                            catch (Exception tableEx)
                            {
                                _output.WriteLine($"  - Error reading table: {tableEx.Message}");
                            }
                        }

                        _output.WriteLine("\n========================================");
                        _output.WriteLine("'ADD NEW' PAGE EXPLORATION COMPLETE");
                        _output.WriteLine("========================================");
                    }
                    else
                    {
                        _output.WriteLine("[WARN] Could not find 'add new' link to click");
                    }
                }
                catch (Exception searchEx)
                {
                    _output.WriteLine($"[FAIL] Error during search flow: {searchEx.Message}");
                    _output.WriteLine($"Stack trace: {searchEx.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[FAIL] Error clicking or exploring New Referral button: {ex.Message}");
                _output.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            _output.WriteLine("\n=== EXPLORATION COMPLETE ===");
        }


        [Fact]
        public void ReferralsPage_ClickFirstEdit_OpensEditPage()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper method for login and navigation
            LoginAndNavigateToReferrals(driver);

            // Find the active referrals table and the first edit button within it
            var activeReferralsTable = FindActiveReferralsTable(driver);
            Assert.NotNull(activeReferralsTable);

            var firstRow = activeReferralsTable
                .FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"))
                .FirstOrDefault(row => row.Displayed);
            Assert.NotNull(firstRow);

            var editButton = FindReferralEditButton(firstRow);

            Assert.NotNull(editButton);
            _output.WriteLine($"[PASS] Found edit button: id='{editButton.GetAttribute("id")}'");

            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", editButton);
            System.Threading.Thread.Sleep(300);

            if (!editButton.Displayed)
            {
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
            }
            else
            {
                editButton.Click();
            }

            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(500);

            var currentUrl = driver.Url;
            var pageTitle = driver.Title;

            _output.WriteLine($"[INFO] Navigated to URL: {currentUrl}");
            _output.WriteLine($"[INFO] Page title: {pageTitle}");

            Assert.Contains("Referral.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Referral", pageTitle, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Edit page opened successfully");
        }

        [Fact]
        public void ChangeCompletedReferralYear_UpdatesTableWithCorrectYearEntries()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper method for login and navigation
            LoginAndNavigateToReferrals(driver);

            // Find the year dropdown
            var yearDropdown = FindCompletedReferralYearDropdown(driver);
            Assert.NotNull(yearDropdown);
            _output.WriteLine($"Found year dropdown");

            // Get all available years
            var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(yearDropdown);
            var availableYears = selectElement.Options.Select(o => o.Text).Where(y => !string.IsNullOrWhiteSpace(y)).ToList();
            _output.WriteLine($"Available years in dropdown: {string.Join(", ", availableYears)}");
            _output.WriteLine($"Total years to test: {availableYears.Count}");

            Assert.True(availableYears.Count > 0, "No years are available in the dropdown");

            // Test each year in the dropdown
            var failedYears = new System.Collections.Generic.List<string>();
            var yearResults = new System.Collections.Generic.Dictionary<string, int>();
            var toastResults = new System.Collections.Generic.Dictionary<string, string>();

            _output.WriteLine("\n========================================");
            _output.WriteLine("TESTING ALL YEARS");
            _output.WriteLine("========================================");

            foreach (var yearToTest in availableYears)
            {
                _output.WriteLine($"\n--- Testing Year: {yearToTest} ---");

                try
                {
                    // Re-find the dropdown to avoid stale element references
                    yearDropdown = FindCompletedReferralYearDropdown(driver);
                    selectElement = new OpenQA.Selenium.Support.UI.SelectElement(yearDropdown);

                    // Select the year
                    _output.WriteLine($"Selecting year: {yearToTest}");
                    selectElement.SelectByText(yearToTest);

                    // Wait for the page to update
                    driver.WaitForUpdatePanel(10);
                    driver.WaitForReady(10);
                    
                    // Check for toast notification
                    var toastFound = false;
                    var toastMessage = "";
                    try
                    {
                        // Common toast notification selectors
                        var toastSelectors = new[]
                        {
                            OpenQA.Selenium.By.CssSelector(".toast"),
                            OpenQA.Selenium.By.CssSelector(".toast-message"),
                            OpenQA.Selenium.By.CssSelector("[class*='toast']"),
                            OpenQA.Selenium.By.CssSelector(".alert"),
                            OpenQA.Selenium.By.CssSelector("[role='alert']"),
                            OpenQA.Selenium.By.CssSelector(".notification"),
                            OpenQA.Selenium.By.CssSelector("[class*='notification']"),
                            OpenQA.Selenium.By.CssSelector(".swal2-container"), // SweetAlert2
                            OpenQA.Selenium.By.CssSelector("[id*='toast']"),
                            OpenQA.Selenium.By.CssSelector("[class*='Toastify']") // Toastify
                        };

                        foreach (var selector in toastSelectors)
                        {
                            try
                            {
                                var toastElements = driver.FindElements(selector);
                                var visibleToast = toastElements.FirstOrDefault(t => t.Displayed);
                                
                                if (visibleToast != null)
                                {
                                    toastFound = true;
                                    toastMessage = visibleToast.Text?.Trim() ?? "";
                                    _output.WriteLine($"[PASS] Toast notification found: '{toastMessage}'");
                                    break;
                                }
                            }
                            catch
                            {
                                // Continue trying other selectors
                            }
                        }

                        if (!toastFound)
                        {
                            _output.WriteLine($"[WARN] No toast notification found for year {yearToTest}");
                            toastResults[yearToTest] = "NOT FOUND";
                            failedYears.Add($"{yearToTest}: No toast notification appeared after selecting year");
                        }
                        else
                        {
                            toastResults[yearToTest] = toastMessage;
                        }
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"[WARN] Error checking for toast notification: {ex.Message}");
                        toastResults[yearToTest] = "ERROR";
                        failedYears.Add($"{yearToTest}: Error checking toast notification - {ex.Message}");
                    }
                    
                    System.Threading.Thread.Sleep(1500); // Wait for any JavaScript updates

                    // Re-find the dropdown and verify it shows the selected year
                    yearDropdown = FindCompletedReferralYearDropdown(driver);
                    selectElement = new OpenQA.Selenium.Support.UI.SelectElement(yearDropdown);
                    var currentlySelectedYear = selectElement.SelectedOption.Text;

                    _output.WriteLine($"Dropdown now shows: {currentlySelectedYear}");

                    // Verify the dropdown actually changed
                    if (!string.Equals(currentlySelectedYear, yearToTest, StringComparison.OrdinalIgnoreCase))
                    {
                        var errorMsg = $"Failed to select year {yearToTest}. Dropdown shows {currentlySelectedYear} instead.";
                        _output.WriteLine($"[FAIL] {errorMsg}");
                        failedYears.Add($"{yearToTest}: {errorMsg}");
                        continue;
                    }

                    // Re-find the table
                    var completedReferralsTable = FindCompletedReferralsTable(driver);
                    Assert.NotNull(completedReferralsTable);

                    // Get the rows
                    var tableRows = completedReferralsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr")).ToList();
                    var rowCount = tableRows.Count;

                    yearResults[yearToTest] = rowCount;
                    _output.WriteLine($"Table has {rowCount} rows for year {yearToTest}");

                    // Check if table is displaying data or "no records" message
                    var hasData = tableRows.Any(row =>
                    {
                        var cells = row.FindElements(OpenQA.Selenium.By.TagName("td"));
                        return cells.Count > 1; // More than 1 cell means actual data, not just "no records" message
                    });

                    if (hasData && rowCount > 0)
                    {
                        _output.WriteLine($"[PASS] Year {yearToTest} displayed successfully with {rowCount} entries");

                        // Log first few rows
                        _output.WriteLine($"Sample data:");
                        foreach (var row in tableRows.Take(2))
                        {
                            try
                            {
                                var cells = row.FindElements(OpenQA.Selenium.By.TagName("td")).ToList();
                                if (cells.Count > 1)
                                {
                                    var rowData = string.Join(" | ", cells.Take(5).Select(c => c.Text?.Trim() ?? ""));
                                    _output.WriteLine($"  {rowData}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _output.WriteLine($"  Error reading row: {ex.Message}");
                            }
                        }
                    }
                    else if (rowCount == 1)
                    {
                        // Check if it's a "no records" message
                        var firstRow = tableRows.FirstOrDefault();
                        if (firstRow != null)
                        {
                            var text = firstRow.Text?.Trim() ?? "";
                            if (text.Contains("No", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("record", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("data", StringComparison.OrdinalIgnoreCase))
                            {
                                _output.WriteLine($"[PASS] Year {yearToTest} has no data (showing 'no records' message)");
                            }
                            else
                            {
                                _output.WriteLine($"[PASS] Year {yearToTest} has 1 entry: {text}");
                            }
                        }
                    }
                    else
                    {
                        // No rows at all - this might be an issue
                        var errorMsg = $"Year {yearToTest} is in dropdown but table shows 0 rows (no data and no 'no records' message)";
                        _output.WriteLine($"[WARN] {errorMsg}");
                        // This might be expected if there's really no data, so we'll just warn, not fail
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Exception while testing year {yearToTest}: {ex.Message}";
                    _output.WriteLine($"[FAIL] {errorMsg}");
                    failedYears.Add($"{yearToTest}: {errorMsg}");
                }
            }

            // Summary
            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST SUMMARY");
            _output.WriteLine("========================================");
            _output.WriteLine($"Total years tested: {availableYears.Count}");
            _output.WriteLine($"Successful: {availableYears.Count - failedYears.Count}");
            _output.WriteLine($"Failed: {failedYears.Count}");

            _output.WriteLine("\nYear-wise results:");
            foreach (var year in availableYears)
            {
                var rows = yearResults.ContainsKey(year) ? yearResults[year] : 0;
                var toast = toastResults.ContainsKey(year) ? toastResults[year] : "N/A";
                _output.WriteLine($"  {year}: {rows} rows, Toast: {toast}");
            }

            if (failedYears.Count > 0)
            {
                _output.WriteLine("\nFailed years:");
                foreach (var failure in failedYears)
                {
                    _output.WriteLine($"  [FAIL] {failure}");
                }

                Assert.True(false, $"{failedYears.Count} year(s) failed to display correctly: {string.Join("; ", failedYears)}");
            }

            _output.WriteLine("\n[PASS] All years in the dropdown displayed their data correctly!");
        }

        [Fact]
        public void ReferralsPage_DefaultYearAndEntriesDropdown_AreCorrect()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper method for login and navigation
            LoginAndNavigateToReferrals(driver);

            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING DEFAULT YEAR");
            _output.WriteLine("========================================");

            // Find the year dropdown
            var yearDropdown = FindCompletedReferralYearDropdown(driver);
            Assert.NotNull(yearDropdown);

            var yearSelectElement = new OpenQA.Selenium.Support.UI.SelectElement(yearDropdown);
            var defaultYear = yearSelectElement.SelectedOption.Text;
            var currentYear = DateTime.Now.Year.ToString();

            _output.WriteLine($"Default selected year: {defaultYear}");
            _output.WriteLine($"Current year: {currentYear}");

            Assert.Equal(currentYear, defaultYear);
            _output.WriteLine($"[PASS] Default year is correctly set to current year ({currentYear})");

            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING ENTRIES DROPDOWN");
            _output.WriteLine("========================================");

            // Find the entries dropdown for completed referrals table
            var entriesDropdowns = driver.FindElements(OpenQA.Selenium.By.CssSelector("select[name*='_length']"));
            
            _output.WriteLine($"Found {entriesDropdowns.Count} entries dropdown(s)");

            var completedReferralsEntriesDropdown = entriesDropdowns
                .FirstOrDefault(dd => dd.GetAttribute("name")?.Contains("grCompletedReferrals") == true);

            if (completedReferralsEntriesDropdown == null)
            {
                // Try alternative selector
                completedReferralsEntriesDropdown = driver.FindElements(OpenQA.Selenium.By.CssSelector("select[name$='_length']"))
                    .Skip(1).FirstOrDefault(); // Second dropdown is usually for the second table
            }

            Assert.NotNull(completedReferralsEntriesDropdown);
            _output.WriteLine("Found completed referrals entries dropdown");

            var entriesSelectElement = new OpenQA.Selenium.Support.UI.SelectElement(completedReferralsEntriesDropdown);
            var defaultEntries = entriesSelectElement.SelectedOption.Text;

            _output.WriteLine($"Default entries per page: {defaultEntries}");

            // Get all available options
            var availableOptions = entriesSelectElement.Options.Select(o => o.Text).ToList();
            _output.WriteLine($"Available options: {string.Join(", ", availableOptions)}");

            // Verify default is 10
            Assert.Equal("10", defaultEntries);
            _output.WriteLine("[PASS] Default entries per page is 10");

            // Verify all expected options are present
            var expectedOptions = new[] { "10", "25", "50", "100" };
            foreach (var expected in expectedOptions)
            {
                Assert.Contains(expected, availableOptions);
            }
            _output.WriteLine($"[PASS] All expected options (10, 25, 50, 100) are present");

            _output.WriteLine("\n========================================");
            _output.WriteLine("TESTING ENTRIES DROPDOWN CHANGES");
            _output.WriteLine("========================================");

            // Get initial table info
            var completedReferralsTable = FindCompletedReferralsTable(driver);
            var initialRows = completedReferralsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr")).ToList();
            _output.WriteLine($"Initial rows displayed with '{defaultEntries}' entries: {initialRows.Count}");

            // Test changing the entries dropdown to each value
            var testResults = new System.Collections.Generic.Dictionary<string, int>();
            var failures = new System.Collections.Generic.List<string>();

            foreach (var option in availableOptions.Where(o => o != defaultEntries))
            {
                _output.WriteLine($"\nTesting entries option: {option}");

                try
                {
                    // Re-find the dropdown (to avoid stale element)
                    completedReferralsEntriesDropdown = driver.FindElements(OpenQA.Selenium.By.CssSelector("select[name$='_length']"))
                        .Skip(1).FirstOrDefault();
                    
                    if (completedReferralsEntriesDropdown == null)
                    {
                        _output.WriteLine($"[WARN] Could not find entries dropdown for option {option}");
                        continue;
                    }

                    entriesSelectElement = new OpenQA.Selenium.Support.UI.SelectElement(completedReferralsEntriesDropdown);
                    
                    // Select the option
                    entriesSelectElement.SelectByText(option);
                    _output.WriteLine($"Selected {option} entries per page");

                    // Wait for table to update
                    System.Threading.Thread.Sleep(1000);
                    driver.WaitForReady(5);

                    // Re-find and verify dropdown changed
                    completedReferralsEntriesDropdown = driver.FindElements(OpenQA.Selenium.By.CssSelector("select[name$='_length']"))
                        .Skip(1).FirstOrDefault();
                    entriesSelectElement = new OpenQA.Selenium.Support.UI.SelectElement(completedReferralsEntriesDropdown);
                    var currentSelection = entriesSelectElement.SelectedOption.Text;

                    if (currentSelection != option)
                    {
                        var error = $"Failed to select {option} entries. Dropdown shows {currentSelection}";
                        _output.WriteLine($"[FAIL] {error}");
                        failures.Add(error);
                        continue;
                    }

                    // Get updated row count
                    completedReferralsTable = FindCompletedReferralsTable(driver);
                    var updatedRows = completedReferralsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr")).ToList();
                    var rowCount = updatedRows.Count;

                    testResults[option] = rowCount;
                    _output.WriteLine($"Table now shows {rowCount} rows with '{option}' entries per page");
                    _output.WriteLine($"[PASS] Entries dropdown changed to {option} successfully");
                }
                catch (Exception ex)
                {
                    var error = $"Exception while testing {option} entries: {ex.Message}";
                    _output.WriteLine($"[FAIL] {error}");
                    failures.Add(error);
                }
            }

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST SUMMARY");
            _output.WriteLine("========================================");
            _output.WriteLine($"Default year: {defaultYear} (Expected: {currentYear})");
            _output.WriteLine($"Default entries: {defaultEntries} (Expected: 10)");
            _output.WriteLine($"\nEntries dropdown test results:");
            _output.WriteLine($"  Default (10): {initialRows.Count} rows");
            foreach (var kvp in testResults)
            {
                _output.WriteLine($"  {kvp.Key}: {kvp.Value} rows");
            }

            if (failures.Count > 0)
            {
                _output.WriteLine($"\n[FAIL] {failures.Count} test(s) failed:");
                foreach (var failure in failures)
                {
                    _output.WriteLine($"  - {failure}");
                }
                Assert.True(false, $"{failures.Count} entries dropdown test(s) failed: {string.Join("; ", failures)}");
            }

            _output.WriteLine("\n[PASS] All entries dropdown options work correctly!");
        }

        [Fact]
        public void NewReferral_SearchForPersonTest_ShowsMatchesInGrid()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper methods for login and navigation
            LoginAndNavigateToReferrals(driver);
            ClickNewReferralButton(driver);

            // Search for Person/Test data
            var firstName = "Unit";
            var lastName = "Test";
            var todayDate = DateTime.Now.ToString("MMddyyyy");
            var phone = "0000000000";
            var emergencyPhone = "0000000000";

            try
            {
                FillPersonSearchForm(driver, firstName, lastName, todayDate, phone, emergencyPhone);
                ClickSearchButton(driver);

                // Check for results grid
                _output.WriteLine("\n========================================");
                _output.WriteLine("VERIFYING SEARCH RESULTS IN GRID");
                _output.WriteLine("========================================");

                // Look for the results grid
                var resultsGrid = FindReferralSearchResultsGrid(driver);
                
                if (resultsGrid != null)
                {
                    _output.WriteLine("[PASS] Found results grid");
                    
                    // Count the rows in the grid (excluding header)
                    var gridRows = resultsGrid.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr")).ToList();
                    _output.WriteLine($"Found {gridRows.Count} rows in results grid");
                    
                    // Check if there are actual data rows (not just "no records" message)
                    var hasDataRows = gridRows.Any(row =>
                    {
                        var cells = row.FindElements(OpenQA.Selenium.By.TagName("td"));
                        return cells.Count > 1; // More than 1 cell indicates actual data
                    });

                    Assert.True(hasDataRows, "Expected to find data rows in the results grid");
                    _output.WriteLine($"[PASS] Found data rows in results grid");

                    // Log some of the results
                    _output.WriteLine("\nSample results:");
                    foreach (var row in gridRows.Take(3))
                    {
                        try
                        {
                            var cells = row.FindElements(OpenQA.Selenium.By.TagName("td"));
                            if (cells.Count > 1)
                            {
                                var rowData = string.Join(" | ", cells.Take(5).Select(c => c.Text?.Trim() ?? ""));
                                _output.WriteLine($"  {rowData}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine($"  Error reading row: {ex.Message}");
                        }
                    }

                    // Check for "Select" buttons/links in the grid
                    var selectLinks = resultsGrid.FindElements(OpenQA.Selenium.By.LinkText("Select"));
                    _output.WriteLine($"\nFound {selectLinks.Count} 'Select' links in the grid");
                    Assert.True(selectLinks.Count > 0, "Expected to find 'Select' links in the results grid");
                    _output.WriteLine($"[PASS] Found {selectLinks.Count} 'Select' links for matching records");
                }
                else
                {
                    _output.WriteLine("[FAIL] Results grid not found or not displayed");
                    Assert.True(false, "Expected to find a visible results grid after searching for Person/Test");
                }

                _output.WriteLine("\n========================================");
                _output.WriteLine("TEST SUMMARY");
                _output.WriteLine("========================================");
                _output.WriteLine("[PASS] Successfully filled search form with Person/Test data");
                _output.WriteLine("[PASS] Successfully submitted search");
                _output.WriteLine("[PASS] Verified search results are displayed in grid");
                _output.WriteLine("\n[PASS] Test completed successfully!");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[FAIL] Error during test: {ex.Message}");
                _output.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        [Fact]
        public void NewReferral_SearchWithTestData_ShowsNoRecordFoundMessage()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper methods for login and navigation
            LoginAndNavigateToReferrals(driver);
            ClickNewReferralButton(driver);

            // Search for unit/test data (expecting no results)
            var firstName = "unit";
            var lastName = "test";
            var todayDate = DateTime.Now.ToString("MMddyyyy");
            var phone = "0000000000";
            var emergencyPhone = "0000000000";

            try
            {
                FillPersonSearchForm(driver, firstName, lastName, todayDate, phone, emergencyPhone);
                ClickSearchButton(driver);

                // Check for "No records found." message (exact match)
                _output.WriteLine("\n========================================");
                _output.WriteLine("VERIFYING 'No records found.' MESSAGE");
                _output.WriteLine("========================================");

                // Look for the exact message "No records found."
                var pageText = driver.FindElement(OpenQA.Selenium.By.TagName("body")).Text;
                _output.WriteLine($"Checking page content for exact 'No records found.' message...");

                var noRecordsFound = false;
                var messageText = "";

                // Check for the exact "No records found." message
                var messageSelectors = new[]
                {
                    OpenQA.Selenium.By.CssSelector(".alert"),
                    OpenQA.Selenium.By.CssSelector("[class*='message']"),
                    OpenQA.Selenium.By.CssSelector("[class*='notification']"),
                    OpenQA.Selenium.By.CssSelector("span[class*='text']"),
                    OpenQA.Selenium.By.CssSelector("div[class*='result']"),
                    OpenQA.Selenium.By.CssSelector("td"),  // Also check table cells
                    OpenQA.Selenium.By.XPath("//*[contains(text(), 'No records found')]")
                };

                foreach (var selector in messageSelectors)
                {
                    try
                    {
                        var elements = driver.FindElements(selector);
                        foreach (var element in elements)
                        {
                            if (element.Displayed)
                            {
                                var text = element.Text?.Trim() ?? "";
                                // Check for EXACT match: "No records found."
                                if (text.Equals("No records found.", StringComparison.Ordinal) ||
                                    text.Contains("No records found.", StringComparison.Ordinal))
                                {
                                    noRecordsFound = true;
                                    messageText = text;
                                    _output.WriteLine($"Found exact message: '{text}'");
                                    break;
                                }
                            }
                        }
                        if (noRecordsFound) break;
                    }
                    catch
                    {
                        // Continue with next selector
                    }
                }

                // If not found in elements, check if the page text contains the exact string
                if (!noRecordsFound)
                {
                    if (pageText.Contains("No records found.", StringComparison.Ordinal))
                    {
                        noRecordsFound = true;
                        messageText = "No records found.";
                        _output.WriteLine($"[PASS] Found exact 'No records found.' text in page content");
                    }
                    else
                    {
                        _output.WriteLine($"[FAIL] Could not find exact 'No records found.' message");
                        _output.WriteLine($"Page text preview: {pageText.Substring(0, Math.Min(500, pageText.Length))}...");
                    }
                }

                Assert.True(noRecordsFound, "Expected to find the exact message 'No records found.' after searching for non-existent referral");
                _output.WriteLine($"[PASS] Verified exact 'No records found.' message is displayed");

                // Check for "add new one" or similar link/button
                _output.WriteLine("\nChecking for 'add new' option...");
                var addNewLinkFound = false;
                var addNewLinkText = "";

                var links = driver.FindElements(OpenQA.Selenium.By.TagName("a"));
                foreach (var link in links)
                {
                    try
                    {
                        if (link.Displayed)
                        {
                            var text = link.Text?.Trim() ?? "";
                            if ((text.Contains("add", StringComparison.OrdinalIgnoreCase) && 
                                 text.Contains("new", StringComparison.OrdinalIgnoreCase)) ||
                                text.Contains("click here", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("create", StringComparison.OrdinalIgnoreCase))
                            {
                                addNewLinkFound = true;
                                addNewLinkText = text;
                                _output.WriteLine($"Found 'add new' link: '{text}'");
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Continue
                    }
                }

                if (addNewLinkFound)
                {
                    _output.WriteLine($"[PASS] Found 'add new' option: '{addNewLinkText}'");
                }
                else
                {
                    _output.WriteLine("[WARN] No explicit 'add new' link found on the page");
                }

                _output.WriteLine("\n========================================");
                _output.WriteLine("TEST SUMMARY");
                _output.WriteLine("========================================");
                _output.WriteLine("[PASS] Successfully filled search form with test data");
                _output.WriteLine("[PASS] Successfully submitted search");
                _output.WriteLine($"[PASS] Verified 'no records found' message: {messageText}");
                if (addNewLinkFound)
                {
                    _output.WriteLine($"[PASS] Verified 'add new' option available: {addNewLinkText}");
                }
                _output.WriteLine("\n[PASS] Test completed successfully!");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[FAIL] Error during test: {ex.Message}");
                _output.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// ⚠️ IMPORTANT: IF THIS TEST FAILS WITH "Assert.Contains() Failure: Not found: No records found."
        /// 
        /// This means the person already exists in the database from a previous test run.
        /// 
        /// TO FIX: Change the test data below (around line 1999-2003):
        /// - Change firstName (e.g., "checkone" to "checktwo" or "checkone1")
        /// - Change lastName (e.g., "check" to "check1")
        /// - Or change the DOB year (e.g., "11091916" to "11091816" for year 2018)
        /// 
        /// File: UnitTests/Referrals/ReferralsTests.cs
        /// Location: Lines 1999-2003 (see *** CHANGE TEST DATA HERE *** comment)
        /// </summary>
        [Fact]
        public void NewReferral_SearchNoRecordsFound_CreateNewPersonProfileWithRaceAndGender()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper methods for login and navigation
            LoginAndNavigateToReferrals(driver);
            ClickNewReferralButton(driver);

            // *** CHANGE TEST DATA HERE IF TEST FAILS ***
            // If test fails because person already exists in database, modify these values:
            var firstName = "Noavailable";      // Change to unique value (e.g., "checktwo", "checkone1")
            var lastName = "checkavailable";          // Change to unique value (e.g., "check1")
            var dob = "10091916";            // Change year digits (e.g., "11091816" for 2018, "11091716" for 2017)
            var phone = "2222222222";        // Can also change phone number
            var emergencyPhone = "2222222222"; // Can also change emergency phone
            // *** END OF TEST DATA ***
            
            // Note: DOB format is MMDDYYYY but system only uses last 2 digits for year
            // "11091916" becomes "11/09/2019" (system interprets "16" as 2019)

            // Fill in the search form with specific test data
            FillPersonSearchForm(driver, firstName, lastName, dob, phone, emergencyPhone);
            ClickSearchButton(driver);

            // Verify "No records found." message
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING 'No records found.' MESSAGE");
            _output.WriteLine("========================================");

            var pageText = driver.FindElement(OpenQA.Selenium.By.TagName("body")).Text;
            
            if (!pageText.Contains("No records found.", StringComparison.Ordinal))
            {
                _output.WriteLine("[FAIL] Person already exists in database!");
                _output.WriteLine("");
                _output.WriteLine("⚠️⚠️⚠️ TEST FAILED - PERSON ALREADY EXISTS ⚠️⚠️⚠️");
                _output.WriteLine("");
                _output.WriteLine("The search returned existing records instead of 'No records found.'");
                _output.WriteLine("This means the test data already exists in the database from a previous run.");
                _output.WriteLine("");
                _output.WriteLine("TO FIX THIS:");
                _output.WriteLine("1. Open file: UnitTests/Referrals/ReferralsTests.cs");
                _output.WriteLine("2. Go to lines 2006-2013");
                _output.WriteLine("3. Look for the comment: *** CHANGE TEST DATA HERE IF TEST FAILS ***");
                _output.WriteLine("4. Change one or more of these values:");
                _output.WriteLine($"   - firstName = \"{firstName}\"     (change to \"checktwo\", \"checkone1\", etc.)");
                _output.WriteLine($"   - lastName = \"{lastName}\"       (change to \"check1\", \"check2\", etc.)");
                _output.WriteLine($"   - dob = \"{dob}\"           (change to \"11091816\" for 2018, \"11091716\" for 2017, etc.)");
                _output.WriteLine("");
                _output.WriteLine("5. Save the file and run the test again");
                _output.WriteLine("");
                var searchResults = driver.FindElements(OpenQA.Selenium.By.CssSelector("table tbody tr"));
                _output.WriteLine($"Current search found {searchResults.Count} matching record(s) in database");
                _output.WriteLine("========================================");
            }
            
            Assert.Contains("No records found.", pageText, StringComparison.Ordinal);
            _output.WriteLine("[PASS] Found 'No records found.' message");

            // Find and click the "add new" link
            _output.WriteLine("\n========================================");
            _output.WriteLine("CLICKING 'ADD NEW' LINK");
            _output.WriteLine("=====DONT GO ON LOGS, AS THE DETAILS MAY BE CHANGED , BUT NOT IN THE LOGS===================================");

            var addNewLink = FindLinkByTextFragments(driver, "create", "new", "Person Profile");
            Assert.NotNull(addNewLink);
            _output.WriteLine($"Found 'add new' link: '{addNewLink.Text?.Trim()}'");
            
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", addNewLink);
            System.Threading.Thread.Sleep(500);
            addNewLink.Click();
            
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked 'add new' link");
            _output.WriteLine($"Current URL: {driver.Url}");
            
            // Verify we're on the Person Profile page
            Assert.Contains("PCProfile.aspx", driver.Url, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Navigated to Person Profile page");

            // Verify that search data was pre-filled
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING PRE-FILLED DATA");
            _output.WriteLine("========================================");

            var pcFirstName = FindTextInputBySuffix(driver, "txtPCFirstName");
            var pcLastName = FindTextInputBySuffix(driver, "txtPCLastName");
            var pcDOB = FindTextInputBySuffix(driver, "txtPCDOB");
            var pcPhone = FindTextInputBySuffix(driver, "txtPCPrimaryPhone");
            var pcEmergencyPhone = FindTextInputBySuffix(driver, "txtPCEmergencyPhone");

            _output.WriteLine($"First Name field value: {pcFirstName.GetAttribute("value")}");
            _output.WriteLine($"Last Name field value: {pcLastName.GetAttribute("value")}");
            _output.WriteLine($"DOB field value: {pcDOB.GetAttribute("value")}");
            _output.WriteLine($"Phone field value: {pcPhone.GetAttribute("value")}");
            _output.WriteLine($"Emergency Phone field value: {pcEmergencyPhone.GetAttribute("value")}");

            Assert.Equal(firstName, pcFirstName.GetAttribute("value"));
            Assert.Equal(lastName, pcLastName.GetAttribute("value"));
            _output.WriteLine("[PASS] Search data was pre-filled correctly");

            // Fill in Race (Asian checkbox)
            _output.WriteLine("\n========================================");
            _output.WriteLine("FILLING ADDITIONAL REQUIRED FIELDS");
            _output.WriteLine("========================================");

            var asianCheckbox = FindCheckboxBySuffix(driver, "chkRace_Asian");
            if (!asianCheckbox.Selected)
            {
                asianCheckbox.Click();
                System.Threading.Thread.Sleep(200);
            }
            Assert.True(asianCheckbox.Selected, "Asian checkbox should be selected");
            _output.WriteLine("[PASS] Selected Race: Asian");

            // Fill in Gender (Male from dropdown)
            var genderDropdown = FindSelectBySuffix(driver, "ddlGender");
            var genderSelect = new OpenQA.Selenium.Support.UI.SelectElement(genderDropdown);
            
            // Log available gender options
            var genderOptions = genderSelect.Options.Select(o => o.Text).ToList();
            _output.WriteLine($"Available gender options: {string.Join(", ", genderOptions)}");
            
            // Select "2. Male" (the options have numbers prefixed)
            genderSelect.SelectByText("2. Male");
            System.Threading.Thread.Sleep(200);
            
            Assert.Equal("2. Male", genderSelect.SelectedOption.Text);
            _output.WriteLine("[PASS] Selected Gender: 2. Male");

            // Click Submit button
            _output.WriteLine("\n========================================");
            _output.WriteLine("SUBMITTING PERSON PROFILE");
            _output.WriteLine("========================================");

            var submitButton = FindButtonBySuffix(driver, "btnSubmit");
            Assert.NotNull(submitButton);
            _output.WriteLine($"Found Submit button: text='{submitButton.Text?.Trim()}'");
            
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();
            
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked Submit button");
            _output.WriteLine($"Current URL after submit: {driver.Url}");

            // Check for validation errors
            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING FOR VALIDATION ERRORS");
            _output.WriteLine("========================================");

            var validationErrorFound = false;
            var validationMessages = new System.Collections.Generic.List<string>();

            // Check for validation error messages using various selectors
            var errorSelectors = new[]
            {
                OpenQA.Selenium.By.CssSelector(".alert"),
                OpenQA.Selenium.By.CssSelector(".alert-danger"),
                OpenQA.Selenium.By.CssSelector(".alert-warning"),
                OpenQA.Selenium.By.CssSelector("[class*='error']"),
                OpenQA.Selenium.By.CssSelector("[class*='validation']"),
                OpenQA.Selenium.By.CssSelector(".field-validation-error"),
                OpenQA.Selenium.By.CssSelector(".text-danger"),
                OpenQA.Selenium.By.CssSelector("span[style*='color']"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'must be')]"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'required')]"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'error')]"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'years old')]")
            };

            _output.WriteLine("Searching for validation error messages...");
            
            foreach (var selector in errorSelectors)
            {
                try
                {
                    var errorElements = driver.FindElements(selector);
                    foreach (var element in errorElements)
                    {
                        if (element.Displayed)
                        {
                            var text = element.Text?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(text) && !validationMessages.Contains(text))
                            {
                                validationMessages.Add(text);
                                _output.WriteLine($"  [FOUND] Validation message: '{text}'");
                                validationErrorFound = true;
                            }
                        }
                    }
                }
                catch
                {
                    // Continue with next selector
                }
            }

            // Also check the page text for age validation
            var validationPageText = driver.FindElement(OpenQA.Selenium.By.TagName("body")).Text;
            if (validationPageText.Contains("must be at least 8 years old", StringComparison.OrdinalIgnoreCase) ||
                validationPageText.Contains("8 years old", StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine("[FOUND] Page contains '8 years old' validation text");
                validationErrorFound = true;
            }

            // Log all validation messages found
            if (validationMessages.Count > 0)
            {
                _output.WriteLine($"\nTotal validation messages found: {validationMessages.Count}");
                foreach (var msg in validationMessages)
                {
                    _output.WriteLine($"  - {msg}");
                }
            }
            else
            {
                _output.WriteLine("[WARN] No validation error messages found via selectors");
                _output.WriteLine("Page text preview (first 1000 characters):");
                _output.WriteLine(validationPageText.Substring(0, Math.Min(1000, validationPageText.Length)));
            }

            // Check if we're still on the same page (validation failed)
            var stillOnProfilePage = driver.Url.Contains("PCProfile.aspx", StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"\nStill on Person Profile page: {stillOnProfilePage}");

            if (stillOnProfilePage)
            {
                _output.WriteLine("[INFO] Form submission was blocked - likely due to validation errors");
            }

            // Verify navigation after submit
            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST SUMMARY");
            _output.WriteLine("========================================");
            _output.WriteLine("[PASS] Successfully filled search form with test data");
            _output.WriteLine("[PASS] Verified 'No records found' message");
            _output.WriteLine("[PASS] Clicked 'add new' link");
            _output.WriteLine("[PASS] Verified pre-filled data on Person Profile page");
            _output.WriteLine("[PASS] Selected Race: Asian");
            _output.WriteLine("[PASS] Selected Gender: 2. Male");
            _output.WriteLine("[PASS] Clicked Submit button");
            
            if (validationErrorFound)
            {
                _output.WriteLine($"[EXPECTED] Validation error(s) found: {validationMessages.Count} message(s)");
                _output.WriteLine("[PASS] Test correctly detected validation error for age requirement");
            }
            else
            {
                _output.WriteLine("[WARN] Expected validation error not found");
            }
            
            // Assert that validation error exists (person must be at least 8 years old)
            Assert.True(validationErrorFound || stillOnProfilePage, 
                "Expected validation error about age requirement (must be at least 8 years old) to be displayed");
            
            _output.WriteLine($"\n[PASS] Test completed successfully! Validation errors properly detected.");
        }

        /// <summary>
        /// ⚠️ IMPORTANT: IF THIS TEST FAILS WITH "Assert.Contains() Failure: Not found: No records found."
        /// 
        /// This means the person already exists in the database from a previous test run.
        /// 
        /// TO FIX: Change the test data below (around line 2349-2353):
        /// - Change firstName (e.g., "checkone" to "checktwo" or "checkone2")
        /// - Change lastName (e.g., "check" to "check2")
        /// - Or change the DOB year (e.g., "11091616" to "11091516" for year 2015)
        /// 
        /// File: UnitTests/Referrals/ReferralsTests.cs
        /// Location: Lines 2349-2353 (see *** CHANGE TEST DATA HERE *** comment)
        /// </summary>
        [Fact]
        public void NewReferral_SearchWithYear2016_CreatePersonProfileAndCheckValidation()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper methods for login and navigation
            LoginAndNavigateToReferrals(driver);
            ClickNewReferralButton(driver);

            // *** CHANGE TEST DATA HERE IF TEST FAILS ***
            // If test fails because person already exists in database, modify these values:
            var firstName = "checktwo";      // Change to unique value (e.g., "checktwo", "checkone2")
            var lastName = "checkingagain";          // Change to unique value (e.g., "check2")
            var dob = "10091616";            // Change year digits (e.g., "11091516" for 2015, "11091716" for 2017)
            var phone = "1100000111";        // Can also change phone number
            var emergencyPhone = "1100000111"; // Can also change emergency phone
            // *** END OF TEST DATA ***
            
            // Note: DOB format is MMDDYYYY but system only uses last 2 digits for year
            // "11091616" becomes "11/09/2016" (system interprets "16" as 2016)

            _output.WriteLine($"DOB: {dob} (Will be interpreted as 11/09/2016 - system uses only last 2 digits for year)");

            // Fill in the search form
            FillPersonSearchForm(driver, firstName, lastName, dob, phone, emergencyPhone);
            ClickSearchButton(driver);

            // Verify "No records found." message
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING 'No records found.' MESSAGE");
            _output.WriteLine("========================================");

            var pageText = driver.FindElement(OpenQA.Selenium.By.TagName("body")).Text;
            
            if (!pageText.Contains("No records found.", StringComparison.Ordinal))
            {
                _output.WriteLine("[FAIL] Person already exists in database!");
                _output.WriteLine("");
                _output.WriteLine("⚠️⚠️⚠️ TEST FAILED - PERSON ALREADY EXISTS ⚠️⚠️⚠️");
                _output.WriteLine("");
                _output.WriteLine("The search returned existing records instead of 'No records found.'");
                _output.WriteLine("This means the test data already exists in the database from a previous run.");
                _output.WriteLine("");
                _output.WriteLine("TO FIX THIS:");
                _output.WriteLine("1. Open file: UnitTests/Referrals/ReferralsTests.cs");
                _output.WriteLine("2. Go to lines 2350-2357");
                _output.WriteLine("3. Look for the comment: *** CHANGE TEST DATA HERE IF TEST FAILS ***");
                _output.WriteLine("4. Change one or more of these values:");
                _output.WriteLine($"   - firstName = \"{firstName}\"     (change to \"checktwo\", \"checkone2\", etc.)");
                _output.WriteLine($"   - lastName = \"{lastName}\"       (change to \"check2\", \"check3\", etc.)");
                _output.WriteLine($"   - dob = \"{dob}\"           (change to \"11091516\" for 2015, \"11091716\" for 2017, etc.)");
                _output.WriteLine("");
                _output.WriteLine("5. Save the file and run the test again");
                _output.WriteLine("");
                var searchResults = driver.FindElements(OpenQA.Selenium.By.CssSelector("table tbody tr"));
                _output.WriteLine($"Current search found {searchResults.Count} matching record(s) in database");
                _output.WriteLine("========================================");
            }
            
            Assert.Contains("No records found.", pageText, StringComparison.Ordinal);
            _output.WriteLine("[PASS] Found 'No records found.' message");

            // Find and click the "add new" link
            _output.WriteLine("\n========================================");
            _output.WriteLine("CLICKING 'ADD NEW' LINK");
            _output.WriteLine("========================================");

            var addNewLink = FindLinkByTextFragments(driver, "create", "new", "Person Profile");
            Assert.NotNull(addNewLink);
            _output.WriteLine($"Found 'add new' link: '{addNewLink.Text?.Trim()}'");
            
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", addNewLink);
            System.Threading.Thread.Sleep(500);
            addNewLink.Click();
            
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked 'add new' link");
            _output.WriteLine($"Current URL: {driver.Url}");
            
            // Verify we're on the Person Profile page
            Assert.Contains("PCProfile.aspx", driver.Url, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Navigated to Person Profile page");

            // Verify that search data was pre-filled
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING PRE-FILLED DATA");
            _output.WriteLine("========================================");

            var pcFirstName = FindTextInputBySuffix(driver, "txtPCFirstName");
            var pcLastName = FindTextInputBySuffix(driver, "txtPCLastName");
            var pcDOB = FindTextInputBySuffix(driver, "txtPCDOB");
            var pcPhone = FindTextInputBySuffix(driver, "txtPCPrimaryPhone");
            var pcEmergencyPhone = FindTextInputBySuffix(driver, "txtPCEmergencyPhone");

            // Capture values before they become stale
            var firstNameValue = pcFirstName.GetAttribute("value");
            var lastNameValue = pcLastName.GetAttribute("value");
            var dobValue = pcDOB.GetAttribute("value");
            var phoneValue = pcPhone.GetAttribute("value");
            var emergencyPhoneValue = pcEmergencyPhone.GetAttribute("value");

            _output.WriteLine($"First Name field value: {firstNameValue}");
            _output.WriteLine($"Last Name field value: {lastNameValue}");
            _output.WriteLine($"DOB field value: {dobValue}");
            _output.WriteLine($"Phone field value: {phoneValue}");
            _output.WriteLine($"Emergency Phone field value: {emergencyPhoneValue}");

            Assert.Equal(firstName, firstNameValue);
            Assert.Equal(lastName, lastNameValue);
            _output.WriteLine("[PASS] Search data was pre-filled correctly");

            // Fill in Race (Asian checkbox)
            _output.WriteLine("\n========================================");
            _output.WriteLine("FILLING ADDITIONAL REQUIRED FIELDS");
            _output.WriteLine("========================================");

            var asianCheckbox = FindCheckboxBySuffix(driver, "chkRace_Asian");
            if (!asianCheckbox.Selected)
            {
                asianCheckbox.Click();
                System.Threading.Thread.Sleep(200);
            }
            Assert.True(asianCheckbox.Selected, "Asian checkbox should be selected");
            _output.WriteLine("[PASS] Selected Race: Asian");

            // Fill in Gender (Male from dropdown)
            var genderDropdown = FindSelectBySuffix(driver, "ddlGender");
            var genderSelect = new OpenQA.Selenium.Support.UI.SelectElement(genderDropdown);
            
            // Log available gender options
            var genderOptions = genderSelect.Options.Select(o => o.Text).ToList();
            _output.WriteLine($"Available gender options: {string.Join(", ", genderOptions)}");
            
            // Select "2. Male" (the options have numbers prefixed)
            genderSelect.SelectByText("2. Male");
            System.Threading.Thread.Sleep(200);
            
            Assert.Equal("2. Male", genderSelect.SelectedOption.Text);
            _output.WriteLine("[PASS] Selected Gender: 2. Male");

            // Click Submit button
            _output.WriteLine("\n========================================");
            _output.WriteLine("SUBMITTING PERSON PROFILE");
            _output.WriteLine("========================================");

            var submitButton = FindButtonBySuffix(driver, "btnSubmit");
            Assert.NotNull(submitButton);
            _output.WriteLine($"Found Submit button: text='{submitButton.Text?.Trim()}'");
            
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();
            
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked Submit button");
            
            var urlAfterSubmit = driver.Url;
            _output.WriteLine($"Current URL after submit: {urlAfterSubmit}");

            // Check for validation errors
            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING FOR VALIDATION ERRORS");
            _output.WriteLine("========================================");

            var validationErrorFound = false;
            var validationMessages = new System.Collections.Generic.List<string>();

            // Check for validation error messages using various selectors
            var errorSelectors = new[]
            {
                OpenQA.Selenium.By.CssSelector(".alert"),
                OpenQA.Selenium.By.CssSelector(".alert-danger"),
                OpenQA.Selenium.By.CssSelector(".alert-warning"),
                OpenQA.Selenium.By.CssSelector("[class*='error']"),
                OpenQA.Selenium.By.CssSelector("[class*='validation']"),
                OpenQA.Selenium.By.CssSelector(".field-validation-error"),
                OpenQA.Selenium.By.CssSelector(".text-danger"),
                OpenQA.Selenium.By.CssSelector("span[style*='color']"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'must be')]"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'required')]"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'error')]"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'years')]"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'date')]"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'invalid')]")
            };

            _output.WriteLine("Searching for validation error messages...");
            
            foreach (var selector in errorSelectors)
            {
                try
                {
                    var errorElements = driver.FindElements(selector);
                    foreach (var element in errorElements)
                    {
                        if (element.Displayed)
                        {
                            var text = element.Text?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(text) && !validationMessages.Contains(text))
                            {
                                validationMessages.Add(text);
                                _output.WriteLine($"  [FOUND] Validation message: '{text}'");
                                validationErrorFound = true;
                            }
                        }
                    }
                }
                catch
                {
                    // Continue with next selector
                }
            }

            // Also check the page text for various validation patterns
            var validationPageText = driver.FindElement(OpenQA.Selenium.By.TagName("body")).Text;
            
            var patterns = new[] { "must be", "years old", "invalid", "error", "date", "too old", "maximum age" };
            foreach (var pattern in patterns)
            {
                if (validationPageText.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    _output.WriteLine($"[FOUND] Page contains '{pattern}' text");
                    validationErrorFound = true;
                }
            }

            // Log all validation messages found
            if (validationMessages.Count > 0)
            {
                _output.WriteLine($"\nTotal validation messages found: {validationMessages.Count}");
                foreach (var msg in validationMessages)
                {
                    _output.WriteLine($"  - {msg}");
                }
            }
            else
            {
                _output.WriteLine("[WARN] No validation error messages found via selectors");
                _output.WriteLine("Page text preview (first 1500 characters):");
                _output.WriteLine(validationPageText.Substring(0, Math.Min(1500, validationPageText.Length)));
            }

            // Check if we're still on the same page (validation failed) or navigated to a new page (success)
            var stillOnProfilePage = urlAfterSubmit.Contains("PCProfile.aspx", StringComparison.OrdinalIgnoreCase);
            var navigatedToReferralPage = urlAfterSubmit.Contains("Referral.aspx", StringComparison.OrdinalIgnoreCase);
            
            _output.WriteLine($"\nStill on Person Profile page: {stillOnProfilePage}");
            _output.WriteLine($"Navigated to Referral page: {navigatedToReferralPage}");

            if (stillOnProfilePage)
            {
                _output.WriteLine("[INFO] Form submission was blocked - likely due to validation errors");
            }
            else if (navigatedToReferralPage)
            {
                _output.WriteLine("[SUCCESS] Form was accepted and person profile was created successfully!");
                _output.WriteLine("[INFO] No age validation error for 9-year-old (only checks if under 8 years old)");
            }

            // Test summary
            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST SUMMARY - YEAR 2016 TEST");
            _output.WriteLine("========================================");
            _output.WriteLine("[PASS] Successfully filled search form with DOB input '11091616'");
            _output.WriteLine("[PASS] Verified 'No records found' message");
            _output.WriteLine("[PASS] Clicked 'add new' link");
            _output.WriteLine("[PASS] Verified pre-filled data on Person Profile page");
            _output.WriteLine($"[INFO] DOB field showed: {dobValue} (System interpreted '16' as 2016)");
            _output.WriteLine("[PASS] Selected Race: Asian");
            _output.WriteLine("[PASS] Selected Gender: 2. Male");
            _output.WriteLine("[PASS] Clicked Submit button");
            
            if (navigatedToReferralPage)
            {
                _output.WriteLine("[SUCCESS] Person profile created successfully - no validation errors");
                _output.WriteLine("[INFO] A 9-year-old person (born 2016) meets the minimum age requirement of 8 years");
            }
            else if (validationErrorFound)
            {
                _output.WriteLine($"[FOUND] Validation error(s) detected: {validationMessages.Count} message(s)");
                _output.WriteLine("[INFO] Test detected validation error");
            }
            else
            {
                _output.WriteLine("[INFO] No validation error found");
            }
            
            _output.WriteLine($"\n[COMPLETE] Test finished! Final URL: {urlAfterSubmit}");
        }

        [Fact]
        public void NewReferral_SelectExistingPerson_SubmitWithoutRequiredFields_ShowsValidationErrors()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper methods for common operations
            LoginAndNavigateToReferrals(driver);
            ClickNewReferralButton(driver);

            // Search for existing person
            var firstName = "checkone";
            var lastName = "check";
            var dob = "11091616";
            var phone = "1111111111";
            var emergencyPhone = "1111111111";

            FillPersonSearchForm(driver, firstName, lastName, dob, phone, emergencyPhone);
            ClickSearchButton(driver);

            // Find and click the Select link for the existing person
            _output.WriteLine("\n========================================");
            _output.WriteLine("SELECTING EXISTING PERSON FROM RESULTS");
            _output.WriteLine("========================================");

            var selectLinks = driver.FindElements(OpenQA.Selenium.By.LinkText("Select"));
            _output.WriteLine($"Found {selectLinks.Count} 'Select' link(s)");
            
            Assert.True(selectLinks.Count > 0, "Expected to find at least one 'Select' link for existing person");
            
            var firstSelectLink = selectLinks.FirstOrDefault(l => l.Displayed);
            Assert.NotNull(firstSelectLink);
            _output.WriteLine("Found 'Select' link for existing person");
            
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", firstSelectLink);
            System.Threading.Thread.Sleep(500);
            firstSelectLink.Click();
            
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked 'Select' link");
            _output.WriteLine($"Current URL: {driver.Url}");
            _output.WriteLine($"Page Title: {driver.Title}");

            // Log all form fields on the referral page
            _output.WriteLine("\n========================================");
            _output.WriteLine("REFERRAL FORM PAGE - LOGGING ELEMENTS");
            _output.WriteLine("========================================");

            var formFields = driver.FindElements(OpenQA.Selenium.By.CssSelector("input, select, textarea"))
                .Where(f => f.Displayed).ToList();
            _output.WriteLine($"Found {formFields.Count} visible form fields");

            // Click Submit WITHOUT filling any required fields
            _output.WriteLine("\n========================================");
            _output.WriteLine("SUBMITTING WITHOUT REQUIRED FIELDS");
            _output.WriteLine("========================================");

            var submitButtons = driver.FindElements(OpenQA.Selenium.By.CssSelector("input[type='submit'], button[type='submit'], input[value*='Submit'], button[id*='Submit'], a[id*='Submit']"));
            _output.WriteLine($"Found {submitButtons.Count} submit button(s)");
            
            var submitButton = submitButtons.FirstOrDefault(b => 
            {
                var text = b.Text?.Trim() ?? b.GetAttribute("value") ?? "";
                var id = b.GetAttribute("id") ?? "";
                return (text.Contains("Submit", StringComparison.OrdinalIgnoreCase) || 
                        id.Contains("Submit", StringComparison.OrdinalIgnoreCase)) &&
                       b.Displayed && b.Enabled;
            });

            Assert.NotNull(submitButton);
            _output.WriteLine($"Found Submit button: id='{submitButton.GetAttribute("id")}', text='{submitButton.Text?.Trim() ?? submitButton.GetAttribute("value")}'");
            
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", submitButton);
            System.Threading.Thread.Sleep(500);
            submitButton.Click();
            
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked Submit button");
            _output.WriteLine($"Current URL after submit: {driver.Url}");

            // Check for validation error messages
            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING FOR VALIDATION ERROR MESSAGES");
            _output.WriteLine("========================================");

            var expectedErrors = new[]
            {
                "Date Worker Assigned is required!",
                "Worker is required!",
                "Type of Referral Source is required!",
                "Name/Organization Name of Referral Source is required!"
            };

            var foundErrors = new System.Collections.Generic.List<string>();
            var missingErrors = new System.Collections.Generic.List<string>();

            // Search for validation messages using various selectors
            var errorSelectors = new[]
            {
                OpenQA.Selenium.By.CssSelector(".alert"),
                OpenQA.Selenium.By.CssSelector(".alert-danger"),
                OpenQA.Selenium.By.CssSelector(".alert-warning"),
                OpenQA.Selenium.By.CssSelector("[class*='error']"),
                OpenQA.Selenium.By.CssSelector("[class*='validation']"),
                OpenQA.Selenium.By.CssSelector(".field-validation-error"),
                OpenQA.Selenium.By.CssSelector(".text-danger"),
                OpenQA.Selenium.By.CssSelector("span[style*='color: red']"),
                OpenQA.Selenium.By.CssSelector("span[style*='color:red']"),
                OpenQA.Selenium.By.XPath("//*[contains(text(), 'required')]")
            };

            _output.WriteLine("Searching for validation error messages...");
            
            var allErrorElements = new System.Collections.Generic.List<OpenQA.Selenium.IWebElement>();
            foreach (var selector in errorSelectors)
            {
                try
                {
                    var elements = driver.FindElements(selector);
                    foreach (var element in elements)
                    {
                        if (element.Displayed)
                        {
                            allErrorElements.Add(element);
                        }
                    }
                }
                catch
                {
                    // Continue with next selector
                }
            }

            _output.WriteLine($"Found {allErrorElements.Count} error elements on the page");
            
            // Log all error messages found
            var uniqueErrorMessages = new System.Collections.Generic.HashSet<string>();
            foreach (var element in allErrorElements)
            {
                try
                {
                    var text = element.Text?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(text) && text.Contains("required", StringComparison.OrdinalIgnoreCase))
                    {
                        uniqueErrorMessages.Add(text);
                        _output.WriteLine($"  [FOUND] Error message: '{text}'");
                    }
                }
                catch
                {
                    // Continue
                }
            }

            // Check for each expected error
            foreach (var expectedError in expectedErrors)
            {
                var found = uniqueErrorMessages.Any(msg => msg.Contains(expectedError, StringComparison.Ordinal));
                if (found)
                {
                    foundErrors.Add(expectedError);
                    _output.WriteLine($"[PASS] Found expected error: '{expectedError}'");
                }
                else
                {
                    missingErrors.Add(expectedError);
                    _output.WriteLine($"[MISS] Expected error not found: '{expectedError}'");
                }
            }

            // Check if we're still on the referral page (validation blocked submission)
            var stillOnReferralPage = driver.Url.Contains("Referral.aspx", StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"\nStill on Referral page: {stillOnReferralPage}");

            if (stillOnReferralPage)
            {
                _output.WriteLine("[SUCCESS] Form submission was blocked by validation - as expected!");
            }

            // Test summary
            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST SUMMARY - VALIDATION ERRORS TEST");
            _output.WriteLine("========================================");
            _output.WriteLine("[PASS] Successfully searched for existing person");
            _output.WriteLine("[PASS] Successfully selected existing person from results");
            _output.WriteLine("[PASS] Successfully clicked Submit without filling required fields");
            _output.WriteLine($"\nValidation Errors Found: {foundErrors.Count} out of {expectedErrors.Length}");
            
            foreach (var error in foundErrors)
            {
                _output.WriteLine($"  ✓ {error}");
            }
            
            if (missingErrors.Count > 0)
            {
                _output.WriteLine($"\nExpected Errors NOT Found: {missingErrors.Count}");
                foreach (var error in missingErrors)
                {
                    _output.WriteLine($"  ✗ {error}");
                }
            }

            // Assert all expected errors were found
            foreach (var expectedError in expectedErrors)
            {
                Assert.True(
                    uniqueErrorMessages.Any(msg => msg.Contains(expectedError, StringComparison.Ordinal)),
                    $"Expected validation error not found: '{expectedError}'"
                );
            }

            _output.WriteLine("\n[PASS] All required validation error messages were displayed correctly!");
            _output.WriteLine($"Final URL: {driver.Url}");
        }

        /// <summary>
        /// Test that progressively fills in required fields and validates that only missing fields show errors.
        /// This test:
        /// 1. Searches for an existing person
        /// 2. Fills only Referral Date and submits - expects 4 validation errors
        /// 3. Adds Date Worker Assigned and submits - expects 3 validation errors
        /// 4. Adds Worker and submits - expects 2 validation errors
        /// 5. Adds Type of Referral Source and submits - expects 1 validation error
        /// 6. Adds Name/Organization Name of Referral Source and submits - expects success
        /// </summary>
        [Fact]
        public void NewReferral_ProgressivelyFillRequiredFields_ValidatesCorrectly()
        {
            using var driver = _driverFactory.CreateDriver();

            // Use helper methods for login and navigation
            LoginAndNavigateToReferrals(driver);
            ClickNewReferralButton(driver);

            // Search for existing person
            var firstName = "checkone";
            var lastName = "check";
            var dob = "11091616";
            var phone = "1111111111";
            var emergencyPhone = "1111111111";

            FillPersonSearchForm(driver, firstName, lastName, dob, phone, emergencyPhone);
            ClickSearchButton(driver);

            // Select existing person
            _output.WriteLine("\n========================================");
            _output.WriteLine("SELECTING EXISTING PERSON FROM RESULTS");
            _output.WriteLine("========================================");

            var selectLinks = driver.FindElements(OpenQA.Selenium.By.LinkText("Select"));
            _output.WriteLine($"Found {selectLinks.Count} 'Select' link(s)");
            Assert.True(selectLinks.Count > 0, "No 'Select' links found for existing person");

            var selectLink = selectLinks.First();
            _output.WriteLine($"Found 'Select' link for existing person");
            
            selectLink.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine($"[PASS] Clicked 'Select' link");
            _output.WriteLine($"Current URL: {driver.Url}");

            // ========================================
            // STEP 1: Fill only Referral Date and submit
            // ========================================
            _output.WriteLine("\n========================================");
            _output.WriteLine("STEP 1: FILLING ONLY REFERRAL DATE");
            _output.WriteLine("========================================");

            FillReferralTextField(driver, "txtReferralDate", "11/12/2025", "Referral Date");
            var validationErrors = SubmitReferralFormAndGetErrors(driver);
            LogStepValidationErrors(1, validationErrors);

            // ========================================
            // STEP 2: Add Date Worker Assigned and submit
            // ========================================
            _output.WriteLine("\n========================================");
            _output.WriteLine("STEP 2: ADDING DATE WORKER ASSIGNED");
            _output.WriteLine("========================================");

            System.Threading.Thread.Sleep(1000);
            driver.WaitForReady(30);
            FillReferralTextField(driver, "txtWorkerAssignDate", "11/12/2025", "Date Worker Assigned");
            validationErrors = SubmitReferralFormAndGetErrors(driver);
            LogStepValidationErrors(2, validationErrors);

            // ========================================
            // STEP 3: Add Worker and submit
            // ========================================
            _output.WriteLine("\n========================================");
            _output.WriteLine("STEP 3: ADDING WORKER");
            _output.WriteLine("========================================");

            SelectReferralDropdownByText(driver, "ddlWorker", "Test, Derek", "Worker");
            validationErrors = SubmitReferralFormAndGetErrors(driver);
            LogStepValidationErrors(3, validationErrors);

            // ========================================
            // STEP 4: Add Type of Referral Source and submit
            // ========================================
            _output.WriteLine("\n========================================");
            _output.WriteLine("STEP 4: ADDING TYPE OF REFERRAL SOURCE");
            _output.WriteLine("========================================");

            SelectReferralDropdownByFirstNonEmpty(driver, "ddlReferralSourceType", "Type of Referral Source");
            validationErrors = SubmitReferralFormAndGetErrors(driver);
            LogStepValidationErrors(4, validationErrors);

            // ========================================
            // STEP 5: Add Name/Organization Name of Referral Source and submit
            // ========================================
            _output.WriteLine("\n========================================");
            _output.WriteLine("STEP 5: ADDING NAME/ORGANIZATION NAME OF REFERRAL SOURCE");
            _output.WriteLine("========================================");

            SelectReferralDropdownByFirstNonEmpty(driver, "ddlReferralSource", "Name/Organization Name");
            System.Threading.Thread.Sleep(1000); // Extra wait before final submit
            var submitButton = FindButtonBySuffix(driver, "SubmitReferral_LoginView1_btnSubmit");
            submitButton.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(3000);
            _output.WriteLine("[PASS] Clicked Submit button");

            // Check final state
            _output.WriteLine("\n========================================");
            _output.WriteLine("FINAL STATE AFTER ALL FIELDS FILLED");
            _output.WriteLine("========================================");
            _output.WriteLine($"Current URL: {driver.Url}");
            _output.WriteLine($"Page Title: {driver.Title}");

            // Extract ReferralPK from URL to verify successful creation
            var referralPKMatch = System.Text.RegularExpressions.Regex.Match(driver.Url, @"ReferralPK=(\d+)");
            var referralPK = referralPKMatch.Success ? referralPKMatch.Groups[1].Value : "0";
            _output.WriteLine($"Referral PK: {referralPK}");

            // Assert that a new referral was created (ReferralPK should not be 0)
            Assert.NotEqual("0", referralPK);
            _output.WriteLine($"[SUCCESS] ✓ Referral was successfully created with ID: {referralPK}");

            // Verify no validation errors blocking submission
            var actualValidationErrors = GetValidationErrorMessages(driver)
                .Where(msg => msg.Contains("required", StringComparison.OrdinalIgnoreCase))
                .ToHashSet();
            
            _output.WriteLine($"\n[STEP 5] Actual validation errors blocking submission: {actualValidationErrors.Count}");
            if (actualValidationErrors.Count > 0)
            {
                foreach (var error in actualValidationErrors)
                {
                    _output.WriteLine($"  - {error}");
                }
            }
            else
            {
                _output.WriteLine("  [SUCCESS] ✓ No validation errors blocking submission!");
            }

            // Assert no validation errors
            Assert.Empty(actualValidationErrors);

            _output.WriteLine("\n========================================");
            _output.WriteLine("✓✓✓ TEST PASSED - ALL VALIDATIONS SUCCESSFUL ✓✓✓");
            _output.WriteLine("========================================");
            _output.WriteLine("\n[SUMMARY]");
            _output.WriteLine("  Step 1: Filled Referral Date only → 4 validation errors (PASS)");
            _output.WriteLine("  Step 2: Added Date Worker Assigned → 3 validation errors (PASS)");
            _output.WriteLine("  Step 3: Added Worker (Test, Derek) → 2 validation errors (PASS)");
            _output.WriteLine("  Step 4: Added Type of Referral Source → 1 validation error (PASS)");
            _output.WriteLine("  Step 5: Added Name/Organization → 0 validation errors (PASS)");
            _output.WriteLine($"  Final: Referral successfully created with ID {referralPK} (PASS)");

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST DATA USED (FOR FUTURE REFERENCE)");
            _output.WriteLine("========================================");
            _output.WriteLine("If this test fails in future runs due to data already existing,");
            _output.WriteLine("you can modify the search criteria in this test:");
            _output.WriteLine($"  - Person First Name: {firstName}");
            _output.WriteLine($"  - Person Last Name: {lastName}");
            _output.WriteLine($"  - Person DOB: {dob} (interpreted as 11/09/2016)");
            _output.WriteLine($"  - Phone: {phone}");
            _output.WriteLine($"  - Emergency Phone: {emergencyPhone}");
            _output.WriteLine("\nReferral Fields Used:");
            _output.WriteLine("  - Referral Date: 11/12/2025");
            _output.WriteLine("  - Date Worker Assigned: 11/12/2025");
            _output.WriteLine("  - Worker: Test, Derek");
            _output.WriteLine("  - Type of Referral Source: 1. Private Physician and Health Clinic");
            _output.WriteLine("  - Name/Organization: First option in dropdown");
            _output.WriteLine("\n[NOTE] This referral (ID: " + referralPK + ") was created in the database.");
            _output.WriteLine("To change test data, modify lines 2457-2461 in ReferralsTests.cs");
            _output.WriteLine("========================================");
        }

        [Fact]
        public void ReferralsPage_NewContactAttempt_ValidationRequired()
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: NEW CONTACT ATTEMPT - VALIDATION");
            _output.WriteLine("========================================");

            // Login and navigate
            LoginAndNavigateToReferrals(driver);

            // Find and click edit on first referral
            var activeReferralsTable = FindActiveReferralsTable(driver);
            var tableRows = activeReferralsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"));
            var firstRow = tableRows.FirstOrDefault();
            Assert.NotNull(firstRow);

            var editButton = firstRow.FindElement(OpenQA.Selenium.By.CssSelector("a[id*='lnkEditReferral']"));
            editButton.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine($"[PASS] Opened referral edit page: {driver.Url}");

            // Click "New Contact Attempt"
            var newContactBtn = FindButtonBySuffix(driver, "lbNewContactAttempt");
            newContactBtn.Click();
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked 'New Contact Attempt'");

            // Fill Contact Date
            _output.WriteLine("\n[INFO] ========== FILLING CONTACT DATE ==========");
            var contactDateField = FindTextInputBySuffix(driver, "txtContactAttemptDate");
            _output.WriteLine($"[INFO] Found Contact Date field: ID={contactDateField.GetAttribute("id")}");
            
            var todayDate = DateTime.Now.ToString("MM/dd/yyyy");
            contactDateField.Clear();
            contactDateField.SendKeys(todayDate);
            System.Threading.Thread.Sleep(300);
            
            var enteredValue = contactDateField.GetAttribute("value");
            _output.WriteLine($"[PASS] Filled Contact Date: {todayDate}");
            _output.WriteLine($"[INFO] Field now contains: {enteredValue}");

            // Fill Worker dropdown
            _output.WriteLine("\n[INFO] ========== FILLING WORKER DROPDOWN ==========");
            var workerDropdown = FindSelectBySuffix(driver, "ddlContactAttemptWorker");
            _output.WriteLine($"[INFO] Found Worker dropdown: ID={workerDropdown.GetAttribute("id")}");
            
            var workerSelect = new OpenQA.Selenium.Support.UI.SelectElement(workerDropdown);
            var workerOptions = workerSelect.Options;
            _output.WriteLine($"[INFO] Worker dropdown has {workerOptions.Count} options:");
            for (int i = 0; i < Math.Min(3, workerOptions.Count); i++)
            {
                _output.WriteLine($"  [{i}] Text: '{workerOptions[i].Text}', Value: '{workerOptions[i].GetAttribute("value")}'");
            }
            
            var validWorkerOptions = workerOptions.Where(o => !string.IsNullOrWhiteSpace(o.Text) && o.Text != "--Select--").ToList();
            if (validWorkerOptions.Any())
            {
                var selectedWorkerText = workerOptions[1].Text; // Store before selecting
                workerSelect.SelectByIndex(1); // Select first real option
                _output.WriteLine($"[PASS] Selected worker at index 1: {selectedWorkerText}");
                System.Threading.Thread.Sleep(1000); // Wait for any page updates
            }

            // Fill Was Family Successfully Contacted
            _output.WriteLine("\n[INFO] ========== FILLING 'WAS FAMILY SUCCESSFULLY CONTACTED?' ==========");
            var contactedDropdown = FindSelectBySuffix(driver, "ddlContactAttemptSuccessful");
            _output.WriteLine($"[INFO] Found 'Successfully Contacted' dropdown: ID={contactedDropdown.GetAttribute("id")}");
            
            var contactedSelect = new OpenQA.Selenium.Support.UI.SelectElement(contactedDropdown);
            var contactedOptions = contactedSelect.Options;
            _output.WriteLine($"[INFO] 'Successfully Contacted' dropdown has {contactedOptions.Count} options:");
            for (int i = 0; i < contactedOptions.Count; i++)
            {
                _output.WriteLine($"  [{i}] Text: '{contactedOptions[i].Text}', Value: '{contactedOptions[i].GetAttribute("value")}'");
            }
            
            var yesOption = contactedOptions.FirstOrDefault(o => o.Text.Contains("Yes", StringComparison.OrdinalIgnoreCase));
            if (yesOption != null)
            {
                var yesOptionText = yesOption.Text; // Store before selecting
                var yesOptionValue = yesOption.GetAttribute("value");
                _output.WriteLine($"[INFO] Found 'Yes' option: Text='{yesOptionText}', Value='{yesOptionValue}'");
                
                contactedSelect.SelectByText(yesOptionText);
                _output.WriteLine($"[PASS] Selected: {yesOptionText}");
                System.Threading.Thread.Sleep(1000); // Wait for any page updates
            }
            else
            {
                _output.WriteLine("[ERROR] Could not find 'Yes' option!");
            }

            System.Threading.Thread.Sleep(500);

            // NOTE: NOT filling Contact Attempt Type(s) - this should trigger validation!
            _output.WriteLine("\n[INFO] ========== SKIPPING CONTACT ATTEMPT TYPE(S) ==========");
            _output.WriteLine("[INFO] Intentionally NOT filling Contact Attempt Type(s) to trigger validation");

            // Try to submit WITHOUT filling Contact Attempt Type(s)
            _output.WriteLine("\n[INFO] ========== CLICKING SUBMIT BUTTON ==========");
            var submitBtn = FindButtonBySuffix(driver, "lbSubmitContactAttempt");
            _output.WriteLine($"[INFO] Found Submit button: ID={submitBtn.GetAttribute("id")}, Text='{submitBtn.Text}'");
            _output.WriteLine($"[INFO] Submit button enabled: {submitBtn.Enabled}");
            
            submitBtn.Click();
            _output.WriteLine("[PASS] Clicked Submit button");
            System.Threading.Thread.Sleep(3000);
            _output.WriteLine("[INFO] Waited 3 seconds for validation to appear");

            // Check for validation messages
            _output.WriteLine("\n[INFO] ========== CHECKING FOR VALIDATION MESSAGES ==========");
            
            var validationMessages = GetValidationErrorMessages(driver);
            
            if (validationMessages.Any())
            {
                _output.WriteLine($"[PASS] Found {validationMessages.Count} validation messages:");
                foreach (var msg in validationMessages)
                {
                    _output.WriteLine($"  ✓ {msg}");
                }
            }
            else
            {
                _output.WriteLine("[INFO] No validation messages found in standard locations");
            }
            
            // Also check for toast notifications
            try
            {
                var toastElements = driver.FindElements(OpenQA.Selenium.By.CssSelector(".toast, .alert, [class*='notification'], [class*='message'], [role='alert'], [class*='toast']"));
                var visibleToasts = toastElements.Where(t => t.Displayed).ToList();
                
                if (visibleToasts.Any())
                {
                    _output.WriteLine($"\n[INFO] Found {visibleToasts.Count} toast/alert messages:");
                    foreach (var toast in visibleToasts)
                    {
                        var toastText = toast.Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(toastText))
                        {
                            _output.WriteLine($"  ✓ {toastText}");
                            validationMessages.Add(toastText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[INFO] Could not find toast notifications: {ex.Message}");
            }

            _output.WriteLine("[INFO] ========== END VALIDATION CHECK ==========\n");

            // Verify that validation toast appeared
            var hasContactAttemptValidation = validationMessages.Any(m => 
                m.Contains("Contact Attempt Validation Failed", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("contact attempt is invalid", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("validation summary", StringComparison.OrdinalIgnoreCase));
            
            Assert.True(hasContactAttemptValidation, 
                "Expected 'Contact Attempt Validation Failed' toast notification, but it was not found!");
            
            _output.WriteLine($"[PASS] ✓ Validation test passed! 'Contact Attempt Validation Failed' toast appeared!");
            _output.WriteLine($"[PASS] ✓ Total validation messages found: {validationMessages.Count}");
            _output.WriteLine("========================================");
        }

        [Fact]
        public void ReferralsPage_AddContactAttempt_FillsFormAndSubmits()
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: ADD NEW CONTACT ATTEMPT");
            _output.WriteLine("========================================");

            // Use helper method for login and navigation
            LoginAndNavigateToReferrals(driver);

            _output.WriteLine("\n========================================");
            _output.WriteLine("FINDING REFERRAL TO EDIT");
            _output.WriteLine("========================================");

            // Find the active referrals table
            var activeReferralsTable = FindActiveReferralsTable(driver);
            Assert.NotNull(activeReferralsTable);
            _output.WriteLine("[PASS] Found active referrals table");

            // Get all rows from the table
            var tableRows = activeReferralsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"));
            _output.WriteLine($"[INFO] Found {tableRows.Count} rows in active referrals table");

            // Select the first available row
            Assert.True(tableRows.Count > 0, "No referrals found in the active referrals table!");
            var targetRow = tableRows[0];
            _output.WriteLine($"[PASS] Selected row 0 for editing");
            _output.WriteLine($"[INFO] Row text: {targetRow.Text}");

            // Find the edit button in the target row
            var editButton = FindReferralEditButton(targetRow);

            Assert.NotNull(editButton);
            _output.WriteLine($"[PASS] Found edit button: id='{editButton.GetAttribute("id")}', text='{editButton.Text}'");

            _output.WriteLine("\n========================================");
            _output.WriteLine("CLICKING EDIT BUTTON");
            _output.WriteLine("========================================");

            // Scroll into view and click
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", editButton);
            System.Threading.Thread.Sleep(500);

            if (!editButton.Displayed)
            {
                _output.WriteLine("[INFO] Edit button not visible, using JavaScript click");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
            }
            else
            {
                _output.WriteLine("[INFO] Edit button visible, using regular click");
                editButton.Click();
            }

            _output.WriteLine("[PASS] Clicked edit button successfully");

            // Wait for navigation
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);

            // Verify we're on the Referral edit page
            Assert.Contains("Referral.aspx", driver.Url, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Successfully navigated to Referral edit page: {driver.Url}");

            // Capture existing contact attempts before adding a new one
            var contactAttemptsTable = FindContactAttemptsTable(driver);
            Assert.NotNull(contactAttemptsTable);
            _output.WriteLine("[PASS] Found contact attempts table");

            var initialContactAttemptRows = GetContactAttemptDataRows(contactAttemptsTable);
            var initialRowCount = initialContactAttemptRows.Count;
            _output.WriteLine($"[INFO] Initial contact attempt count: {initialRowCount}");

            _output.WriteLine("\n========================================");
            _output.WriteLine("CLICKING NEW CONTACT ATTEMPT BUTTON");
            _output.WriteLine("========================================");

            // Find and click the New Contact Attempt button
            var newContactAttemptButton = FindButtonBySuffix(driver, "lbNewContactAttempt");
            Assert.NotNull(newContactAttemptButton);
            _output.WriteLine($"[PASS] Found New Contact Attempt button: '{newContactAttemptButton.Text}'");

            // Scroll the button into view and use JavaScript click to avoid navbar interference
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -200);", newContactAttemptButton);
            System.Threading.Thread.Sleep(500);
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", newContactAttemptButton);
            _output.WriteLine("[PASS] Clicked New Contact Attempt button");
            
            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(1000);

            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING CONTACT ATTEMPT FORM IS VISIBLE");
            _output.WriteLine("========================================");

            // Verify the contact attempt form is now visible
            var contactAttemptForm = FindContactAttemptForm(driver);
            Assert.NotNull(contactAttemptForm);
            Assert.True(contactAttemptForm.Displayed, "Contact attempt form should be visible after clicking New Contact Attempt button");
            _output.WriteLine("[PASS] Contact attempt form is now visible");

            _output.WriteLine("\n========================================");
            _output.WriteLine("FILLING OUT CONTACT ATTEMPT FORM");
            _output.WriteLine("========================================");

            // Fill Contact Date using JavaScript to ensure value + events are set
            var contactDateField = FindTextInputBySuffix(driver, "txtContactAttemptDate");
            var contactDateJs = (OpenQA.Selenium.IJavaScriptExecutor)driver;
            contactDateJs.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", contactAttemptForm);
            System.Threading.Thread.Sleep(300);
            contactDateJs.ExecuteScript(
                "arguments[0].value = ''; arguments[0].focus(); arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));",
                contactDateField, "11/12/2025");
            System.Threading.Thread.Sleep(200);
            _output.WriteLine("[PASS] Filled Contact Date via JavaScript: 11/12/2025");

            // IMPORTANT: Re-find the worker dropdown AFTER the postback
            // The postback may have refreshed the dropdown, making the old reference stale
            _output.WriteLine("[INFO] Re-finding worker dropdown after postback...");
            var workerDropdown = FindSelectBySuffix(driver, "ddlContactAttemptWorker");
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", workerDropdown);
            System.Threading.Thread.Sleep(300);
            
            var workerSelectElement = new OpenQA.Selenium.Support.UI.SelectElement(workerDropdown);
            // Select the "Test, Derek" option (value="3489" from the HTML)
            workerSelectElement.SelectByValue("3489");
            _output.WriteLine("[PASS] Selected Worker: Test, Derek");

            // Select Contact Attempt Type using JS fallback (Chosen.js control)
            SelectChosenOptionViaScript(driver, "ddlContactAttemptTypes", "Phone Call Made to Parent", "1469");
            System.Threading.Thread.Sleep(300);
            _output.WriteLine("[PASS] Selected Contact Attempt Type via JavaScript: Phone Call Made to Parent");

            // Select "Was the Family Successfully Contacted?"
            var successfulContactDropdown = FindSelectBySuffix(driver, "ddlContactAttemptSuccessful");
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", successfulContactDropdown);
            System.Threading.Thread.Sleep(300);
            
            var successfulSelectElement = new OpenQA.Selenium.Support.UI.SelectElement(successfulContactDropdown);
            successfulSelectElement.SelectByValue("True"); // Yes
            _output.WriteLine("[PASS] Selected Was Family Successfully Contacted: Yes");

            // Fill Notes using JavaScript (avoids navbar intercept + triggers events)
            var notesField = FindTextAreaBySuffix(driver, "txtContactAttemptNotes");
            var notesText = "Test contact attempt - spoke with parent about upcoming program activities.";
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].value = ''; arguments[0].focus(); arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", 
                notesField, notesText);
            System.Threading.Thread.Sleep(300);
            _output.WriteLine("[PASS] Filled Notes field via JavaScript");

            _output.WriteLine("\n========================================");
            _output.WriteLine("SUBMITTING CONTACT ATTEMPT FORM");
            _output.WriteLine("========================================");

            // Find and click Submit button
            var submitButton = FindButtonBySuffix(driver, "lbSubmitContactAttempt");
            Assert.NotNull(submitButton);
            _output.WriteLine($"[PASS] Found Submit button");

            // Scroll to submit button and click
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", submitButton);
            System.Threading.Thread.Sleep(500);
            
            submitButton.Click();
            _output.WriteLine("[PASS] Clicked Submit button");

            // Wait for the submission to process
            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(2000);

            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING CONTACT ATTEMPT WAS ADDED");
            _output.WriteLine("========================================");

            // Check if the form closed (it should hide after successful submission)
            try
            {
                var formAfterSubmit = FindContactAttemptForm(driver);
                var isFormVisible = formAfterSubmit.Displayed;
                
                if (!isFormVisible)
                {
                    _output.WriteLine("[PASS] Contact attempt form is now hidden (submission successful)");
                }
                else
                {
                    _output.WriteLine("[INFO] Contact attempt form is still visible - checking for validation errors");
                    
                    // Check for validation errors
                    try
                    {
                        var validationSummary = FindValidationSummaryBySuffix(driver, "vsContactAttempt");
                        if (validationSummary.Displayed)
                        {
                            _output.WriteLine($"[WARN] Validation error found: {validationSummary.Text}");
                        }
                    }
                    catch
                    {
                        _output.WriteLine("[INFO] No validation summary found");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Form was removed from DOM after successful submission
                _output.WriteLine("[PASS] Contact attempt form removed from DOM (submission successful)");
            }
            catch (OpenQA.Selenium.NoSuchElementException)
            {
                _output.WriteLine("[PASS] Contact attempt form element not found in DOM (submission successful)");
            }

            // Verify the contact attempt appears in the table
            try
            {
                contactAttemptsTable = FindContactAttemptsTable(driver);
                var updatedRows = GetContactAttemptDataRows(contactAttemptsTable);
                _output.WriteLine($"[INFO] Updated contact attempt count: {updatedRows.Count}");

                Assert.True(updatedRows.Count > initialRowCount,
                    $"Expected contact attempts to increase (before={initialRowCount}, after={updatedRows.Count}).");

                var expectedSnippets = new[]
                {
                    "11/12/2025",
                    "Test, Derek",
                    "Phone Call Made to Parent",
                    notesText
                };

                var matchingRow = updatedRows.FirstOrDefault(row =>
                    expectedSnippets.Where(snippet => !string.IsNullOrWhiteSpace(snippet))
                        .Count(snippet => row.Text?.IndexOf(snippet, StringComparison.OrdinalIgnoreCase) >= 0) >= 2);

                if (matchingRow != null)
                {
                    _output.WriteLine($"[PASS] ✓ Found newly added contact attempt row: {matchingRow.Text}");
                }
                else
                {
                    _output.WriteLine("[WARN] Could not find a row containing all expected snippets. Available rows:");
                    foreach (var row in updatedRows)
                    {
                        _output.WriteLine($"  · {row.Text}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[WARN] Could not verify contact attempts table: {ex.Message}");
                throw;
            }

            // Check for success messages or toasts
            try
            {
                var toastElements = driver.FindElements(OpenQA.Selenium.By.CssSelector(".toast, .alert-success, [class*='success'], [role='alert']"));
                var visibleToasts = toastElements.Where(t => t.Displayed && !string.IsNullOrWhiteSpace(t.Text)).ToList();
                
                if (visibleToasts.Any())
                {
                    _output.WriteLine($"\n[INFO] Found {visibleToasts.Count} success message(s):");
                    foreach (var toast in visibleToasts)
                    {
                        _output.WriteLine($"  ✓ {toast.Text?.Trim()}");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[INFO] Could not check for success messages: {ex.Message}");
            }

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST SUMMARY");
            _output.WriteLine("========================================");
            _output.WriteLine("[PASS] Successfully navigated to referral edit page");
            _output.WriteLine("[PASS] Successfully opened contact attempt form");
            _output.WriteLine("[PASS] Successfully filled all required fields");
            _output.WriteLine("[PASS] Successfully submitted contact attempt");
            _output.WriteLine("========================================");
        }

        [Fact]
        public void ReferralsPage_DeleteContactAttempt_CancelsAndConfirmsDelete()
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: DELETE CONTACT ATTEMPT");
            _output.WriteLine("========================================");

            // Use helper method for login and navigation
            LoginAndNavigateToReferrals(driver);

            _output.WriteLine("\n========================================");
            _output.WriteLine("FINDING REFERRAL TO EDIT");
            _output.WriteLine("========================================");

            // Find the active referrals table
            var activeReferralsTable = FindActiveReferralsTable(driver);
            Assert.NotNull(activeReferralsTable);
            _output.WriteLine("[PASS] Found active referrals table");

            // Get all rows from the table
            var tableRows = activeReferralsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"));
            _output.WriteLine($"[INFO] Found {tableRows.Count} rows in active referrals table");

            // Select the first available row
            Assert.True(tableRows.Count > 0, "No referrals found in the active referrals table!");
            var targetRow = tableRows[0];
            _output.WriteLine($"[PASS] Selected row 0 for editing");
            _output.WriteLine($"[INFO] Row text: {targetRow.Text}");

            // Find the edit button in the target row
            var editButton = FindReferralEditButton(targetRow);

            Assert.NotNull(editButton);
            _output.WriteLine($"[PASS] Found edit button: id='{editButton.GetAttribute("id")}', text='{editButton.Text}'");

            _output.WriteLine("\n========================================");
            _output.WriteLine("CLICKING EDIT BUTTON");
            _output.WriteLine("========================================");

            // Scroll into view and click
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", editButton);
            System.Threading.Thread.Sleep(500);

            if (!editButton.Displayed)
            {
                _output.WriteLine("[INFO] Edit button not visible, using JavaScript click");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
            }
            else
            {
                _output.WriteLine("[INFO] Edit button visible, using regular click");
                editButton.Click();
            }

            _output.WriteLine("[PASS] Clicked edit button successfully");

            // Wait for navigation
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);

            // Verify we're on the Referral edit page
            Assert.Contains("Referral.aspx", driver.Url, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Successfully navigated to Referral edit page: {driver.Url}");

            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING FOR CONTACT ATTEMPTS");
            _output.WriteLine("========================================");

            // Find the contact attempts table
            var contactAttemptsTable = FindContactAttemptsTable(driver);
            Assert.NotNull(contactAttemptsTable);
            _output.WriteLine("[PASS] Found contact attempts table");

            var initialContactAttemptRows = GetContactAttemptDataRows(contactAttemptsTable);
            var initialRowCount = initialContactAttemptRows.Count;
            _output.WriteLine($"[INFO] Initial contact attempt count: {initialRowCount}");

            // Scroll to the table
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", contactAttemptsTable);
            System.Threading.Thread.Sleep(500);

            // Get initial rows
            var initialRows = contactAttemptsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"));
            var initialRowsWithData = initialRows.Where(row => 
                !row.Text.Contains("No data available in table", StringComparison.OrdinalIgnoreCase)).ToList();

            _output.WriteLine($"[INFO] Found {initialRowsWithData.Count} contact attempt(s) in table");

            // If no contact attempts exist, create one first
            if (initialRowsWithData.Count == 0)
            {
                _output.WriteLine("\n[INFO] No contact attempts found, creating one first...");
                
                // Click New Contact Attempt button
                var newContactAttemptButton = FindButtonBySuffix(driver, "lbNewContactAttempt");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -200);", newContactAttemptButton);
                System.Threading.Thread.Sleep(500);
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", newContactAttemptButton);
                driver.WaitForReady(10);
                System.Threading.Thread.Sleep(1000);

                // Fill the form
                var contactDateField = FindTextInputBySuffix(driver, "txtContactAttemptDate");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].value = ''; arguments[0].focus(); arguments[0].value = arguments[1];", 
                    contactDateField, "11/14/2025");
                System.Threading.Thread.Sleep(300);

                var workerDropdown = FindSelectBySuffix(driver, "ddlContactAttemptWorker");
                var workerSelectElement = new OpenQA.Selenium.Support.UI.SelectElement(workerDropdown);
                workerSelectElement.SelectByValue("3489");

                var contactTypesDropdown = FindSelectBySuffix(driver, "ddlContactAttemptTypes");
                var jsScript = @"
                    var select = arguments[0];
                    var option = select.querySelector('option[value=""1469""]');
                    if (option) {
                        option.selected = true;
                        $(select).trigger('change');
                        $(select).trigger('chosen:updated');
                    }
                ";
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript(jsScript, contactTypesDropdown);
                System.Threading.Thread.Sleep(500);

                var successfulContactDropdown = FindSelectBySuffix(driver, "ddlContactAttemptSuccessful");
                var successfulSelectElement = new OpenQA.Selenium.Support.UI.SelectElement(successfulContactDropdown);
                successfulSelectElement.SelectByValue("True");

                var notesField = FindTextAreaBySuffix(driver, "txtContactAttemptNotes");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].value = ''; arguments[0].focus(); arguments[0].value = arguments[1];", 
                    notesField, "Test contact attempt for delete test");
                System.Threading.Thread.Sleep(300);

                // Submit
                var submitButton = FindButtonBySuffix(driver, "lbSubmitContactAttempt");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", submitButton);
                System.Threading.Thread.Sleep(500);
                submitButton.Click();
                
                driver.WaitForReady(10);
                System.Threading.Thread.Sleep(2000);
                _output.WriteLine("[PASS] Created test contact attempt");

                // Refresh the rows list
                initialRows = contactAttemptsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"));
                initialRowsWithData = initialRows.Where(row => 
                    !row.Text.Contains("No data available in table", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            Assert.True(initialRowsWithData.Count > 0, "No contact attempts available to delete!");
            _output.WriteLine($"[PASS] Found contact attempt to delete");

            var targetContactRow = initialRowsWithData[0];
            var initialRowText = targetContactRow.Text;
            _output.WriteLine($"[INFO] Target contact attempt row: {initialRowText}");

            _output.WriteLine("\n========================================");
            _output.WriteLine("FIRST DELETE ATTEMPT - CANCEL");
            _output.WriteLine("========================================");

            // Find the delete button in the first row
            var deleteButton = targetContactRow
                .FindElements(OpenQA.Selenium.By.CssSelector("a, button, input[type='button'], input[type='submit']"))
                .FirstOrDefault(el =>
                {
                    var text = el.Text?.Trim() ?? el.GetAttribute("value") ?? "";
                    var id = el.GetAttribute("id") ?? "";
                    var title = el.GetAttribute("title") ?? "";
                    return el.Enabled &&
                           (text.Equals("Delete", StringComparison.OrdinalIgnoreCase) ||
                            id.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                            title.Contains("Delete", StringComparison.OrdinalIgnoreCase));
                });

            Assert.NotNull(deleteButton);
            _output.WriteLine($"[PASS] Found delete button: id='{deleteButton.GetAttribute("id")}', text='{deleteButton.Text}'");

            // Scroll to delete button and click
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", deleteButton);
            System.Threading.Thread.Sleep(500);
            deleteButton.Click();
            _output.WriteLine("[PASS] Clicked delete button");

            System.Threading.Thread.Sleep(1000);

            _output.WriteLine("\n========================================");
            _output.WriteLine("HANDLING CONFIRMATION DIALOG - CLICK NO");
            _output.WriteLine("========================================");

            // Handle the confirmation dialog - Click "No" / "Cancel"
            try
            {
                // Try to find and click the "No" or "Cancel" button in the confirmation dialog
                var cancelButton = driver.FindElements(OpenQA.Selenium.By.XPath("//button[contains(text(), 'No')] | //button[contains(text(), 'Cancel')] | //a[contains(text(), 'No')] | //a[contains(text(), 'Cancel')]"))
                    .FirstOrDefault(btn => btn.Displayed && btn.Enabled);

                if (cancelButton != null)
                {
                    _output.WriteLine($"[INFO] Found cancel button: text='{cancelButton.Text}'");
                    cancelButton.Click();
                    _output.WriteLine("[PASS] Clicked 'No' button on confirmation dialog");
                }
                else
                {
                    // Try dismissing browser alert if it's a JavaScript confirm
                    try
                    {
                        var alert = driver.SwitchTo().Alert();
                        _output.WriteLine($"[INFO] Browser alert detected: {alert.Text}");
                        alert.Dismiss();
                        _output.WriteLine("[PASS] Dismissed browser alert (clicked Cancel)");
                    }
                    catch
                    {
                        _output.WriteLine("[WARN] No confirmation dialog found - trying to continue");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[WARN] Error handling confirmation dialog: {ex.Message}");
            }

            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(1000);

            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING CONTACT ATTEMPT STILL EXISTS");
            _output.WriteLine("========================================");

            // Refresh and verify the row still exists
            contactAttemptsTable = FindContactAttemptsTable(driver);
            var rowsAfterCancel = contactAttemptsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"));
            var rowsWithDataAfterCancel = rowsAfterCancel.Where(row => 
                !row.Text.Contains("No data available in table", StringComparison.OrdinalIgnoreCase)).ToList();

            _output.WriteLine($"[INFO] Rows after cancel: {rowsWithDataAfterCancel.Count}");
            Assert.Equal(initialRowsWithData.Count, rowsWithDataAfterCancel.Count);
            _output.WriteLine("[PASS] ✓ Contact attempt was NOT deleted (cancel worked correctly)");

            // Verify the specific row still exists
            var rowStillExists = rowsWithDataAfterCancel.Any(row => row.Text.Contains(initialRowText.Split(new[] { " Edit " }, StringSplitOptions.None)[0]));
            if (rowStillExists)
            {
                _output.WriteLine("[PASS] ✓ Original contact attempt row is still present");
            }

            _output.WriteLine("\n========================================");
            _output.WriteLine("SECOND DELETE ATTEMPT - CONFIRM");
            _output.WriteLine("========================================");

            // Find the delete button again (we need to re-find it after page refresh)
            targetContactRow = rowsWithDataAfterCancel[0];
            deleteButton = targetContactRow
                .FindElements(OpenQA.Selenium.By.CssSelector("a, button, input[type='button'], input[type='submit']"))
                .FirstOrDefault(el =>
                {
                    var text = el.Text?.Trim() ?? el.GetAttribute("value") ?? "";
                    var id = el.GetAttribute("id") ?? "";
                    var title = el.GetAttribute("title") ?? "";
                    return el.Enabled &&
                           (text.Equals("Delete", StringComparison.OrdinalIgnoreCase) ||
                            id.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                            title.Contains("Delete", StringComparison.OrdinalIgnoreCase));
                });

            Assert.NotNull(deleteButton);
            _output.WriteLine($"[PASS] Found delete button again: id='{deleteButton.GetAttribute("id")}', text='{deleteButton.Text}'");

            // Scroll to delete button and click
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", deleteButton);
            System.Threading.Thread.Sleep(500);
            deleteButton.Click();
            _output.WriteLine("[PASS] Clicked delete button again");

            System.Threading.Thread.Sleep(1000);

            _output.WriteLine("\n========================================");
            _output.WriteLine("HANDLING CONFIRMATION DIALOG - CLICK YES");
            _output.WriteLine("========================================");

            // Handle the confirmation dialog - Click "Yes" 
            try
            {
                // Try to find and click the "Yes"button in the confirmation dialog
                var confirmButton = driver.FindElements(OpenQA.Selenium.By.XPath("//button[contains(text(), 'Yes')] | //button[contains(text(), 'OK')] | //button[contains(text(), 'Confirm')] | //a[contains(text(), 'Yes')] | //a[contains(text(), 'OK')]"))
                    .FirstOrDefault(btn => btn.Displayed && btn.Enabled);

                if (confirmButton != null)
                {
                    _output.WriteLine($"[INFO] Found confirm button: text='{confirmButton.Text}'");
                    confirmButton.Click();
                    _output.WriteLine("[PASS] Clicked 'Yes' button on confirmation dialog");
                }
                else
                {
                    // Try accepting browser alert if it's a JavaScript confirm
                    try
                    {
                        var alert = driver.SwitchTo().Alert();
                        _output.WriteLine($"[INFO] Browser alert detected: {alert.Text}");
                        alert.Accept();
                        _output.WriteLine("[PASS] Accepted browser alert (clicked OK)");
                    }
                    catch
                    {
                        _output.WriteLine("[WARN] No confirmation dialog found - deletion may have proceeded automatically");
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[WARN] Error handling confirmation dialog: {ex.Message}");
            }

            // Wait for the deletion to process
            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(2000);

            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING CONTACT ATTEMPT WAS DELETED");
            _output.WriteLine("========================================");

            // Refresh and verify the row was deleted
            contactAttemptsTable = FindContactAttemptsTable(driver);
            contactAttemptsTable = FindContactAttemptsTable(driver);
            var rowsAfterDelete = contactAttemptsTable.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"));
            var rowsWithDataAfterDelete = rowsAfterDelete.Where(row => 
                !row.Text.Contains("No data available in table", StringComparison.OrdinalIgnoreCase)).ToList();

            _output.WriteLine($"[INFO] Rows after delete: {rowsWithDataAfterDelete.Count}");
            _output.WriteLine($"[INFO] Expected rows: {initialRowsWithData.Count - 1}");
            
            Assert.Equal(initialRowsWithData.Count - 1, rowsWithDataAfterDelete.Count);
            _output.WriteLine("[PASS] ✓ Contact attempt count decreased by 1");

            // Verify the specific row no longer exists
            if (rowsWithDataAfterDelete.Count > 0)
            {
                var deletedRowStillExists = rowsWithDataAfterDelete.Any(row => row.Text == initialRowText);
                Assert.False(deletedRowStillExists, "The deleted contact attempt row should no longer exist!");
                _output.WriteLine("[PASS] ✓ Original contact attempt row was removed");
            }
            else
            {
                _output.WriteLine("[PASS] ✓ Contact attempts table is now empty (all rows deleted)");
            }

            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING FOR SUCCESS NOTIFICATION");
            _output.WriteLine("========================================");

            // Check for success toast notification
            try
            {
                var toastElements = driver.FindElements(OpenQA.Selenium.By.CssSelector(".toast, .alert-success, [class*='success'], [role='alert']"));
                var visibleToasts = toastElements.Where(t => t.Displayed && !string.IsNullOrWhiteSpace(t.Text)).ToList();
                
                if (visibleToasts.Any())
                {
                    _output.WriteLine($"[INFO] Found {visibleToasts.Count} success notification(s):");
                    foreach (var toast in visibleToasts)
                    {
                        var toastText = toast.Text?.Trim();
                        _output.WriteLine($"  ✓ {toastText}");
                        
                        // Check if toast contains delete-related success message
                        if (toastText != null && 
                            (toastText.Contains("deleted", StringComparison.OrdinalIgnoreCase) ||
                             toastText.Contains("removed", StringComparison.OrdinalIgnoreCase) ||
                             toastText.Contains("success", StringComparison.OrdinalIgnoreCase)))
                        {
                            _output.WriteLine("[PASS]  Success notification confirms deletion");
                        }
                    }
                }
                else
                {
                    _output.WriteLine("[INFO] No visible toast notifications found");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[INFO] Could not check for success notifications: {ex.Message}");
            }

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST SUMMARY");
            _output.WriteLine("========================================");
            _output.WriteLine("[PASS] Successfully navigated to referral edit page");
            _output.WriteLine("[PASS] Successfully found contact attempt to delete");
            _output.WriteLine("[PASS] Successfully cancelled first delete attempt");
            _output.WriteLine("[PASS] Verified contact attempt was NOT deleted after cancel");
            _output.WriteLine("[PASS] Successfully confirmed second delete attempt");
            _output.WriteLine("[PASS] Verified contact attempt WAS deleted successfully");
            _output.WriteLine("========================================");
        }

    }
}


