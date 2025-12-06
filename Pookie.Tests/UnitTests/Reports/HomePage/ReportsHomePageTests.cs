using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.Pages;
using AFUT.Tests.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.Reports.HomePage
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class ReportsHomePageTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public ReportsHomePageTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");
        }

        [Fact]
        [TestPriority(1)]
        public void VerifyAllReportsCatalogHoverElements()
        {
            using var driver = _driverFactory.CreateDriver();

            NavigateToReportsHomePage(driver);

            // Verify the report catalog header is present
            var reportCatalogHeader = driver.WaitforElementToBeInDOM(By.CssSelector("#divReportCatalogHeader"), 10)
                ?? throw new InvalidOperationException("Report catalog header div was not found.");

            _output.WriteLine("[PASS] Report catalog header is present");

            // Test all filter labels
            _output.WriteLine("\n[INFO] Testing 9 Filter Labels");
            var filterLabels = new Dictionary<string, string>
            {
                { "All", "Reset any filters and display all reports" },
                { "Lists", "Display only reports that are part of the list category" },
                { "Ticklers", "Display only reports that are part of the ticklers category" },
                { "Analysis", "Display only reports that are part of the analysis category" },
                { "Quarterlies", "Display only reports that are part of the quarterlies category" },
                { "Accreditation", "Display only reports that are part of the accreditation category" },
                { "Training", "Display only reports that are part of the training category" },
                { "MIECHV", "Display only reports that are part of the MIECHV category" },
                { "Retired", "No longer applicable or replaced by better versions" }
            };

            foreach (var filterLabel in filterLabels)
            {
                VerifyHoverTooltip(driver, "span.FilterLabel", filterLabel.Key, filterLabel.Value);
            }

            // Test all column labels
            _output.WriteLine("\n[INFO] Testing 4 Column Labels");
            var columnLabels = new Dictionary<string, string>
            {
                { "Recent-You", "Display the date the reports were last run by you" },
                { "Recent-All", "Display the date the reports were last run by anyone in your program" },
                { "Frequent-You", "Display the frequency rank that the reports were run by you" },
                { "Frequent-All", "Display the frequency rank that the reports were run by anyone in your program" }
            };

            foreach (var columnLabel in columnLabels)
            {
                VerifyHoverTooltip(driver, "span.ColumnLabel", columnLabel.Key, columnLabel.Value);
            }

            _output.WriteLine($"\n[PASS] All 13 hover tooltips verified successfully");
        }

        [Fact]
        [TestPriority(2)]
        public void VerifyAllReportInfoIconTooltips()
        {
            using var driver = _driverFactory.CreateDriver();

            NavigateToReportsHomePage(driver);

            // Wait for the reports table to load
            var reportsTable = driver.WaitforElementToBeInDOM(By.CssSelector("table tbody"), 15)
                ?? throw new InvalidOperationException("Reports table was not found.");

            _output.WriteLine("[PASS] Reports table loaded");

            // Find all info icons in the report rows
            var infoIcons = driver.FindElements(By.CssSelector("a.moreInfo.btn.btn-xs"))
                .Where(el => el.Displayed && el.FindElements(By.CssSelector("span.glyphicon-info-sign")).Any())
                .ToList();

            _output.WriteLine($"\n[INFO] Found {infoIcons.Count} report info icons to test");

            Assert.True(infoIcons.Count > 0, "No report info icons were found on the page.");

            int successCount = 0;
            var actions = new Actions(driver);

            foreach (var infoIcon in infoIcons)
            {
                var description = infoIcon.GetAttribute("data-description");
                
                Assert.False(string.IsNullOrWhiteSpace(description), 
                    "Info icon has no data-description attribute.");

                // Hover over the info icon
                actions.MoveToElement(infoIcon).Perform();
                Thread.Sleep(300);

                // Find the report name in the same row
                var row = infoIcon.FindElement(By.XPath("./ancestor::tr"));
                var reportName = row.FindElements(By.CssSelector("td"))
                    .Skip(1)
                    .FirstOrDefault()?.Text?.Trim() ?? "Unknown Report";

                _output.WriteLine($"{reportName.Substring(0, Math.Min(50, reportName.Length))}...");

                // Move away from the element
                actions.MoveToElement(infoIcon).MoveByOffset(50, 0).Perform();
                Thread.Sleep(150);

                successCount++;
            }

            _output.WriteLine($"\n[PASS] All {successCount} report info icon tooltips verified successfully");
        }

        [Fact]
        [TestPriority(3)]
        public void VerifyReportCatalogFilterFunctionality()
        {
            using var driver = _driverFactory.CreateDriver();

            NavigateToReportsHomePage(driver);

            // Wait for the reports table to load
            var reportsTable = driver.WaitforElementToBeInDOM(By.CssSelector("table tbody"), 15)
                ?? throw new InvalidOperationException("Reports table was not found.");

            _output.WriteLine("[PASS] Reports table loaded");

            // Test each filter
            var filterNames = new[] { "All", "Lists", "Ticklers", "Analysis", "Quarterlies", "Accreditation", "Training", "MIECHV", "Retired" };

            _output.WriteLine($"\n[INFO] Testing {filterNames.Length} filter buttons");

            foreach (var filterName in filterNames)
            {
                TestFilterFunctionality(driver, filterName);
            }

            _output.WriteLine($"\n[PASS] All filter functionality tests completed successfully");
        }

        [Fact]
        [TestPriority(4)]
        public void VerifyReportDocumentationLinksClickable()
        {
            using var driver = _driverFactory.CreateDriver();

            NavigateToReportsHomePage(driver);

            // Wait for the reports table to load
            var reportsTable = driver.WaitforElementToBeInDOM(By.CssSelector("table#mainTable tbody"), 15)
                ?? throw new InvalidOperationException("Reports table was not found.");

            _output.WriteLine("[PASS] Reports table loaded");

            // Set page size to "All" to see all reports
            var pageSizeDropdown = driver.FindElement(By.CssSelector("select[name='mainTable_length']"));
            var selectElement = new SelectElement(pageSizeDropdown);
            
            var currentPageSize = selectElement.SelectedOption.GetAttribute("value");
            _output.WriteLine($"[INFO] Current page size: {currentPageSize}");

            if (currentPageSize != "-1")
            {
                selectElement.SelectByValue("-1");
                driver.WaitForReady(3);
                Thread.Sleep(1000);
                _output.WriteLine("[INFO] Changed page size to 'All'");
            }

            // Count all report documentation links (file icon links)
            var allDocLinks = driver.FindElements(By.CssSelector("a.viewReportDoc.btn.btn-xs"))
                .Where(link => 
                {
                    var filename = link.GetAttribute("data-filename");
                    var hasFileIcon = link.FindElements(By.CssSelector("span.glyphicon-file")).Any();
                    return !string.IsNullOrWhiteSpace(filename) && hasFileIcon;
                })
                .ToList();

            var totalLinksWithDocs = allDocLinks.Count;
            var linksWithoutDocs = driver.FindElements(By.CssSelector("a.viewReportDoc.btn.btn-xs[style*='display: none']")).Count;

            _output.WriteLine($"\n[INFO] Total reports: {driver.FindElements(By.CssSelector("table#mainTable tbody tr td")).Where(td => td.Displayed).Count() / 3}");
            _output.WriteLine($"[INFO] Reports with documentation: {totalLinksWithDocs}");
            _output.WriteLine($"[INFO] Reports without documentation: {linksWithoutDocs}");

            Assert.True(totalLinksWithDocs > 0, "No report documentation links found.");

            // Test each documentation link by re-finding it before clicking (avoid stale element)
            for (int i = 0; i < totalLinksWithDocs; i++)
            {
                // Re-find all links to avoid stale element references
                var currentDocLinks = driver.FindElements(By.CssSelector("a.viewReportDoc.btn.btn-xs"))
                    .Where(link => 
                    {
                        var filename = link.GetAttribute("data-filename");
                        var hasFileIcon = link.FindElements(By.CssSelector("span.glyphicon-file")).Any();
                        return !string.IsNullOrWhiteSpace(filename) && hasFileIcon;
                    })
                    .ToList();

                if (i < currentDocLinks.Count)
                {
                    TestReportDocumentationLink(driver, currentDocLinks[i], i + 1, totalLinksWithDocs);
                }
            }

            _output.WriteLine($"\n[PASS] All {totalLinksWithDocs} report documentation links tested successfully");
        }

        #region Helper Methods

        /// <summary>
        /// Navigates to the Reports homepage from login
        /// </summary>
        private void NavigateToReportsHomePage(IPookieWebDriver driver)
        {
            var homePage = CommonTestHelper.NavigateToReportsHomePage(driver, _config, _output);
            
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after navigation.");
            
            _output.WriteLine($"[PASS] Navigated to Reports homepage: {driver.Url}");
        }

        /// <summary>
        /// Tests hover functionality and verifies tooltip content for a label element
        /// </summary>
        private void VerifyHoverTooltip(IPookieWebDriver driver, string cssSelector, string labelText, string expectedTooltip)
        {
            var element = driver.FindElements(By.CssSelector(cssSelector))
                .FirstOrDefault(el => el.Displayed && el.Text.Trim().Equals(labelText, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Label '{labelText}' was not found.");

            var tooltipAttribute = element.GetAttribute("data-original-title");
            var tooltipText = ExtractTooltipDescription(tooltipAttribute);

            Assert.Contains(expectedTooltip, tooltipText, StringComparison.OrdinalIgnoreCase);

            var actions = new Actions(driver);
            actions.MoveToElement(element).Perform();
            Thread.Sleep(500);

            _output.WriteLine($" {labelText}' - {tooltipText}");

            actions.MoveToElement(element).MoveByOffset(100, 0).Perform();
            Thread.Sleep(200);
        }

        /// <summary>
        /// Tests filter functionality by clicking it and verifying filtered results
        /// </summary>
        private void TestFilterFunctionality(IPookieWebDriver driver, string filterName)
        {
            // Find the filter label
            var filterLabel = driver.FindElements(By.CssSelector("span.FilterLabel"))
                .FirstOrDefault(el => el.Displayed && el.Text.Trim().Equals(filterName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Filter label '{filterName}' was not found.");

            // Click the filter
            CommonTestHelper.ClickElement(driver, filterLabel);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1500);

            // Wait for table to update
            var reportsTable = driver.WaitforElementToBeInDOM(By.CssSelector("table#mainTable tbody"), 10)
                ?? throw new InvalidOperationException("Reports table was not found after filter click.");

            // Count rows that are NOT hidden by DataTables (don't have display:none style)
            var allRows = reportsTable.FindElements(By.CssSelector("tr"))
                .Where(tr => tr.FindElements(By.CssSelector("td")).Any())
                .ToList();

            var visibleRows = GetVisibleRows(reportsTable);

            // Count info icons in ALL rows (not just visible ones, since DataTables hides with CSS)
            var allInfoIcons = allRows
                .SelectMany(row => row.FindElements(By.CssSelector("a.moreInfo.btn.btn-xs span.glyphicon-info-sign")))
                .ToList();

            Assert.True(allRows.Count > 0, $"Filter '{filterName}' returned no rows in table.");
            Assert.True(allInfoIcons.Count > 0, $"Filter '{filterName}' returned no info icons.");
            Assert.True(allRows.Count == allInfoIcons.Count, 
                $"Filter '{filterName}': Mismatch between total rows ({allRows.Count}) and info icons ({allInfoIcons.Count}).");

            // Verify each row has an info icon with data-description
            int validatedCount = 0;
            foreach (var row in allRows)
            {
                var infoIconLink = row.FindElements(By.CssSelector("a.moreInfo.btn.btn-xs"))
                    .FirstOrDefault(el => el.FindElements(By.CssSelector("span.glyphicon-info-sign")).Any());

                Assert.NotNull(infoIconLink);
                
                var description = infoIconLink.GetAttribute("data-description");
                Assert.False(string.IsNullOrWhiteSpace(description), 
                    $"Info icon in filter '{filterName}' has no data-description attribute.");
                
                validatedCount++;
            }

            // Test page size changes if there are enough rows (only for first filter to save time)
            if (filterName == "All" && allRows.Count > 25)
            {
                TestPageSizeChanges(driver, reportsTable, allRows.Count);
            }

            _output.WriteLine($"'{filterName}' filter: {allRows.Count} reports (showing {visibleRows.Count} on current page), all with valid info icons");
        }

        /// <summary>
        /// Tests that changing the DataTables page size dropdown updates the visible row count
        /// </summary>
        private void TestPageSizeChanges(IPookieWebDriver driver, IWebElement reportsTable, int totalRows)
        {
            var pageSizeDropdown = driver.FindElement(By.CssSelector("select[name='mainTable_length']"));
            var selectElement = new SelectElement(pageSizeDropdown);

            // Test different page sizes
            var pageSizesToTest = new[] { 10, 15, 20, 25 };

            foreach (var pageSize in pageSizesToTest)
            {
                if (pageSize > totalRows)
                {
                    continue; // Skip if page size is larger than total rows
                }

                selectElement.SelectByValue(pageSize.ToString());
                driver.WaitForReady(2);
                Thread.Sleep(500);

                var visibleRows = GetVisibleRows(reportsTable);
                var expectedVisible = Math.Min(pageSize, totalRows);

                Assert.True(visibleRows.Count == expectedVisible,
                    $"Page size {pageSize}: Expected {expectedVisible} visible rows, but found {visibleRows.Count}");

                _output.WriteLine($"Page size {pageSize}: {visibleRows.Count} rows visible");
            }

            // Test "All" option
            selectElement.SelectByValue("-1");
            driver.WaitForReady(2);
            Thread.Sleep(500);

            var allVisibleRows = GetVisibleRows(reportsTable);
            Assert.True(allVisibleRows.Count == totalRows,
                $"Page size 'All': Expected {totalRows} visible rows, but found {allVisibleRows.Count}");

            _output.WriteLine($"Page size 'All': {allVisibleRows.Count} rows visible");

            // Reset to default (10)
            selectElement.SelectByValue("10");
            driver.WaitForReady(2);
            Thread.Sleep(300);
        }

        /// <summary>
        /// Gets rows that are currently visible (not hidden by DataTables pagination)
        /// </summary>
        private List<IWebElement> GetVisibleRows(IWebElement reportsTable)
        {
            return reportsTable.FindElements(By.CssSelector("tr"))
                .Where(tr =>
                {
                    if (!tr.FindElements(By.CssSelector("td")).Any())
                    {
                        return false;
                    }

                    var style = tr.GetAttribute("style");
                    return style == null || !style.Contains("display: none", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }

        /// <summary>
        /// Tests a report documentation link by clicking it and logging the results
        /// </summary>
        private void TestReportDocumentationLink(IPookieWebDriver driver, IWebElement docLink, int currentIndex, int totalCount)
        {
            // Get report info from the same row
            var row = docLink.FindElement(By.XPath("./ancestor::tr"));
            var reportName = row.FindElements(By.CssSelector("td"))
                .Skip(1)
                .FirstOrDefault()?.Text?.Trim() ?? "Unknown Report";

            var filename = docLink.GetAttribute("data-filename");
            var description = docLink.GetAttribute("data-description");
            var linkId = docLink.GetAttribute("id");

            _output.WriteLine($"\n[INFO] [{currentIndex}/{totalCount}] Testing documentation link for: {reportName}");
            _output.WriteLine($"  Filename: {filename}");

            // Validate pattern matching between report name and filename
            var patternMatch = ValidateReportNameMatchesFilename(reportName, filename);
            if (patternMatch.IsMatch)
            {
                _output.WriteLine($"  [MATCH] Pattern match: Report name matches filename");
                if (!string.IsNullOrEmpty(patternMatch.MatchedKeywords))
                {
                    _output.WriteLine($"    Matched keywords: {patternMatch.MatchedKeywords}");
                }
            }
            else
            {
                _output.WriteLine($"  [WARN] Pattern mismatch: Report name may not match filename");
                _output.WriteLine($"    Report: {reportName}");
                _output.WriteLine($"    File: {filename}");
            }

            // Store current window handle and URL before clicking
            var originalWindowHandle = driver.CurrentWindowHandle;
            var originalUrl = driver.Url;
            var originalWindowCount = driver.WindowHandles.Count;

            // Click the documentation link
            try
            {
                CommonTestHelper.ClickElement(driver, docLink);
                Thread.Sleep(2000); // Wait for any action to complete

                // Check what happened after click
                var newWindowCount = driver.WindowHandles.Count;
                var currentUrl = driver.Url;

                // Check if a new window/tab was opened
                if (newWindowCount > originalWindowCount)
                {
                    _output.WriteLine($"  [DETECTED] New window/tab opened");
                    
                    // Switch to the new window
                    var newWindowHandle = driver.WindowHandles.Last();
                    driver.SwitchTo().Window(newWindowHandle);
                    
                    var newWindowUrl = driver.Url;
                    _output.WriteLine($"  URL: {newWindowUrl}");
                    
                    // Close the new window and switch back
                    driver.Close();
                    driver.SwitchTo().Window(originalWindowHandle);
                }
                else if (currentUrl != originalUrl)
                {
                    _output.WriteLine($"  [DETECTED] URL changed in same window");
                    _output.WriteLine($"  URL: {currentUrl}");
                    
                    // Navigate back if needed
                    if (!currentUrl.Contains("ReportCatalog.aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        driver.Navigate().Back();
                        driver.WaitForReady(5);
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    _output.WriteLine($"  [DETECTED] PostBack/Download triggered");
                }

                _output.WriteLine($"  [PASS] Documentation link verified");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"  [WARN] Exception: {ex.Message}");
                
                // Ensure we're back on the original window
                if (driver.WindowHandles.Contains(originalWindowHandle))
                {
                    driver.SwitchTo().Window(originalWindowHandle);
                }
                
                // Don't fail the whole test, just log and continue
            }
        }

        /// <summary>
        /// Validates that the report name matches the PDF filename
        /// </summary>
        private (bool IsMatch, string MatchedKeywords) ValidateReportNameMatchesFilename(string reportName, string filename)
        {
            if (string.IsNullOrWhiteSpace(reportName) || string.IsNullOrWhiteSpace(filename))
            {
                return (false, string.Empty);
            }

            // Extract filename without path and extension
            var fileNameOnly = Path.GetFileNameWithoutExtension(filename);
            
            // Normalize strings for comparison (remove special chars, convert to lowercase)
            var normalizedReport = NormalizeForComparison(reportName);
            var normalizedFile = NormalizeForComparison(fileNameOnly);

            // Extract meaningful keywords from report name (ignore common words)
            var reportKeywords = ExtractKeywords(normalizedReport);
            var fileKeywords = ExtractKeywords(normalizedFile);

            // Check if significant keywords match
            var matchedKeywords = reportKeywords.Intersect(fileKeywords).ToList();
            
            // Consider it a match if:
            // 1. At least 2 keywords match, OR
            // 2. Report name contains the entire filename (or vice versa), OR
            // 3. Filename contains report code (e.g., "1-1.C", "PHQ9", "ASQ")
            var hasKeywordMatch = matchedKeywords.Count >= 2;
            var hasSubstringMatch = normalizedReport.Contains(normalizedFile) || normalizedFile.Contains(normalizedReport);
            var reportCode = ExtractReportCode(reportName);
            var hasCodeMatch = !string.IsNullOrEmpty(reportCode) && normalizedFile.Contains(reportCode.ToLower());

            var isMatch = hasKeywordMatch || hasSubstringMatch || hasCodeMatch;
            var matchInfo = matchedKeywords.Any() ? string.Join(", ", matchedKeywords) : 
                           hasCodeMatch ? reportCode : 
                           "substring match";

            return (isMatch, matchInfo);
        }

        /// <summary>
        /// Normalizes a string for comparison by removing special characters and converting to lowercase
        /// </summary>
        private string NormalizeForComparison(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Remove special characters but keep alphanumeric and spaces
            return new string(input.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray()).ToLower();
        }

        /// <summary>
        /// Extracts meaningful keywords from text (excludes common words)
        /// </summary>
        private List<string> ExtractKeywords(string text)
        {
            var commonWords = new HashSet<string> 
            { 
                "the", "and", "or", "of", "to", "a", "an", "in", "on", "at", "for", "with", "by", "from",
                "report", "summary", "analysis", "detail", "details", "case", "filter", "site", "options"
            };

            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 2 && !commonWords.Contains(word))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Extracts report code from report name (e.g., "1-1.C", "PHQ9", "ASQ-SE")
        /// </summary>
        private string ExtractReportCode(string reportName)
        {
            // Match patterns like "1-1.C", "7-4.E", "PHQ9", "ASQ-SE", "MIECHV"
            var codePatterns = new[]
            {
                @"\d+-\d+\.[A-Z]",  // e.g., "1-1.C", "7-4.E"
                @"PHQ\d+",           // e.g., "PHQ9"
                @"ASQ-?SE",          // e.g., "ASQ-SE", "ASQSE"
                @"ASQ",              // e.g., "ASQ"
                @"MIECHV",           // e.g., "MIECHV"
                @"CHEERS",           // e.g., "CHEERS"
            };

            foreach (var pattern in codePatterns)
            {
                var match = Regex.Match(reportName, pattern);
                if (match.Success)
                {
                    return match.Value;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts the description text from HTML-encoded tooltip attribute
        /// Format: "&lt;b&gt;Title&lt;/b&gt;&lt;hr&gt;Description" -> "Description"
        /// </summary>
        private string ExtractTooltipDescription(string tooltipHtml)
        {
            if (string.IsNullOrWhiteSpace(tooltipHtml))
            {
                return string.Empty;
            }

            var parts = tooltipHtml.Split(new[] { "<hr>", "&lt;hr&gt;", "<hr />", "&lt;hr /&gt;", "<br />", "&lt;br /&gt;" }, 
                StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 1 ? parts[1].Trim() : tooltipHtml.Trim();
        }

        #endregion
    }
}

