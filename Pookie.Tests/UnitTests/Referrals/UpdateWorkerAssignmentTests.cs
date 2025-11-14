using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.Referrals
{
    public class UpdateWorkerAssignmentTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public UpdateWorkerAssignmentTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        #region Helper Methods

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
            var referralsLink = driver.FindElements(By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);

            Assert.NotNull(referralsLink);
            _output.WriteLine($"Found Referrals link with text: '{referralsLink.Text?.Trim()}'");
            referralsLink.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            
            _output.WriteLine("[PASS] Successfully navigated to Referrals page");
            _output.WriteLine($"Current URL: {driver.Url}");
        }

        private IWebElement FindActiveReferralsTable(IPookieWebDriver driver)
        {
            var grid = driver.FindElements(By.CssSelector("[id*='grActiveReferrals']"))
                .FirstOrDefault(el => el.Displayed);
            
            if (grid != null)
            {
                var table = grid.FindElements(By.CssSelector("table"))
                    .FirstOrDefault(t => t.Displayed);
                
                if (table != null)
                {
                    return table;
                }
            }

            throw new InvalidOperationException("Unable to locate the Active Referrals table.");
        }

        private IWebElement FindReferralEditButton(IWebElement tableRow)
        {
            return tableRow.FindElements(By.CssSelector("a.btn.btn-default"))
                .FirstOrDefault(el => el.Displayed && el.Enabled)
                ?? throw new InvalidOperationException("Unable to locate the edit button within the referral row.");
        }

        private void OpenReferralEditForm(IPookieWebDriver driver)
        {
            var activeReferralsTable = FindActiveReferralsTable(driver);
            Assert.NotNull(activeReferralsTable);
            _output.WriteLine("[PASS] Found active referrals table");

            var tableRows = activeReferralsTable.FindElements(By.CssSelector("tbody tr"));
            _output.WriteLine($"[INFO] Found {tableRows.Count} rows in active referrals table");

            Assert.True(tableRows.Count > 0, "No referrals found in the active referrals table!");
            var targetRow = tableRows[0];
            _output.WriteLine($"[PASS] Selected row 0 for editing");

            var editButton = FindReferralEditButton(targetRow);
            Assert.NotNull(editButton);
            _output.WriteLine($"[PASS] Found edit button: id='{editButton.GetAttribute("id")}'");

            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", editButton);
            System.Threading.Thread.Sleep(500);

            if (!editButton.Displayed)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
            }
            else
            {
                editButton.Click();
            }

            _output.WriteLine("[PASS] Clicked edit button successfully");
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
        }

        private void OpenWorkerAssignmentEditForm(IPookieWebDriver driver)
        {
            _output.WriteLine("Scrolling to bottom of page to find worker assignment edit button...");
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            System.Threading.Thread.Sleep(1000);

            var workerAssignmentEditButton = driver.FindElements(By.CssSelector("a[id*='lbEditWorkerAssignment']"))
                .FirstOrDefault(b => b.Displayed);
            
            Assert.True(workerAssignmentEditButton != null, "Worker assignment edit button was not found.");
            Assert.NotNull(workerAssignmentEditButton);
            _output.WriteLine($"[PASS] Found worker assignment edit button: id='{workerAssignmentEditButton.GetAttribute("id")}'");

            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", workerAssignmentEditButton);
            System.Threading.Thread.Sleep(500);
            
            if (!workerAssignmentEditButton.Displayed)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", workerAssignmentEditButton);
            }
            else
            {
                workerAssignmentEditButton.Click();
            }

            _output.WriteLine("[PASS] Clicked worker assignment edit button successfully");
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
        }

        private void SetDateAndWaitForPostback(IPookieWebDriver driver, string targetDate)
        {
            var dateField = driver.FindElements(By.CssSelector("input[id*='txtWorkerAssignmentDate']"))
                .FirstOrDefault(f => f.Displayed);

            Assert.True(dateField != null, "Worker assignment date field was not found.");
            Assert.NotNull(dateField);
            _output.WriteLine($"[PASS] Found date field: id='{dateField.GetAttribute("id")}'");

            var currentDateValue = dateField.GetAttribute("value");
            _output.WriteLine($"[INFO] Current date field value: '{currentDateValue}'");
            _output.WriteLine($"[INFO] Changing date to: {targetDate}");

            // Clear and type the new date
            dateField.Clear();
            System.Threading.Thread.Sleep(200);
            dateField.SendKeys(targetDate);
            System.Threading.Thread.Sleep(300);
            
            // IMPORTANT: Tab out to trigger the onchange event (which triggers __doPostBack)
            _output.WriteLine("[INFO] Triggering onchange event (this will cause a postback)...");
            dateField.SendKeys(OpenQA.Selenium.Keys.Tab);
            System.Threading.Thread.Sleep(500);
            
            // Wait for the postback to complete - the page will refresh/update
            _output.WriteLine("[INFO] Waiting for postback to complete...");
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(3000); // Give extra time for the postback
            _output.WriteLine($"[PASS] Date changed to: {targetDate}, postback completed");
        }

        private void SelectWorker(IPookieWebDriver driver)
        {
            // IMPORTANT: Re-find the worker dropdown AFTER the postback
            // The postback may have refreshed the dropdown, making the old reference stale
            _output.WriteLine("[INFO] Re-finding worker dropdown after postback...");
            var workerDropdown = driver.FindElements(By.CssSelector("select[id*='ddlWorkerAssignmentWorker']"))
                .FirstOrDefault(d => d.Displayed);
            
            Assert.True(workerDropdown != null, "Worker assignment dropdown was not found after date change postback.");
            Assert.NotNull(workerDropdown);
            _output.WriteLine($"[PASS] Re-found worker dropdown: id='{workerDropdown.GetAttribute("id")}'");

            var workerSelect = new SelectElement(workerDropdown);
            var currentWorkerText = workerSelect.SelectedOption?.Text ?? string.Empty;
            _output.WriteLine($"[INFO] Currently selected worker: '{currentWorkerText}'");

            var preferredWorkers = new[]
            {
                "2431, Worker",
                "3477, Worker",
                "79, Worker",
                "Test, Derek"
            };

            var workerChanged = false;
            foreach (var workerName in preferredWorkers)
            {
                if (string.Equals(workerName, currentWorkerText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    workerSelect.SelectByText(workerName);
                    _output.WriteLine($"[PASS] Selected worker: {workerName}");
                    workerChanged = true;
                    break;
                }
                catch (NoSuchElementException)
                {
                    // Ignore and try next worker
                }
            }

            if (!workerChanged)
            {
                // Fallback: use JavaScript to get the first non-default option value
                var fallbackValue = (string?)((IJavaScriptExecutor)driver).ExecuteScript(@"
                    var select = arguments[0];
                    if (!select) return null;
                    for (var i = 0; i < select.options.length; i++) {
                        var opt = select.options[i];
                        if (opt && opt.value && opt.value !== '0' && !opt.selected) {
                            return opt.value;
                        }
                    }
                    return null;", workerDropdown);

                Assert.False(string.IsNullOrWhiteSpace(fallbackValue), "No alternative workers available to select!");
                workerSelect.SelectByValue(fallbackValue);
                _output.WriteLine($"[PASS] Selected worker by fallback value: {fallbackValue}");
            }

            System.Threading.Thread.Sleep(500);
        }

        private void SubmitWorkerAssignment(IPookieWebDriver driver)
        {
            var submitButton = driver.FindElements(By.CssSelector("a.btn.btn-primary[id*='lbSubmitWorkerAssignment']"))
                .FirstOrDefault(b => b.Displayed && b.Enabled);
            
            Assert.True(submitButton != null, "Submit button was not found. Make sure the form is ready.");
            Assert.NotNull(submitButton);
            _output.WriteLine($"[PASS] Found submit button: id='{submitButton.GetAttribute("id")}'");

            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", submitButton);
            System.Threading.Thread.Sleep(500);
            
            submitButton.Click();
            _output.WriteLine("[PASS] Clicked submit button");

            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
        }

        private string FindToastMessage(IPookieWebDriver driver)
        {
            var toastSelectors = new[]
            {
                By.CssSelector(".toast"),
                By.CssSelector(".toast-message"),
                By.CssSelector("[class*='toast']"),
                By.CssSelector("[id*='toast']"),
                By.CssSelector(".alert-success"),
                By.CssSelector(".alert-danger"),
                By.CssSelector("[class*='alert']"),
                By.CssSelector("[role='alert']"),
                By.CssSelector("[class*='Toastify']")
            };

            foreach (var selector in toastSelectors)
            {
                try
                {
                    var toastElements = driver.FindElements(selector);
                    var visibleToast = toastElements.FirstOrDefault(t => t.Displayed && !string.IsNullOrWhiteSpace(t.Text));

                    if (visibleToast != null)
                    {
                        var message = visibleToast.Text?.Trim() ?? "";
                        _output.WriteLine($"[PASS] Toast notification found: '{message}'");
                        return message;
                    }
                }
                catch
                {
                    // Continue trying other selectors
                }
            }

            // Wait and try again
            System.Threading.Thread.Sleep(2000);
            foreach (var selector in toastSelectors)
            {
                try
                {
                    var toastElements = driver.FindElements(selector);
                    var visibleToast = toastElements.FirstOrDefault(t => t.Displayed && !string.IsNullOrWhiteSpace(t.Text));

                    if (visibleToast != null)
                    {
                        var message = visibleToast.Text?.Trim() ?? "";
                        _output.WriteLine($"[PASS] Toast notification found after wait: '{message}'");
                        return message;
                    }
                }
                catch
                {
                    // Continue trying other selectors
                }
            }

            return "";
        }

        private void VerifyValidationError(IPookieWebDriver driver, string expectedError)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            System.Threading.Thread.Sleep(1000);
            
            var validationSelectors = new[]
            {
                By.CssSelector(".validation-summary-errors"),
                By.CssSelector("[class*='validation-summary']"),
                By.CssSelector(".field-validation-error"),
                By.CssSelector(".text-danger"),
                By.CssSelector("[style*='color:Red']")
            };

            var foundValidationError = false;
            foreach (var selector in validationSelectors)
            {
                try
                {
                    var validationElements = driver.FindElements(selector);
                    var visibleValidations = validationElements
                        .Where(v => v.Displayed && !string.IsNullOrWhiteSpace(v.Text))
                        .ToList();

                    foreach (var validation in visibleValidations)
                    {
                        var validationText = validation.Text?.Trim() ?? "";
                        _output.WriteLine($"  - {validationText}");
                        
                        if (validationText.Contains(expectedError, StringComparison.OrdinalIgnoreCase))
                        {
                            foundValidationError = true;
                            _output.WriteLine($"[PASS] ✓ Found expected validation error: '{validationText}'");
                        }
                    }
                }
                catch
                {
                    // Continue trying other selectors
                }
            }

            Assert.True(foundValidationError, 
                $"Expected validation error '{expectedError}' was not found in the validation summary.");
        }

        #endregion

        [Fact]
        public void ReferralsPage_UpdateWorkerAssignment_EditsWorkerAndDate()
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: UPDATE WORKER ASSIGNMENT");
            _output.WriteLine("========================================");

            LoginAndNavigateToReferrals(driver);
            OpenReferralEditForm(driver);
            OpenWorkerAssignmentEditForm(driver);

            _output.WriteLine("\n========================================");
            _output.WriteLine("UPDATING WORKER ASSIGNMENT");
            _output.WriteLine("========================================");

            // Find the worker assignment dropdown first
            var workerDropdown = driver.FindElements(By.CssSelector("select[id*='ddlWorkerAssignmentWorker']"))
                .FirstOrDefault(d => d.Displayed);
            Assert.True(workerDropdown != null, "Worker assignment dropdown was not found.");
            Assert.NotNull(workerDropdown);
            _output.WriteLine($"[PASS] Found worker dropdown: id='{workerDropdown.GetAttribute("id")}'");

            // Step 1: Change the date (this will trigger a postback!)
            _output.WriteLine("\nStep 1: Changing the date...");
            var dateField = driver.FindElements(By.CssSelector("input[id*='txtWorkerAssignmentDate']"))
                .FirstOrDefault(f => f.Displayed);
            var currentDateValue = dateField?.GetAttribute("value") ?? "";
            
            DateTime parsedDate;
            if (DateTime.TryParse(currentDateValue, out parsedDate))
            {
                parsedDate = parsedDate.AddDays(1);
            }
            else
            {
                parsedDate = DateTime.Now.AddDays(1);
            }
            
            var targetDate = parsedDate.ToString("MM/dd/yyyy");
            SetDateAndWaitForPostback(driver, targetDate);

            // Step 2: Change the worker (AFTER the postback)
            _output.WriteLine("\nStep 2: Changing the worker...");
            SelectWorker(driver);

            // Step 3: Submit
            _output.WriteLine("\nStep 3: Submitting...");
            SubmitWorkerAssignment(driver);

            // Step 4: Verify success toast
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING SUCCESS TOAST");
            _output.WriteLine("========================================");

            var toastMessage = FindToastMessage(driver);
            Assert.True(!string.IsNullOrEmpty(toastMessage), "Toast notification was not found after submitting worker assignment update.");
            Assert.True(
                toastMessage.Contains("Worker Assignment Edited", StringComparison.OrdinalIgnoreCase) ||
                toastMessage.Contains("Worker assignment successfully edited", StringComparison.OrdinalIgnoreCase),
                $"Expected toast message about 'Worker Assignment Edited', but got: '{toastMessage}'");

            _output.WriteLine($"[PASS] ✓ Toast message verified: '{toastMessage}'");
        }

        [Fact]
        public void ReferralsPage_UpdateWorkerAssignment_InvalidDate_ShowsValidationError()
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: UPDATE WORKER ASSIGNMENT - INVALID DATE");
            _output.WriteLine("========================================");

            LoginAndNavigateToReferrals(driver);
            OpenReferralEditForm(driver);
            OpenWorkerAssignmentEditForm(driver);

            _output.WriteLine("\n========================================");
            _output.WriteLine("SETTING INVALID DATE (BEFORE REFERRAL DATE)");
            _output.WriteLine("========================================");

            // Find the worker assignment dropdown first
            var workerDropdown = driver.FindElements(By.CssSelector("select[id*='ddlWorkerAssignmentWorker']"))
                .FirstOrDefault(d => d.Displayed);
            Assert.True(workerDropdown != null, "Worker assignment dropdown was not found.");
            Assert.NotNull(workerDropdown);
            _output.WriteLine($"[PASS] Found worker dropdown: id='{workerDropdown.GetAttribute("id")}'");

            // Set date to 11/7/25 (this should be before the referral date)
            var invalidDate = "11/07/2025";
            _output.WriteLine($"[INFO] Setting date to invalid date: {invalidDate} (before referral date)");
            SetDateAndWaitForPostback(driver, invalidDate);

            // Change the worker (after the postback)
            _output.WriteLine("\nChanging the worker...");
            SelectWorker(driver);

            // Submit
            _output.WriteLine("\nSubmitting...");
            SubmitWorkerAssignment(driver);

            // Verify validation error
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING VALIDATION ERROR");
            _output.WriteLine("========================================");

            var toastMessage = FindToastMessage(driver);
            Assert.True(!string.IsNullOrEmpty(toastMessage), "Toast notification was not found.");
            Assert.True(
                toastMessage.Contains("Validation Failed", StringComparison.OrdinalIgnoreCase),
                $"Expected validation failed toast message, but got: '{toastMessage}'");

            VerifyValidationError(driver, "Contact Date cannot be before the Referral Date");
        }
    }
}
