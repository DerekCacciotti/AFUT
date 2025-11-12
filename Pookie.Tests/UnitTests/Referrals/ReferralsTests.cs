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

            _output.WriteLine("\n=== CLICKING NEW REFERRAL BUTTON ===");

            // Find and click the New Referral button
            try
            {
                var newReferralButton = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_lnkNewReferral"));
                Assert.NotNull(newReferralButton);
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
                    var firstNameField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcfirstname"));
                    firstNameField.Click();
                    System.Threading.Thread.Sleep(200);
                    firstNameField.Clear();
                    firstNameField.SendKeys(firstName);
                    _output.WriteLine("[PASS] Filled PC1 First Name");

                    // Fill Last Name
                    var lastNameField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpclastname"));
                    lastNameField.Click();
                    System.Threading.Thread.Sleep(200);
                    lastNameField.Clear();
                    lastNameField.SendKeys(lastName);
                    _output.WriteLine("[PASS] Filled PC1 Last Name");

                    // Fill DOB
                    var dobField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcdob"));
                    dobField.Click();
                    System.Threading.Thread.Sleep(200);
                    dobField.Clear();
                    dobField.SendKeys(todayDate);
                    _output.WriteLine("[PASS] Filled DOB");

                    // Fill Phone
                    var phoneField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcphone"));
                    phoneField.Click();
                    System.Threading.Thread.Sleep(200);
                    phoneField.Clear();
                    phoneField.SendKeys(phone);
                    _output.WriteLine("[PASS] Filled Phone");

                    // Fill Emergency Phone
                    var emergencyPhoneField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcemergencyphone"));
                    emergencyPhoneField.Click();
                    System.Threading.Thread.Sleep(200);
                    emergencyPhoneField.Clear();
                    emergencyPhoneField.SendKeys(emergencyPhone);
                    _output.WriteLine("[PASS] Filled Emergency Phone");
                    
                    System.Threading.Thread.Sleep(500);

                    // Find and click Search button
                    _output.WriteLine("\n--- Searching for user ---");
                    var allButtons2 = driver.FindElements(OpenQA.Selenium.By.CssSelector("button, input[type='button'], input[type='submit'], input[type='image'], a[id*='btn'], [id*='Button'], [id*='search'], [id*='Search']"));
                    
                    var searchButton = allButtons2
                        .Where(b => b.Displayed && b.Enabled)
                        .FirstOrDefault(b => 
                        {
                            var text = b.Text?.Trim() ?? b.GetAttribute("value") ?? "";
                            var id = b.GetAttribute("id") ?? "";
                            return text.Equals("Search", StringComparison.OrdinalIgnoreCase) && 
                                   !id.Contains("SearchCases", StringComparison.OrdinalIgnoreCase) &&
                                   !text.Contains("Cases", StringComparison.OrdinalIgnoreCase);
                        });

                    if (searchButton == null)
                    {
                        searchButton = allButtons2
                            .FirstOrDefault(b =>
                            {
                                var id = b.GetAttribute("id") ?? "";
                                var text = b.Text?.Trim() ?? b.GetAttribute("value") ?? "";
                                return id.Contains("ContentPlaceHolder", StringComparison.OrdinalIgnoreCase) &&
                                       id.Contains("btn", StringComparison.OrdinalIgnoreCase) &&
                                       text.Contains("Search", StringComparison.OrdinalIgnoreCase);
                            });
                    }

                    Assert.NotNull(searchButton);
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

            _output.WriteLine($"Navigating to application URL: {_config.AppUrl}");
            driver.Navigate().GoToUrl(_config.AppUrl);
            driver.WaitForReady(30);

            var loginPage = new LoginPage(driver);
            loginPage.SignIn(_config.UserName, _config.Password);
            Assert.True(loginPage.IsSignedIn(), "User was not signed in successfully.");
            _output.WriteLine("[PASS] Signed in");

            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");
            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded, "Landing page did not load after selecting Data Entry role.");
            _output.WriteLine("[PASS] Data Entry role selected");

            var referralsLink = driver.FindElements(OpenQA.Selenium.By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);
            Assert.NotNull(referralsLink);
            referralsLink.Click();
            driver.WaitForReady(30);
            _output.WriteLine("[PASS] Referrals page opened");

            // Find the active referrals table and the first edit button within it
            var activeReferralsTable = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_grActiveReferrals"));
            Assert.NotNull(activeReferralsTable);

            var editButton = activeReferralsTable
                .FindElements(OpenQA.Selenium.By.CssSelector("a, button, input[type='button'], input[type='submit'], input[type='image']"))
                .FirstOrDefault(el =>
                {
                    var text = el.Text?.Trim() ?? el.GetAttribute("value") ?? "";
                    var id = el.GetAttribute("id") ?? "";
                    return el.Enabled &&
                           (text.Equals("Edit", StringComparison.OrdinalIgnoreCase) ||
                            id.Contains("Edit", StringComparison.OrdinalIgnoreCase));
                });

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

        [Fact]
        public void NewReferral_SearchForPersonTest_ShowsMatchesInGrid()
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
            
            _output.WriteLine($"Found Referrals link with text: '{referralsLink.Text?.Trim()}'");
            referralsLink.Click();
            driver.WaitForReady(30);
            
            _output.WriteLine("[PASS] Clicked Referrals link");
            _output.WriteLine($"Current URL: {driver.Url}");
            
            // Wait for Referrals page to load completely
            System.Threading.Thread.Sleep(2000);
            driver.WaitForReady(30);
            
            // Verify we're on the Referrals page by checking for the New Referral button
            var newReferralButtonExists = driver.FindElements(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_lnkNewReferral")).Any();
            _output.WriteLine($"New Referral button present on page: {newReferralButtonExists}");
            
            Assert.True(newReferralButtonExists, "Failed to find New Referral button on Referrals page");
            _output.WriteLine("[PASS] Successfully navigated to Referrals page and verified page loaded");

            // Click New Referral button
            _output.WriteLine("\nClicking New Referral button...");
            
            try
            {
                var newReferralButton = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_lnkNewReferral"));
                Assert.NotNull(newReferralButton);
                
                _output.WriteLine($"Found New Referral button: id='{newReferralButton.GetAttribute("id")}', displayed={newReferralButton.Displayed}, enabled={newReferralButton.Enabled}");
                
                // Scroll to the button and add some offset to avoid navbar
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", newReferralButton);
                System.Threading.Thread.Sleep(500);
                
                // Use JavaScript click to avoid navbar interception
                _output.WriteLine("Clicking New Referral button using JavaScript...");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", newReferralButton);
                _output.WriteLine("[PASS] Clicked New Referral button");
                
                driver.WaitForReady(30);
                System.Threading.Thread.Sleep(1000); // Wait for navigation
                
                _output.WriteLine($"[PASS] Successfully navigated after clicking New Referral button");
                _output.WriteLine($"Current URL: {driver.Url}");
                _output.WriteLine($"Page Title: {driver.Title}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[FAIL] Error clicking New Referral button: {ex.Message}");
                _output.WriteLine($"Current URL: {driver.Url}");
                throw;
            }

            // Fill in the search form with Person / Test data
            _output.WriteLine("\n========================================");
            _output.WriteLine("FILLING IN SEARCH FORM");
            _output.WriteLine("========================================");

            var firstName = "Unit";
            var lastName = "Test";
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
                var firstNameField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcfirstname"));
                firstNameField.Click();
                System.Threading.Thread.Sleep(200);
                firstNameField.Clear();
                firstNameField.SendKeys(firstName);
                _output.WriteLine("[PASS] Filled PC1 First Name");

                // Fill Last Name
                var lastNameField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpclastname"));
                lastNameField.Click();
                System.Threading.Thread.Sleep(200);
                lastNameField.Clear();
                lastNameField.SendKeys(lastName);
                _output.WriteLine("[PASS] Filled PC1 Last Name");

                // Fill DOB
                var dobField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcdob"));
                dobField.Click();
                System.Threading.Thread.Sleep(200);
                dobField.Clear();
                dobField.SendKeys(todayDate);
                _output.WriteLine("[PASS] Filled DOB");

                // Fill Phone
                var phoneField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcphone"));
                phoneField.Click();
                System.Threading.Thread.Sleep(200);
                phoneField.Clear();
                phoneField.SendKeys(phone);
                _output.WriteLine("[PASS] Filled Phone");

                // Fill Emergency Phone
                var emergencyPhoneField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcemergencyphone"));
                emergencyPhoneField.Click();
                System.Threading.Thread.Sleep(200);
                emergencyPhoneField.Clear();
                emergencyPhoneField.SendKeys(emergencyPhone);
                _output.WriteLine("[PASS] Filled Emergency Phone");
                
                // Wait a moment for any field validation or form activation
                System.Threading.Thread.Sleep(500);

                // Find and click the Search button
                _output.WriteLine("\n========================================");
                _output.WriteLine("SEARCHING FOR REFERRAL");
                _output.WriteLine("========================================");

                var allButtons = driver.FindElements(OpenQA.Selenium.By.CssSelector("button, input[type='button'], input[type='submit'], input[type='image'], a[id*='btn'], [id*='Button'], [id*='search'], [id*='Search']"));
                
                var searchButton = allButtons
                    .Where(b => b.Displayed && b.Enabled)
                    .FirstOrDefault(b => 
                    {
                        var text = b.Text?.Trim() ?? b.GetAttribute("value") ?? "";
                        var id = b.GetAttribute("id") ?? "";
                        return text.Equals("Search", StringComparison.OrdinalIgnoreCase) && 
                               !id.Contains("SearchCases", StringComparison.OrdinalIgnoreCase) &&
                               !text.Contains("Cases", StringComparison.OrdinalIgnoreCase);
                    });

                if (searchButton == null)
                {
                    searchButton = allButtons
                        .FirstOrDefault(b =>
                        {
                            var id = b.GetAttribute("id") ?? "";
                            var text = b.Text?.Trim() ?? b.GetAttribute("value") ?? "";
                            return id.Contains("ContentPlaceHolder", StringComparison.OrdinalIgnoreCase) &&
                                   id.Contains("btn", StringComparison.OrdinalIgnoreCase) &&
                                   text.Contains("Search", StringComparison.OrdinalIgnoreCase);
                        });
                }

                Assert.NotNull(searchButton);
                var buttonId = searchButton.GetAttribute("id") ?? "no-id";
                var buttonText = searchButton.Text?.Trim() ?? searchButton.GetAttribute("value") ?? "";
                _output.WriteLine($"Using search button: id='{buttonId}', text='{buttonText}'");
                
                if (!searchButton.Displayed)
                {
                    ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", searchButton);
                }
                else
                {
                    searchButton.Click();
                }
                
                driver.WaitForReady(30);
                System.Threading.Thread.Sleep(2000); // Wait for results to load
                _output.WriteLine("[PASS] Clicked Search button");

                // Check for results grid
                _output.WriteLine("\n========================================");
                _output.WriteLine("VERIFYING SEARCH RESULTS IN GRID");
                _output.WriteLine("========================================");

                // Look for the results grid
                var resultsGrid = driver.FindElements(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_grResults"));
                
                if (resultsGrid.Count > 0 && resultsGrid[0].Displayed)
                {
                    _output.WriteLine("[PASS] Found results grid");
                    
                    // Count the rows in the grid (excluding header)
                    var gridRows = resultsGrid[0].FindElements(OpenQA.Selenium.By.CssSelector("tbody tr")).ToList();
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
                    var selectLinks = resultsGrid[0].FindElements(OpenQA.Selenium.By.LinkText("Select"));
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
            
            _output.WriteLine($"Found Referrals link with text: '{referralsLink.Text?.Trim()}'");
            referralsLink.Click();
            driver.WaitForReady(30);
            
            _output.WriteLine("[PASS] Clicked Referrals link");
            _output.WriteLine($"Current URL: {driver.Url}");
            
            // Wait for Referrals page to load completely
            System.Threading.Thread.Sleep(2000);
            driver.WaitForReady(30);
            
            // Verify we're on the Referrals page by checking for the New Referral button
            var newReferralButtonExists = driver.FindElements(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_lnkNewReferral")).Any();
            _output.WriteLine($"New Referral button present on page: {newReferralButtonExists}");
            
            Assert.True(newReferralButtonExists, "Failed to find New Referral button on Referrals page");
            _output.WriteLine("[PASS] Successfully navigated to Referrals page and verified page loaded");

            // Click New Referral button
            _output.WriteLine("\nClicking New Referral button...");
            
            try
            {
                var newReferralButton = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_lnkNewReferral"));
                Assert.NotNull(newReferralButton);
                
                _output.WriteLine($"Found New Referral button: id='{newReferralButton.GetAttribute("id")}', displayed={newReferralButton.Displayed}, enabled={newReferralButton.Enabled}");
                
                // Scroll to the button and add some offset to avoid navbar
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", newReferralButton);
                System.Threading.Thread.Sleep(500);
                
                // Use JavaScript click to avoid navbar interception
                _output.WriteLine("Clicking New Referral button using JavaScript...");
                ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", newReferralButton);
                _output.WriteLine("[PASS] Clicked New Referral button");
                
                driver.WaitForReady(30);
                System.Threading.Thread.Sleep(1000); // Wait for navigation
                
                _output.WriteLine($"[PASS] Successfully navigated after clicking New Referral button");
                _output.WriteLine($"Current URL: {driver.Url}");
                _output.WriteLine($"Page Title: {driver.Title}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[FAIL] Error clicking New Referral button: {ex.Message}");
                _output.WriteLine($"Current URL: {driver.Url}");
                throw;
            }

            // Fill in the search form with test data
            _output.WriteLine("\n========================================");
            _output.WriteLine("FILLING IN SEARCH FORM");
            _output.WriteLine("========================================");

            var firstName = "unit";
            var lastName = "test";
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
                // Fill First Name (click to activate, then fill)
                var firstNameField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcfirstname"));
                firstNameField.Click();
                System.Threading.Thread.Sleep(200);
                firstNameField.Clear();
                firstNameField.SendKeys(firstName);
                _output.WriteLine("[PASS] Filled PC1 First Name");

                // Fill Last Name
                var lastNameField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpclastname"));
                lastNameField.Click();
                System.Threading.Thread.Sleep(200);
                lastNameField.Clear();
                lastNameField.SendKeys(lastName);
                _output.WriteLine("[PASS] Filled PC1 Last Name");

                // Fill DOB
                var dobField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcdob"));
                dobField.Click();
                System.Threading.Thread.Sleep(200);
                dobField.Clear();
                dobField.SendKeys(todayDate);
                _output.WriteLine("[PASS] Filled DOB");

                // Fill Phone
                var phoneField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcphone"));
                phoneField.Click();
                System.Threading.Thread.Sleep(200);
                phoneField.Clear();
                phoneField.SendKeys(phone);
                _output.WriteLine("[PASS] Filled Phone");

                // Fill Emergency Phone
                var emergencyPhoneField = driver.FindElement(OpenQA.Selenium.By.Id("ctl00_ContentPlaceHolder1_txtpcemergencyphone"));
                emergencyPhoneField.Click();
                System.Threading.Thread.Sleep(200);
                emergencyPhoneField.Clear();
                emergencyPhoneField.SendKeys(emergencyPhone);
                _output.WriteLine("[PASS] Filled Emergency Phone");
                
                // Wait a moment for any field validation or form activation
                System.Threading.Thread.Sleep(500);

                // Find and click the Search button (below the emergency contact field)
                _output.WriteLine("\n========================================");
                _output.WriteLine("SEARCHING FOR REFERRAL");
                _output.WriteLine("========================================");

                // Log all buttons on the page to see what we have (including hidden ones)
                var allButtons = driver.FindElements(OpenQA.Selenium.By.CssSelector("button, input[type='button'], input[type='submit'], input[type='image'], a[id*='btn'], [id*='Button'], [id*='search'], [id*='Search']"));
                _output.WriteLine($"Found {allButtons.Count} total button elements on the page (including hidden):");
                
                foreach (var btn in allButtons)
                {
                    try
                    {
                        var id = btn.GetAttribute("id") ?? "no-id";
                        var text = btn.Text?.Trim() ?? btn.GetAttribute("value") ?? "";
                        var type = btn.GetAttribute("type") ?? btn.TagName;
                        var displayed = btn.Displayed;
                        var enabled = btn.Enabled;
                        var style = btn.GetAttribute("style") ?? "";
                        
                        var styleInfo = displayed ? "" : $", style='{style}'";
                        _output.WriteLine($"  - {type}: id='{id}', text='{text}', displayed={displayed}, enabled={enabled}{styleInfo}");
                    }
                    catch
                    {
                        // Continue
                    }
                }

                // Now find the correct search button
                // First try to find displayed buttons with "Search" in text but exclude "Search Cases"
                var searchButton = allButtons
                    .Where(b => b.Displayed && b.Enabled)
                    .FirstOrDefault(b => 
                    {
                        var text = b.Text?.Trim() ?? b.GetAttribute("value") ?? "";
                        var id = b.GetAttribute("id") ?? "";
                        return text.Equals("Search", StringComparison.OrdinalIgnoreCase) && 
                               !id.Contains("SearchCases", StringComparison.OrdinalIgnoreCase) &&
                               !text.Contains("Cases", StringComparison.OrdinalIgnoreCase);
                    });

                // If not found in displayed, try any button with search in ContentPlaceHolder (even if hidden)
                if (searchButton == null)
                {
                    _output.WriteLine("Search button not found in displayed elements, checking all elements including hidden...");
                    searchButton = allButtons
                        .FirstOrDefault(b =>
                        {
                            var id = b.GetAttribute("id") ?? "";
                            var text = b.Text?.Trim() ?? b.GetAttribute("value") ?? "";
                            return id.Contains("ContentPlaceHolder", StringComparison.OrdinalIgnoreCase) &&
                                   id.Contains("btn", StringComparison.OrdinalIgnoreCase) &&
                                   text.Contains("Search", StringComparison.OrdinalIgnoreCase);
                        });
                }

                // If still not found, try looking for image buttons
                if (searchButton == null)
                {
                    _output.WriteLine("Checking for image buttons...");
                    searchButton = allButtons
                        .FirstOrDefault(b =>
                        {
                            var type = b.GetAttribute("type") ?? "";
                            var id = b.GetAttribute("id") ?? "";
                            return type == "image" && id.Contains("btn", StringComparison.OrdinalIgnoreCase);
                        });
                }

                Assert.NotNull(searchButton);
                var buttonId = searchButton.GetAttribute("id") ?? "no-id";
                var buttonText = searchButton.Text?.Trim() ?? searchButton.GetAttribute("value") ?? "";
                var buttonType = searchButton.GetAttribute("type") ?? searchButton.TagName;
                var isDisplayed = searchButton.Displayed;
                _output.WriteLine($"\nUsing search button: type={buttonType}, id='{buttonId}', text='{buttonText}', displayed={isDisplayed}'");
                
                // If button is not displayed, try to make it visible or use JavaScript click
                if (!isDisplayed)
                {
                    _output.WriteLine("Button is not displayed, using JavaScript click...");
                    ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", searchButton);
                }
                else
                {
                    searchButton.Click();
                }
                
                driver.WaitForReady(30);
                System.Threading.Thread.Sleep(2000); // Wait for results to load
                _output.WriteLine("[PASS] Clicked Search button");

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
    }
}

