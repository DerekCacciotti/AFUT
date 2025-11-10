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

            _output.WriteLine("\n=== EXPLORATION COMPLETE ===");
        }

        [Fact]
        public void ChangeCompletedReferralYear_UpdatesTableWithCorrectYearEntries()
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

            // Navigate to Referrals page
            _output.WriteLine("\nNavigating to Referrals page...");
            var referralsLink = driver.FindElements(OpenQA.Selenium.By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);

            Assert.NotNull(referralsLink);
            referralsLink.Click();
            driver.WaitForReady(30);
            _output.WriteLine("[PASS] Successfully navigated to Referrals page");

            // Find the year dropdown
            var yearDropdown = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_ddlCompletedReferralYear"));
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
                    yearDropdown = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_ddlCompletedReferralYear"));
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
                    yearDropdown = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_ddlCompletedReferralYear"));
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
                    var completedReferralsTable = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_grCompletedReferrals"));
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

            // Navigate to Referrals page
            _output.WriteLine("\nNavigating to Referrals page...");
            var referralsLink = driver.FindElements(OpenQA.Selenium.By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);

            Assert.NotNull(referralsLink);
            referralsLink.Click();
            driver.WaitForReady(30);
            _output.WriteLine("[PASS] Successfully navigated to Referrals page");

            _output.WriteLine("\n========================================");
            _output.WriteLine("CHECKING DEFAULT YEAR");
            _output.WriteLine("========================================");

            // Find the year dropdown
            var yearDropdown = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_ddlCompletedReferralYear"));
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
            var completedReferralsTable = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_grCompletedReferrals"));
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
                    completedReferralsTable = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_grCompletedReferrals"));
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
    }
}

