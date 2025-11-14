using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.Referrals
{
    public class UploadDocumentsTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public UploadDocumentsTests(AppConfig config, ITestOutputHelper output)
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

        /// <summary>
        /// Finds the active referrals table
        /// </summary>
        private IWebElement FindActiveReferralsTable(IPookieWebDriver driver)
        {
            var cssSelectors = new[]
            {
                ".table.table-condensed.table-responsive.dataTable.no-footer.dtr-column",
                ".dataTables_wrapper table.dataTable",
                ".referrals table",
                ".referrals-list table",
                ".active-referrals table",
                ".referrals-grid table",
                "table.table"
            };

            foreach (var selector in cssSelectors)
            {
                try
                {
                    var candidates = driver.FindElements(By.CssSelector(selector));
                    var match = candidates.FirstOrDefault(el => el.Displayed && LooksLikeActiveReferrals(el));
                    if (match != null)
                    {
                        _output.WriteLine($"[INFO] Found Active Referrals table using CSS selector '{selector}'");
                        return match;
                    }
                }
                catch (InvalidSelectorException invalidSelector)
                {
                    _output.WriteLine($"[WARN] Invalid selector '{selector}': {invalidSelector.Message}");
                }
                catch (WebDriverException driverException)
                {
                    _output.WriteLine($"[WARN] Unable to evaluate selector '{selector}': {driverException.Message}");
                }
            }

            throw new InvalidOperationException("Unable to locate the Active Referrals table using CSS selectors.");
        }

        /// <summary>
        /// Checks if a table looks like the active referrals table
        /// </summary>
        private bool LooksLikeActiveReferrals(IWebElement table)
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

        /// <summary>
        /// Checks if element text contains a value (case-insensitive)
        /// </summary>
        private static bool ElementTextContains(IWebElement element, string expectedValue)
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

        /// <summary>
        /// Finds the edit button within a referral table row
        /// </summary>
        private IWebElement FindReferralEditButton(IWebElement tableRow)
        {
            if (tableRow == null)
            {
                throw new ArgumentNullException(nameof(tableRow));
            }

            var primarySelectors = new[]
            {
                "a.btn.btn-default",
                "button.btn.btn-default",
                "a.btn",
                "button.btn",
                "a[id*='lnkEditReferral']",
                "a[id*='Edit']"
            };

            foreach (var selector in primarySelectors)
            {
                var match = tableRow.FindElements(By.CssSelector(selector))
                    .FirstOrDefault(el => el.Displayed &&
                                          el.Enabled &&
                                          (ElementTextContains(el, "Edit") ||
                                           ElementHasIcon(el, "glyphicon-pencil")));
                if (match != null)
                {
                    _output.WriteLine($"[INFO] Found edit button via selector '{selector}'");
                    return match;
                }
            }

            var fallback = tableRow
                .FindElements(By.CssSelector("a, button, input[type='button'], input[type='submit'], input[type='image']"))
                .FirstOrDefault(el =>
                {
                    var text = el.Text?.Trim() ?? el.GetAttribute("value") ?? "";
                    var id = el.GetAttribute("id") ?? "";
                    var title = el.GetAttribute("title") ?? "";
                    return el.Enabled &&
                           (text.Equals("Edit", StringComparison.OrdinalIgnoreCase) ||
                            id.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
                            title.Contains("Edit", StringComparison.OrdinalIgnoreCase));
                });

            if (fallback != null)
            {
                _output.WriteLine("[INFO] Found edit button via fallback element search");
                return fallback;
            }

            throw new InvalidOperationException("Unable to locate the edit button within the referral row.");
        }

        /// <summary>
        /// Checks if element has a specific icon class
        /// </summary>
        private static bool ElementHasIcon(IWebElement element, string iconClass)
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
            return element.FindElements(By.CssSelector(iconSelector)).Any(icon => icon.Displayed);
        }

        /// <summary>
        /// Clicks Edit on the first referral in the active referrals table
        /// </summary>
        private void ClickEditOnFirstReferral(IPookieWebDriver driver)
        {
            _output.WriteLine("\nLooking for Active Referrals table...");
            var activeReferralsTable = FindActiveReferralsTable(driver);
            Assert.NotNull(activeReferralsTable);
            _output.WriteLine("[PASS] Found Active Referrals table");

            _output.WriteLine("Finding first row in table...");
            var firstRow = activeReferralsTable
                .FindElements(By.CssSelector("tbody tr"))
                .FirstOrDefault(row => row.Displayed);
            Assert.NotNull(firstRow);
            _output.WriteLine("[PASS] Found first row");

            _output.WriteLine("Finding edit button in first row...");
            var editButton = FindReferralEditButton(firstRow);
            Assert.NotNull(editButton);
            _output.WriteLine($"[PASS] Found edit button: id='{editButton.GetAttribute("id")}'");

            _output.WriteLine("Clicking edit button...");
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", editButton);
            System.Threading.Thread.Sleep(500);

            if (!editButton.Displayed)
            {
                driver.ExecuteScript("arguments[0].click();", editButton);
            }
            else
            {
                editButton.Click();
            }

            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked edit button");
            _output.WriteLine($"Current URL: {driver.Url}");
        }

        /// <summary>
        /// Finds a button/link by suffix (CSS selectors only)
        /// </summary>
        private IWebElement FindButtonBySuffix(IPookieWebDriver driver, params string[] suffixes)
        {
            var selectors = new[]
            {
                "button.btn",
                "button",
                "input.btn",
                "input[type='button']",
                "input[type='submit']",
                "a.btn"
            };

            var attributes = new[] { "name", "id" };

            foreach (var baseSelector in selectors)
            {
                foreach (var suffix in suffixes)
                {
                    foreach (var attribute in attributes)
                    {
                        var endsWithSelector = $"{baseSelector}[{attribute}$='{suffix}']";
                        var match = driver.FindElements(By.CssSelector(endsWithSelector))
                            .FirstOrDefault(el => el.Displayed);
                        if (match != null)
                        {
                            _output.WriteLine($"[INFO] Found button via selector '{endsWithSelector}'");
                            return match;
                        }

                        var containsSelector = $"{baseSelector}[{attribute}*='{suffix}']";
                        match = driver.FindElements(By.CssSelector(containsSelector))
                            .FirstOrDefault(el => el.Displayed);
                        if (match != null)
                        {
                            _output.WriteLine($"[INFO] Found button via selector '{containsSelector}'");
                            return match;
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Unable to locate button using suffixes: {string.Join(", ", suffixes)}");
        }

        /// <summary>
        /// Clicks the "Upload New Document" button on the referral edit page
        /// </summary>
        private void ClickUploadNewDocumentButton(IPookieWebDriver driver)
        {
            _output.WriteLine("\nLooking for 'Upload New Document' button...");
            
            var uploadButton = FindButtonBySuffix(driver, "lbNewUploadedFile", "NewUploadedFile");
            Assert.NotNull(uploadButton);
            _output.WriteLine($"[PASS] Found Upload New Document button: id='{uploadButton.GetAttribute("id")}', text='{uploadButton.Text?.Trim()}'");

            _output.WriteLine("Clicking Upload New Document button...");
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", uploadButton);
            System.Threading.Thread.Sleep(500);

            if (!uploadButton.Displayed)
            {
                driver.ExecuteScript("arguments[0].click();", uploadButton);
            }
            else
            {
                uploadButton.Click();
            }

            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked Upload New Document button");
        }

        /// <summary>
        /// Finds a file input element for document upload
        /// </summary>
        private IWebElement FindFileUploadInput(IPookieWebDriver driver, string suffix = null)
        {
            var selectors = new[]
            {
                suffix != null ? $"input[type='file'][id$='{suffix}']" : "input[type='file']",
                suffix != null ? $"input[type='file'][name$='{suffix}']" : "input[type='file']",
                "input[type='file']"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var elements = driver.FindElements(By.CssSelector(selector));
                    var match = elements.FirstOrDefault(el => el.Displayed);
                    if (match != null)
                    {
                        _output.WriteLine($"[INFO] Found file upload input using selector '{selector}'");
                        return match;
                    }
                }
                catch (InvalidSelectorException ex)
                {
                    _output.WriteLine($"[WARN] Invalid selector '{selector}': {ex.Message}");
                }
            }

            throw new InvalidOperationException("Unable to locate file upload input element.");
        }

        /// <summary>
        /// Creates a temporary test file for upload
        /// </summary>
        private string CreateTestFile(string fileName = "test_document.txt", string content = "This is a test document for upload.")
        {
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(tempPath, content);
            _output.WriteLine($"[INFO] Created test file: {tempPath}");
            return tempPath;
        }

        /// <summary>
        /// Finds the file upload input field (CSS selectors only)
        /// </summary>
        private IWebElement FindFileUploadInputField(IPookieWebDriver driver)
        {
            var selectors = new[]
            {
                "input[type='file'][id*='fuUploadedFile']",
                "input[type='file'][name*='fuUploadedFile']",
                "input[type='file'][id*='UploadedFile']",
                "input[type='file']"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var elements = driver.FindElements(By.CssSelector(selector));
                    var match = elements.FirstOrDefault(el => el.Displayed);
                    if (match != null)
                    {
                        _output.WriteLine($"[INFO] Found file upload input using selector '{selector}'");
                        return match;
                    }
                }
                catch (InvalidSelectorException ex)
                {
                    _output.WriteLine($"[WARN] Invalid selector '{selector}': {ex.Message}");
                }
            }

            throw new InvalidOperationException("Unable to locate file upload input field.");
        }

        /// <summary>
        /// Finds the Upload submit button for documents (CSS selectors only)
        /// </summary>
        private IWebElement FindUploadSubmitButton(IPookieWebDriver driver)
        {
            var selectors = new[]
            {
                "a[id*='lbSubmitUploadedFile']",
                "a[id*='SubmitUploadedFile']",
                "a[class*='btn-primary'][class*='custom-submit-button']",
                "a.btn-primary",
                "button[type='submit']",
                "input[type='submit']",
                "a[class*='upload']",
                "button[class*='upload']"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var elements = driver.FindElements(By.CssSelector(selector));
                    var match = elements.FirstOrDefault(el => 
                    {
                        if (!el.Displayed || !el.Enabled) return false;
                        
                        // Check for upload icon inside the element
                        try
                        {
                            var icon = el.FindElements(By.CssSelector(".glyphicon-upload"));
                            if (icon.Any(i => i.Displayed))
                            {
                                return true;
                            }
                        }
                        catch { }
                        
                        var text = el.Text?.Trim() ?? "";
                        var id = el.GetAttribute("id")?.Trim() ?? "";
                        return text.IndexOf("Upload", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               id.IndexOf("SubmitUploadedFile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               id.IndexOf("lbSubmitUploadedFile", StringComparison.OrdinalIgnoreCase) >= 0;
                    });
                    if (match != null)
                    {
                        _output.WriteLine($"[INFO] Found upload submit button using selector '{selector}'");
                        return match;
                    }
                }
                catch (InvalidSelectorException ex)
                {
                    _output.WriteLine($"[WARN] Invalid selector '{selector}': {ex.Message}");
                }
            }

            throw new InvalidOperationException("Unable to locate upload submit button.");
        }

        /// <summary>
        /// Checks for validation error messages (CSS selectors only)
        /// </summary>
        private List<string> GetValidationErrorMessages(IPookieWebDriver driver)
        {
            var errorSelectors = new[]
            {
                By.CssSelector(".alert-danger"),
                By.CssSelector(".alert-warning"),
                By.CssSelector("[class*='error']"),
                By.CssSelector("[class*='validation']"),
                By.CssSelector(".field-validation-error"),
                By.CssSelector(".text-danger"),
                By.CssSelector("span[style*='color: red']"),
                By.CssSelector("span[style*='color:red']"),
                By.CssSelector("[id*='rfv']"),
                By.CssSelector("[id*='rev']"),
                By.CssSelector("[style*='color:Red']"),
                By.CssSelector("[style*='color: Red']")
            };

            var errorMessages = new List<string>();
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
                            if (!string.IsNullOrWhiteSpace(text) && !errorMessages.Contains(text))
                            {
                                errorMessages.Add(text);
                            }
                        }
                    }
                }
                catch { }
            }

            return errorMessages;
        }

        /// <summary>
        /// Verifies what appears on the page after document upload (CSS selectors only)
        /// </summary>
        private void VerifyPostUploadPageContent(IPookieWebDriver driver, string fileName)
        {
            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(3000);

            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING PAGE CONTENT AFTER UPLOAD");
            _output.WriteLine("========================================");

            // Check for success messages/toasts (CSS selectors only)
            _output.WriteLine("\n--- Checking for Success Messages/Toasts ---");
            var toastSelectors = new[]
            {
                By.CssSelector(".toast"),
                By.CssSelector(".toast-message"),
                By.CssSelector("[class*='toast']"),
                By.CssSelector(".alert-success"),
                By.CssSelector(".alert"),
                By.CssSelector("[class*='success']"),
                By.CssSelector("[role='alert']"),
                By.CssSelector("[class*='notification']"),
                By.CssSelector("[id*='toast']"),
                By.CssSelector("[class*='Toastify']")
            };

            var foundMessages = new List<string>();
            foreach (var selector in toastSelectors)
            {
                try
                {
                    var elements = driver.FindElements(selector);
                    var visibleElements = elements.Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text?.Trim())).ToList();
                    
                    foreach (var element in visibleElements)
                    {
                        var text = element.Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(text) && !foundMessages.Contains(text))
                        {
                            foundMessages.Add(text);
                            _output.WriteLine($"  [FOUND] Message: '{text}'");
                            _output.WriteLine($"    Selector: {selector}");
                            _output.WriteLine($"    Element classes: '{element.GetAttribute("class")}'");
                        }
                    }
                }
                catch { }
            }

            if (foundMessages.Count == 0)
            {
                _output.WriteLine("  [INFO] No success messages/toasts found");
            }

            // Check for document in tables/lists (CSS selectors only)
            _output.WriteLine("\n--- Checking for Document in Tables/Lists ---");
            var tableSelectors = new[]
            {
                By.CssSelector("table"),
                By.CssSelector("table.dataTable"),
                By.CssSelector("table.table"),
                By.CssSelector("[class*='table']"),
                By.CssSelector("[id*='Document']"),
                By.CssSelector("[id*='Upload']")
            };

            var documentFound = false;
            foreach (var selector in tableSelectors)
            {
                try
                {
                    var tables = driver.FindElements(selector);
                    foreach (var table in tables)
                    {
                        if (!table.Displayed) continue;

                        var rows = table.FindElements(By.CssSelector("tbody tr, tr"));
                        foreach (var row in rows)
                        {
                            try
                            {
                                if (!row.Displayed) continue;
                                
                                var rowText = row.Text?.Trim() ?? "";
                                if (rowText.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    rowText.IndexOf("TestHFNY", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    rowText.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    documentFound = true;
                                    _output.WriteLine($"  [FOUND] Document in table row:");
                                    _output.WriteLine($"    Row text: '{rowText}'");
                                    _output.WriteLine($"    Table selector: {selector}");
                                    
                                    // Log table cells if available
                                    var cells = row.FindElements(By.CssSelector("td, th"));
                                    if (cells.Any())
                                    {
                                        _output.WriteLine($"    Cells ({cells.Count}):");
                                        foreach (var cell in cells.Take(5))
                                        {
                                            var cellText = cell.Text?.Trim() ?? "";
                                            if (!string.IsNullOrWhiteSpace(cellText))
                                            {
                                                _output.WriteLine($"      - '{cellText}'");
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                            catch { }
                        }
                        if (documentFound) break;
                    }
                    if (documentFound) break;
                }
                catch { }
            }

            if (!documentFound)
            {
                _output.WriteLine("  [INFO] Document not found in any table/list");
            }

            // Check for any panels/sections related to documents (CSS selectors only)
            _output.WriteLine("\n--- Checking for Document Sections/Panels ---");
            var panelSelectors = new[]
            {
                By.CssSelector("[class*='panel']"),
                By.CssSelector("[id*='Document']"),
                By.CssSelector("[id*='Upload']"),
                By.CssSelector(".panel-title"),
                By.CssSelector("[class*='document']")
            };

            foreach (var selector in panelSelectors)
            {
                try
                {
                    var elements = driver.FindElements(selector);
                    var visibleElements = elements.Where(el => el.Displayed).Take(5).ToList();
                    
                    foreach (var element in visibleElements)
                    {
                        var text = element.Text?.Trim();
                        var id = element.GetAttribute("id") ?? "";
                        var className = element.GetAttribute("class") ?? "";
                        
                        if (!string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(id))
                        {
                            _output.WriteLine($"  [FOUND] Panel/Section:");
                            _output.WriteLine($"    Text: '{text}'");
                            _output.WriteLine($"    ID: '{id}'");
                            _output.WriteLine($"    Class: '{className}'");
                        }
                    }
                }
                catch { }
            }

            // Summary
            _output.WriteLine("\n--- Summary ---");
            _output.WriteLine($"Success messages found: {foundMessages.Count}");
            _output.WriteLine($"Document found in table: {documentFound}");
            
            if (foundMessages.Count > 0)
            {
                _output.WriteLine("\nSuccess Messages:");
                foreach (var msg in foundMessages)
                {
                    _output.WriteLine($"  - {msg}");
                }
            }
        }

        #endregion

        #region Tests

        [Fact]
        public void UploadDocument_WithPDFFile_HandlesValidationAndUploads()
        {
            using var driver = _driverFactory.CreateDriver();

            // Navigate to Referrals page
            LoginAndNavigateToReferrals(driver);

            // Click Edit on the first referral
            ClickEditOnFirstReferral(driver);

            // Click Upload New Document button
            ClickUploadNewDocumentButton(driver);

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: Upload PDF Document with Validation");
            _output.WriteLine("========================================");

            // Get the path to the test PDF file
            var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "TestHFNY.pdf");
            if (!File.Exists(testFilePath))
            {
                // Try alternative path (if running from bin folder)
                testFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestFiles", "TestHFNY.pdf");
                testFilePath = Path.GetFullPath(testFilePath);
            }

            Assert.True(File.Exists(testFilePath), $"Test file not found at: {testFilePath}");
            _output.WriteLine($"Using test file: {testFilePath}");

            // Find file upload input field
            _output.WriteLine("\nLooking for file upload input field...");
            var fileInput = FindFileUploadInputField(driver);
            _output.WriteLine($"[PASS] Found file upload input: id='{fileInput.GetAttribute("id")}', name='{fileInput.GetAttribute("name")}'");
            
            // Select the file (this will trigger file dialog and select the file)
            _output.WriteLine($"\nSelecting file: TestHFNY.pdf");
            fileInput.SendKeys(testFilePath);
            _output.WriteLine("[PASS] File selected");
            
            // Wait for file name to appear in the input field
            System.Threading.Thread.Sleep(1000);
            var fileNameInInput = fileInput.GetAttribute("value");
            if (!string.IsNullOrWhiteSpace(fileNameInInput))
            {
                _output.WriteLine($"[PASS] File name appears in input field: {Path.GetFileName(fileNameInInput)}");
            }
            else
            {
                _output.WriteLine("[INFO] File name not visible in input field (this is normal for file inputs)");
            }

            // Find and click the Upload submit button
            _output.WriteLine("\nLooking for Upload submit button...");
            var uploadSubmitButton = FindUploadSubmitButton(driver);
            _output.WriteLine($"[PASS] Found Upload submit button: id='{uploadSubmitButton.GetAttribute("id")}', text='{uploadSubmitButton.Text?.Trim()}'");
            
            _output.WriteLine("Clicking Upload submit button...");
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", uploadSubmitButton);
            System.Threading.Thread.Sleep(500);
            uploadSubmitButton.Click();
            
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(3000);
            _output.WriteLine("[PASS] Clicked Upload submit button");

            // Verify success toast message appears
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING SUCCESS TOAST MESSAGE");
            _output.WriteLine("========================================");
            
            var toastSelectors = new[]
            {
                By.CssSelector(".toast"),
                By.CssSelector(".toast-message"),
                By.CssSelector("[class*='toast']"),
                By.CssSelector(".alert-success"),
                By.CssSelector(".alert"),
                By.CssSelector("[class*='success']"),
                By.CssSelector("[role='alert']"),
                By.CssSelector("[class*='notification']"),
                By.CssSelector("[id*='toast']"),
                By.CssSelector("[class*='Toastify']")
            };

            var successToastFound = false;
            var toastText = "";
            
            foreach (var selector in toastSelectors)
            {
                try
                {
                    var elements = driver.FindElements(selector);
                    var visibleElements = elements.Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text?.Trim())).ToList();
                    
                    foreach (var element in visibleElements)
                    {
                        var text = element.Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // Check if toast contains the expected success messages
                            if (text.IndexOf("Document Uploaded", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                text.IndexOf("Successfully uploaded the document", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                text.IndexOf("Successfully uploaded", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                successToastFound = true;
                                toastText = text;
                                _output.WriteLine($"[SUCCESS] Found success toast message:");
                                _output.WriteLine($"  Text: '{text}'");
                                _output.WriteLine($"  Selector: {selector}");
                                _output.WriteLine($"  Element classes: '{element.GetAttribute("class")}'");
                                break;
                            }
                        }
                    }
                    if (successToastFound) break;
                }
                catch { }
            }

            Assert.True(successToastFound, $"Expected success toast message with 'Document Uploaded' or 'Successfully uploaded the document' but found: {(string.IsNullOrWhiteSpace(toastText) ? "no toast message" : toastText)}");
            _output.WriteLine("[PASS] ✓ Success toast message verified");

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST COMPLETED");
            _output.WriteLine("========================================");
        }

        /// <summary>
        /// Finds the uploaded documents table (CSS selectors only)
        /// </summary>
        private IWebElement FindUploadedDocumentsTable(IPookieWebDriver driver)
        {
            var selectors = new[]
            {
                "table[id*='UploadedFile']",
                "table[id*='Document']",
                "table[class*='table']",
                "table"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var tables = driver.FindElements(By.CssSelector(selector));
                    foreach (var table in tables)
                    {
                        if (!table.Displayed) continue;
                        
                        // Check if table contains document-related content
                        var tableText = table.Text?.Trim() ?? "";
                        if (tableText.IndexOf("pdf", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            tableText.IndexOf("View", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            tableText.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _output.WriteLine($"[INFO] Found uploaded documents table using selector '{selector}'");
                            return table;
                        }
                    }
                }
                catch { }
            }

            throw new InvalidOperationException("Unable to locate uploaded documents table.");
        }

        /// <summary>
        /// Finds the delete button for uploaded documents (CSS selectors only)
        /// </summary>
        private IWebElement FindDeleteDocumentButton(IWebElement documentRow)
        {
            if (documentRow == null)
            {
                throw new ArgumentNullException(nameof(documentRow));
            }

            var selectors = new[]
            {
                "button.btn-danger.delete-gridview",
                "button[class*='delete-gridview']",
                "button.btn-danger",
                "button[data-target*='DeleteUploadedFileModal']",
                "button[class*='btn-danger']"
            };

            foreach (var selector in selectors)
            {
                var match = documentRow.FindElements(By.CssSelector(selector))
                    .FirstOrDefault(el => el.Displayed && el.Enabled &&
                        (ElementTextContains(el, "Delete") ||
                         ElementHasIcon(el, "glyphicon-trash")));
                if (match != null)
                {
                    _output.WriteLine($"[INFO] Found delete button via selector '{selector}'");
                    return match;
                }
            }

            // Fallback: look for any button with delete icon or text
            var fallback = documentRow
                .FindElements(By.CssSelector("button, a"))
                .FirstOrDefault(el =>
                {
                    if (!el.Displayed || !el.Enabled) return false;
                    
                    var text = el.Text?.Trim() ?? "";
                    var className = el.GetAttribute("class") ?? "";
                    var dataTarget = el.GetAttribute("data-target") ?? "";
                    
                    return (text.Equals("Delete", StringComparison.OrdinalIgnoreCase) ||
                            className.IndexOf("delete", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            dataTarget.IndexOf("DeleteUploadedFileModal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            ElementHasIcon(el, "glyphicon-trash"));
                });

            if (fallback != null)
            {
                _output.WriteLine("[INFO] Found delete button via fallback search");
                return fallback;
            }

            throw new InvalidOperationException("Unable to locate delete button in document row.");
        }

        /// <summary>
        /// Handles the delete confirmation modal dialog (CSS selectors only)
        /// </summary>
        private void HandleDeleteConfirmationModal(IPookieWebDriver driver, bool confirm)
        {
            System.Threading.Thread.Sleep(1000);

            // Look for modal dialog
            var modalSelectors = new[]
            {
                By.CssSelector("#divDeleteUploadedFileModal"),
                By.CssSelector("[id*='DeleteUploadedFileModal']"),
                By.CssSelector(".modal"),
                By.CssSelector("[class*='modal']")
            };

            IWebElement modal = null;
            foreach (var selector in modalSelectors)
            {
                try
                {
                    var modals = driver.FindElements(selector);
                    modal = modals.FirstOrDefault(m => m.Displayed);
                    if (modal != null)
                    {
                        _output.WriteLine($"[INFO] Found delete confirmation modal");
                        break;
                    }
                }
                catch { }
            }

            if (modal != null)
            {
                // Look for Yes/No buttons inside the modal
                var buttonText = confirm ? "Yes" : "No";
                var buttonSelectors = new[]
                {
                    By.CssSelector("button"),
                    By.CssSelector("a.btn"),
                    By.CssSelector(".btn")
                };

                foreach (var btnSelector in buttonSelectors)
                {
                    try
                    {
                        var buttons = modal.FindElements(btnSelector);
                        var targetButton = buttons.FirstOrDefault(btn =>
                        {
                            if (!btn.Displayed || !btn.Enabled) return false;
                            var text = btn.Text?.Trim() ?? "";
                            return text.Equals(buttonText, StringComparison.OrdinalIgnoreCase) ||
                                   text.Equals(confirm ? "OK" : "Cancel", StringComparison.OrdinalIgnoreCase) ||
                                   (confirm && text.Equals("Confirm", StringComparison.OrdinalIgnoreCase));
                        });

                        if (targetButton != null)
                        {
                            _output.WriteLine($"[INFO] Found '{buttonText}' button in modal");
                            targetButton.Click();
                            _output.WriteLine($"[PASS] Clicked '{buttonText}' button");
                            return;
                        }
                    }
                    catch { }
                }
            }

            // Fallback: Try browser alert if modal not found
            try
            {
                var alert = driver.SwitchTo().Alert();
                _output.WriteLine($"[INFO] Browser alert detected: {alert.Text}");
                if (confirm)
                {
                    alert.Accept();
                    _output.WriteLine("[PASS] Accepted browser alert");
                }
                else
                {
                    alert.Dismiss();
                    _output.WriteLine("[PASS] Dismissed browser alert");
                }
            }
            catch
            {
                _output.WriteLine("[WARN] No confirmation modal or alert found");
            }
        }

        [Fact]
        public void UploadDocument_OpensUploadDialog()
        {
            using var driver = _driverFactory.CreateDriver();

            // Navigate to Referrals page
            LoginAndNavigateToReferrals(driver);

            // Click Edit on the first referral
            ClickEditOnFirstReferral(driver);

            // Click Upload New Document button
            ClickUploadNewDocumentButton(driver);

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: Verify Upload Dialog Opens");
            _output.WriteLine("========================================");

            // Check if file input appears after clicking Upload New Document button
            _output.WriteLine("\nChecking if file upload input is visible...");
            var fileInput = driver.FindElements(By.CssSelector("input[type='file']"))
                .FirstOrDefault(el => el.Displayed);

            Assert.NotNull(fileInput);
            _output.WriteLine("[SUCCESS] ✓ Upload dialog opened - file input is visible");
        }

        [Fact]
        public void UploadDocument_DeleteDocument_CancelsAndConfirmsDelete()
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: DELETE UPLOADED DOCUMENT");
            _output.WriteLine("========================================");

            // Navigate to Referrals page
            LoginAndNavigateToReferrals(driver);

            // Click Edit on the first referral
            ClickEditOnFirstReferral(driver);

            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING FOR UPLOADED DOCUMENTS");
            _output.WriteLine("========================================");

            // Find the uploaded documents table
            var documentsTable = FindUploadedDocumentsTable(driver);
            Assert.NotNull(documentsTable);
            _output.WriteLine("[PASS] Found uploaded documents table");

            // Get initial document rows
            var initialRows = documentsTable.FindElements(By.CssSelector("tbody tr, tr"))
                .Where(row => row.Displayed && 
                    !row.Text.Contains("No data available", StringComparison.OrdinalIgnoreCase) &&
                    row.Text.IndexOf("pdf", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            var initialRowCount = initialRows.Count;
            _output.WriteLine($"[INFO] Initial document count: {initialRowCount}");

            // If no documents exist, upload one first
            if (initialRowCount == 0)
            {
                _output.WriteLine("\n[INFO] No documents found, uploading one first...");
                
                // Click Upload New Document button
                ClickUploadNewDocumentButton(driver);

                // Get the path to the test PDF file
                var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "TestHFNY.pdf");
                if (!File.Exists(testFilePath))
                {
                    testFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestFiles", "TestHFNY.pdf");
                    testFilePath = Path.GetFullPath(testFilePath);
                }

                Assert.True(File.Exists(testFilePath), $"Test file not found at: {testFilePath}");

                // Find file upload input field
                var fileInput = FindFileUploadInputField(driver);
                fileInput.SendKeys(testFilePath);
                System.Threading.Thread.Sleep(1000);

                // Find and click Upload submit button
                var uploadSubmitButton = FindUploadSubmitButton(driver);
                driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", uploadSubmitButton);
                System.Threading.Thread.Sleep(500);
                uploadSubmitButton.Click();
                
                driver.WaitForReady(30);
                System.Threading.Thread.Sleep(3000);
                _output.WriteLine("[PASS] Uploaded test document");

                // Refresh the table
                documentsTable = FindUploadedDocumentsTable(driver);
                initialRows = documentsTable.FindElements(By.CssSelector("tbody tr, tr"))
                    .Where(row => row.Displayed && 
                        !row.Text.Contains("No data available", StringComparison.OrdinalIgnoreCase) &&
                        row.Text.IndexOf("pdf", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                initialRowCount = initialRows.Count;
            }

            Assert.True(initialRowCount > 0, "No documents available to delete!");
            _output.WriteLine($"[PASS] Found document to delete");

            var targetDocumentRow = initialRows[0];
            var initialRowText = targetDocumentRow.Text;
            _output.WriteLine($"[INFO] Target document row: {initialRowText}");

            _output.WriteLine("\n========================================");
            _output.WriteLine("FIRST DELETE ATTEMPT - CANCEL");
            _output.WriteLine("========================================");

            // Find the delete button
            var deleteButton = FindDeleteDocumentButton(targetDocumentRow);
            Assert.NotNull(deleteButton);
            _output.WriteLine($"[PASS] Found delete button: id='{deleteButton.GetAttribute("id")}', text='{deleteButton.Text?.Trim()}'");

            // Scroll to delete button and click
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", deleteButton);
            System.Threading.Thread.Sleep(500);
            deleteButton.Click();
            _output.WriteLine("[PASS] Clicked delete button");

            // Handle confirmation dialog - Click "No"
            _output.WriteLine("\n========================================");
            _output.WriteLine("HANDLING CONFIRMATION DIALOG - CLICK NO");
            _output.WriteLine("========================================");
            HandleDeleteConfirmationModal(driver, confirm: false);

            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(1000);

            // Verify document still exists
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING DOCUMENT STILL EXISTS");
            _output.WriteLine("========================================");

            documentsTable = FindUploadedDocumentsTable(driver);
            var rowsAfterCancel = documentsTable.FindElements(By.CssSelector("tbody tr, tr"))
                .Where(row => row.Displayed && 
                    !row.Text.Contains("No data available", StringComparison.OrdinalIgnoreCase) &&
                    row.Text.IndexOf("pdf", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            _output.WriteLine($"[INFO] Rows after cancel: {rowsAfterCancel.Count}");
            Assert.Equal(initialRowCount, rowsAfterCancel.Count);
            _output.WriteLine("[PASS] ✓ Document was NOT deleted (cancel worked correctly)");

            _output.WriteLine("\n========================================");
            _output.WriteLine("SECOND DELETE ATTEMPT - CONFIRM");
            _output.WriteLine("========================================");

            // Find the delete button again
            targetDocumentRow = rowsAfterCancel[0];
            deleteButton = FindDeleteDocumentButton(targetDocumentRow);
            Assert.NotNull(deleteButton);
            _output.WriteLine($"[PASS] Found delete button again: id='{deleteButton.GetAttribute("id")}', text='{deleteButton.Text?.Trim()}'");

            // Scroll to delete button and click
            driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", deleteButton);
            System.Threading.Thread.Sleep(500);
            deleteButton.Click();
            _output.WriteLine("[PASS] Clicked delete button again");

            // Handle confirmation dialog - Click "Yes"
            _output.WriteLine("\n========================================");
            _output.WriteLine("HANDLING CONFIRMATION DIALOG - CLICK YES");
            _output.WriteLine("========================================");
            HandleDeleteConfirmationModal(driver, confirm: true);

            // Wait for deletion to process
            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(2000);

            // Verify document was deleted
            _output.WriteLine("\n========================================");
            _output.WriteLine("VERIFYING DOCUMENT WAS DELETED");
            _output.WriteLine("========================================");

            documentsTable = FindUploadedDocumentsTable(driver);
            var rowsAfterDelete = documentsTable.FindElements(By.CssSelector("tbody tr, tr"))
                .Where(row => row.Displayed && 
                    !row.Text.Contains("No data available", StringComparison.OrdinalIgnoreCase) &&
                    row.Text.IndexOf("pdf", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            _output.WriteLine($"[INFO] Rows after delete: {rowsAfterDelete.Count}");
            _output.WriteLine($"[INFO] Expected rows: {initialRowCount - 1}");
            
            Assert.Equal(initialRowCount - 1, rowsAfterDelete.Count);
            _output.WriteLine("[PASS] ✓ Document count decreased by 1");

            // Verify the specific document no longer exists
            if (rowsAfterDelete.Count > 0)
            {
                var deletedRowStillExists = rowsAfterDelete.Any(row => row.Text == initialRowText);
                Assert.False(deletedRowStillExists, "The deleted document row should no longer exist!");
                _output.WriteLine("[PASS] ✓ Original document row was removed");
            }
            else
            {
                _output.WriteLine("[PASS] ✓ Documents table is now empty");
            }

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST SUMMARY");
            _output.WriteLine("========================================");
            _output.WriteLine("[PASS] Successfully navigated to referral edit page");
            _output.WriteLine("[PASS] Successfully found document to delete");
            _output.WriteLine("[PASS] Successfully cancelled first delete attempt");
            _output.WriteLine("[PASS] Verified document was NOT deleted after cancel");
            _output.WriteLine("[PASS] Successfully confirmed second delete attempt");
            _output.WriteLine("[PASS] Verified document WAS deleted successfully");
            _output.WriteLine("========================================");
        }

        #endregion
    }
}

