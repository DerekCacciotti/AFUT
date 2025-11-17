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
    public class DeleteCaseCheckTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public DeleteCaseCheckTests(AppConfig config, ITestOutputHelper output)
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
            _output.WriteLine("Navigating to application URL: {0}", _config.AppUrl);
            driver.Navigate().GoToUrl(_config.AppUrl);
            driver.WaitForReady(30);

            _output.WriteLine("Signing in with user: {0}", _config.UserName);
            var loginPage = new LoginPage(driver);
            loginPage.SignIn(_config.UserName, _config.Password);
            Assert.True(loginPage.IsSignedIn(), "User was not signed in successfully.");
            _output.WriteLine("[PASS] Successfully signed in");

            _output.WriteLine("Selecting DataEntry role...");
            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");
            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded, "Landing page did not load after selecting Data Entry role.");
            _output.WriteLine("[PASS] Successfully selected Data Entry role");

            _output.WriteLine("Navigating to Referrals page...");
            var referralsLink = driver.FindElements(OpenQA.Selenium.By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);
            Assert.NotNull(referralsLink);
            referralsLink.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Successfully navigated to Referrals page");
        }

        private OpenQA.Selenium.IWebElement FindActiveReferralsTable(IPookieWebDriver driver)
        {
            var match = driver.FindElements(OpenQA.Selenium.By.CssSelector(".table.table-condensed.table-responsive.dataTable.no-footer.dtr-column"))
                .FirstOrDefault(el => el.Displayed && LooksLikeActiveReferrals(el));
            
            return match ?? throw new InvalidOperationException("Unable to locate the Active Referrals table.");
        }

        private bool LooksLikeActiveReferrals(OpenQA.Selenium.IWebElement table)
        {
            var id = table.GetAttribute("id") ?? string.Empty;
            var className = table.GetAttribute("class") ?? string.Empty;
            return (className.IndexOf("active", StringComparison.OrdinalIgnoreCase) >= 0 && className.IndexOf("referral", StringComparison.OrdinalIgnoreCase) >= 0) ||
                   id.IndexOf("ActiveReferral", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   ElementTextContains(table, "Active Referrals");
        }

        private static bool ElementTextContains(OpenQA.Selenium.IWebElement element, string expectedValue)
        {
            var text = element.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text) && text.IndexOf(expectedValue, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            var value = element.GetAttribute("value")?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value) && value.IndexOf(expectedValue, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private System.Collections.Generic.List<OpenQA.Selenium.IWebElement> GetTableRows(OpenQA.Selenium.IWebElement table)
        {
            return table.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr"))
                .Where(row => row.Displayed && 
                    !row.Text.Contains("No data available in table", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private OpenQA.Selenium.IWebElement FindDeleteButton(OpenQA.Selenium.IWebElement tableRow)
        {
            return tableRow.FindElements(OpenQA.Selenium.By.CssSelector("a.btn.btn-danger"))
                .FirstOrDefault(el => el.Displayed && el.Enabled &&
                    (el.GetAttribute("id")?.Contains("btnDeleteReferral", StringComparison.OrdinalIgnoreCase) == true ||
                     ElementTextContains(el, "Delete")))
                ?? throw new InvalidOperationException("Unable to locate the delete button within the referral row.");
        }

        private void ClickDeleteButton(IPookieWebDriver driver, OpenQA.Selenium.IWebElement deleteButton)
        {
            _output.WriteLine("Clicking delete button...");
            ((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true); window.scrollBy(0, -150);", deleteButton);
            System.Threading.Thread.Sleep(500);
            deleteButton.Click();
            System.Threading.Thread.Sleep(1000);
            _output.WriteLine("[PASS] Clicked delete button");
        }

        private OpenQA.Selenium.IWebElement WaitForDeleteConfirmationModal(IPookieWebDriver driver)
        {
            System.Threading.Thread.Sleep(500);
            var modal = driver.FindElements(OpenQA.Selenium.By.CssSelector(".dc-confirmation-modal.modal"))
                .FirstOrDefault(el => el.Displayed);
            
            if (modal != null)
            {
                _output.WriteLine("[PASS] Delete confirmation modal appeared");
                return modal;
            }
            
            throw new InvalidOperationException("Delete confirmation modal did not appear after clicking delete button.");
        }

        private OpenQA.Selenium.IWebElement FindNoButton(OpenQA.Selenium.IWebElement modal)
        {
            var button = modal.FindElements(OpenQA.Selenium.By.CssSelector("button.btn.btn-default[data-dismiss='modal']"))
                .FirstOrDefault(el => el.Displayed && (ElementTextContains(el, "No") || ElementTextContains(el, "return")));
            
            if (button != null)
            {
                _output.WriteLine("Found 'No, return' button");
                return button;
            }
            
            throw new InvalidOperationException("Unable to locate the 'No, return' button in the confirmation modal.");
        }

        private OpenQA.Selenium.IWebElement FindYesButton(OpenQA.Selenium.IWebElement modal)
        {
            var button = modal.FindElements(OpenQA.Selenium.By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el => el.Displayed && el.Enabled &&
                    (el.GetAttribute("id")?.Contains("lbConfirmDelete", StringComparison.OrdinalIgnoreCase) == true ||
                     ElementTextContains(el, "Yes") || ElementTextContains(el, "delete")));
            
            if (button != null)
            {
                _output.WriteLine("Found 'Yes, delete' button");
                return button;
            }
            
            throw new InvalidOperationException("Unable to locate the 'Yes, delete' button in the confirmation modal.");
        }

        private void VerifyModalClosed(IPookieWebDriver driver)
        {
            var modal = driver.FindElements(OpenQA.Selenium.By.CssSelector(".dc-confirmation-modal.modal"))
                .FirstOrDefault(el => el.Displayed);
            Assert.Null(modal);
            _output.WriteLine("[PASS] Modal closed");
        }

        private void VerifyRowCount(OpenQA.Selenium.IWebElement table, int expectedCount)
        {
            var rows = GetTableRows(table);
            _output.WriteLine("Verifying row count: Expected {0}, Actual {1}", expectedCount, rows.Count);
            Assert.Equal(expectedCount, rows.Count);
            _output.WriteLine("[PASS] Row count verified");
        }

        private void VerifyEmptyTableMessage(OpenQA.Selenium.IWebElement table)
        {
            // Look for the empty message cell with class "dataTables_empty" and colspan attribute
            var emptyCell = table.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr td.dataTables_empty[colspan]"))
                .FirstOrDefault(el => el.Displayed && 
                    el.Text?.Contains("No data available in table", StringComparison.OrdinalIgnoreCase) == true);
            
            if (emptyCell != null)
            {
                _output.WriteLine("Found empty table message: '{0}'", emptyCell.Text?.Trim());
                Assert.True(true, "Empty table message verified");
                _output.WriteLine("[PASS] Empty table message verified");
                return;
            }
            
            // Fallback: check for any td with colspan that contains the message
            var emptyCellFallback = table.FindElements(OpenQA.Selenium.By.CssSelector("tbody tr td[colspan]"))
                .FirstOrDefault(el => el.Displayed && 
                    el.Text?.Contains("No data available in table", StringComparison.OrdinalIgnoreCase) == true);
            
            if (emptyCellFallback != null)
            {
                _output.WriteLine("Found empty table message: '{0}'", emptyCellFallback.Text?.Trim());
                Assert.True(true, "Empty table message verified");
                _output.WriteLine("[PASS] Empty table message verified");
                return;
            }
            
            throw new InvalidOperationException("Expected 'No data available in table' message but it was not found.");
        }

        #endregion

        [Fact]
        public void ReferralsPage_DeleteReferral_CancelAndConfirmDelete()
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST: DELETE REFERRAL - CANCEL AND CONFIRM");
            _output.WriteLine("========================================");

            LoginAndNavigateToReferrals(driver);

            _output.WriteLine("\nFinding active referrals table...");
            var activeReferralsTable = FindActiveReferralsTable(driver);
            var tableRows = GetTableRows(activeReferralsTable);
            Assert.True(tableRows.Count > 0, "No referrals found in the active referrals table!");
            _output.WriteLine("Found {0} referral(s) in active referrals table", tableRows.Count);
            
            var targetRow = tableRows[0];
            var initialRowCount = tableRows.Count;

            _output.WriteLine("\n--- First Delete Attempt: Cancel ---");
            var deleteButton = FindDeleteButton(targetRow);
            ClickDeleteButton(driver, deleteButton);
            
            var confirmationModal = WaitForDeleteConfirmationModal(driver);
            Assert.True(confirmationModal.Displayed, "Confirmation modal should be visible");
            
            var modalTitle = confirmationModal.FindElements(OpenQA.Selenium.By.CssSelector(".modal-title")).FirstOrDefault();
            Assert.NotNull(modalTitle);
            Assert.Contains("Delete Confirmation", modalTitle.Text, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("Modal title verified: '{0}'", modalTitle.Text?.Trim());
            
            _output.WriteLine("Clicking 'No, return' button...");
            var noButton = FindNoButton(confirmationModal);
            noButton.Click();
            driver.WaitForReady(10);
            System.Threading.Thread.Sleep(1000);
            _output.WriteLine("[PASS] Clicked 'No, return' button");
            
            VerifyModalClosed(driver);
            activeReferralsTable = FindActiveReferralsTable(driver);
            VerifyRowCount(activeReferralsTable, initialRowCount);
            _output.WriteLine("[PASS] Referral was NOT deleted (cancellation worked)");

            _output.WriteLine("\n--- Second Delete Attempt: Confirm ---");
            tableRows = GetTableRows(activeReferralsTable);
            targetRow = tableRows[0];
            deleteButton = FindDeleteButton(targetRow);
            ClickDeleteButton(driver, deleteButton);
            
            confirmationModal = WaitForDeleteConfirmationModal(driver);
            Assert.True(confirmationModal.Displayed, "Confirmation modal should be visible");
            
            _output.WriteLine("Clicking 'Yes, delete' button...");
            var yesButton = FindYesButton(confirmationModal);
            yesButton.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);
            _output.WriteLine("[PASS] Clicked 'Yes, delete' button");
            
            VerifyModalClosed(driver);
            activeReferralsTable = FindActiveReferralsTable(driver);
            
            // If only one referral existed and was deleted, verify empty table message
            if (initialRowCount == 1)
            {
                _output.WriteLine("Only one referral existed - verifying empty table message...");
                VerifyEmptyTableMessage(activeReferralsTable);
                _output.WriteLine("[PASS] Referral was successfully deleted - table is now empty");
            }
            else
            {
                VerifyRowCount(activeReferralsTable, initialRowCount - 1);
                _output.WriteLine("[PASS] Referral was successfully deleted");
            }

            _output.WriteLine("\n========================================");
            _output.WriteLine("TEST COMPLETED SUCCESSFULLY");
            _output.WriteLine("========================================");
        }
    }
}

