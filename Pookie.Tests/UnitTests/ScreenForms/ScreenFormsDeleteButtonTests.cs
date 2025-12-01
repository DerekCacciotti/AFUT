using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.ScreenForms
{
    public class ScreenFormsDeleteButtonTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public ScreenFormsDeleteButtonTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void ReferralsWaitingScreen_ClickDelete_ConfirmsRemoval()
        {
            using var driver = _driverFactory.CreateDriver();

            LoginAndNavigateToReferrals(driver);

            var waitingTable = FindReferralsWaitingScreenTable(driver);
            var rows = GetReferralRows(waitingTable);
            Assert.NotEmpty(rows);

            var initialRowCount = rows.Count;
            var targetRow = rows.First();
            var rowSignature = CaptureRowSignature(targetRow);

            _output.WriteLine($"[INFO] Initial waiting rows: {initialRowCount}");
            _output.WriteLine($"[INFO] Target row signature: '{rowSignature}'");

            var deleteLink = FindDeleteLink(targetRow);
            ClickElement(driver, deleteLink, "delete link");

            var confirmationModal = WaitForDeleteConfirmationModal(driver);
            Assert.Contains("Delete Confirmation", confirmationModal.Text, StringComparison.OrdinalIgnoreCase);

            var confirmButton = confirmationModal
                .FindElements(By.CssSelector(".modal-footer a.btn.btn-primary"))
                .FirstOrDefault(el => el.Displayed && el.Enabled &&
                                      ((el.Text ?? string.Empty).Contains("Yes", StringComparison.OrdinalIgnoreCase) ||
                                       (el.Text ?? string.Empty).Contains("delete", StringComparison.OrdinalIgnoreCase)));

            Assert.NotNull(confirmButton);
            _output.WriteLine("[INFO] Confirm delete button located");

            ClickElement(driver, confirmButton!, "confirm delete button");
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(1500);

            WaitForModalToClose(driver);
            var result = WaitForDeleteResult(driver, initialRowCount, rowSignature);

            switch (result)
            {
                case DeleteVerificationResult.RowRemoved:
                    _output.WriteLine("[PASS] Referral row removed from waiting table.");
                    break;
                case DeleteVerificationResult.TableNowEmpty:
                    _output.WriteLine("[PASS] Referral deleted and waiting table now shows empty state.");
                    break;
                case DeleteVerificationResult.WaitingSectionHidden:
                    _output.WriteLine("[PASS] Referral deleted and waiting section removed (only row).");
                    break;
                default:
                    throw new InvalidOperationException("Unexpected delete verification result.");
            }
        }

        private void LoginAndNavigateToReferrals(IPookieWebDriver driver)
        {
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
            Assert.True(landingPage.IsLoaded, "Landing page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Selected DataEntry role");

            var referralsLink = driver.FindElements(By.CssSelector(".navbar a, nav a"))
                .FirstOrDefault(link => link.GetAttribute("href")?.Contains("Referrals.aspx", StringComparison.OrdinalIgnoreCase) == true);

            Assert.NotNull(referralsLink);

            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", referralsLink);
            System.Threading.Thread.Sleep(150);

            referralsLink.Click();
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(2000);

            _output.WriteLine("[PASS] Navigated to Referrals page");
        }

        private IWebElement FindReferralsWaitingScreenTable(IPookieWebDriver driver)
        {
            var table = TryFindReferralsWaitingScreenTable(driver, requireDisplayed: true);
            if (table != null)
            {
                return table;
            }

            throw new InvalidOperationException("Unable to locate the Referrals Waiting Screen table.");
        }

        private IWebElement? TryFindReferralsWaitingScreenTable(IPookieWebDriver driver, bool requireDisplayed)
        {
            var selectors = new[]
            {
                "table[id*='grReferralsWaitingScreen']",
                ".table.table-condensed.table-responsive.dataTable",
                ".panel table.table"
            };

            foreach (var selector in selectors)
            {
                var table = driver.FindElements(By.CssSelector(selector))
                    .FirstOrDefault(el =>
                        (!requireDisplayed || el.Displayed) &&
                        LooksLikeWaitingScreenTable(el));

                if (table != null)
                {
                    return table;
                }
            }

            return null;
        }

        private static bool LooksLikeWaitingScreenTable(IWebElement element)
        {
            var id = element.GetAttribute("id") ?? string.Empty;
            var classes = element.GetAttribute("class") ?? string.Empty;
            var text = element.Text ?? string.Empty;

            return id.Contains("WaitingScreen", StringComparison.OrdinalIgnoreCase) ||
                   classes.Contains("Waiting", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Create Screen Form", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Referrals Waiting", StringComparison.OrdinalIgnoreCase);
        }

        private static System.Collections.Generic.List<IWebElement> GetReferralRows(IWebElement table)
        {
            return table.FindElements(By.CssSelector("tbody tr"))
                .Where(row => row.Displayed && !row.Text.Contains("No data available", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static string CaptureRowSignature(IWebElement row)
        {
            var cells = row.FindElements(By.CssSelector("th, td"))
                .Select(cell => cell.Text?.Trim() ?? string.Empty)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();

            return cells.Any() ? string.Join(" | ", cells) : row.Text?.Trim() ?? string.Empty;
        }

        private static IWebElement FindDeleteLink(IWebElement row)
        {
            var deleteLink = row.FindElements(By.CssSelector("a.btn.btn-danger, .delete-control a.btn.btn-danger"))
                .FirstOrDefault(link =>
                {
                    if (!link.Displayed || !link.Enabled)
                    {
                        return false;
                    }

                    var id = link.GetAttribute("id") ?? string.Empty;
                    var text = link.Text?.Trim() ?? string.Empty;
                    return id.Contains("btnDeleteReferral", StringComparison.OrdinalIgnoreCase) ||
                           text.Contains("Delete", StringComparison.OrdinalIgnoreCase);
                });

            return deleteLink ?? throw new InvalidOperationException("Unable to locate the delete link in the waiting row.");
        }

        private void ClickElement(IPookieWebDriver driver, IWebElement element, string description)
        {
            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -120);", element);
            System.Threading.Thread.Sleep(200);

            try
            {
                element.Click();
            }
            catch (ElementClickInterceptedException)
            {
                js.ExecuteScript("arguments[0].click();", element);
            }

            System.Threading.Thread.Sleep(500);
            driver.WaitForReady(5);
            System.Threading.Thread.Sleep(250);
            _output.WriteLine($"[INFO] Clicked {description}");
        }

        private IWebElement WaitForDeleteConfirmationModal(IPookieWebDriver driver)
        {
            var endTime = DateTime.Now.AddSeconds(10);
            while (DateTime.Now < endTime)
            {
                var modal = driver.FindElements(By.CssSelector(".dc-confirmation-modal.modal"))
                    .FirstOrDefault(el => el.Displayed);

                if (modal != null)
                {
                    _output.WriteLine("[PASS] Delete confirmation modal appeared");
                    return modal;
                }

                System.Threading.Thread.Sleep(200);
            }

            throw new TimeoutException("Timed out waiting for delete confirmation modal.");
        }

        private void WaitForModalToClose(IPookieWebDriver driver)
        {
            var endTime = DateTime.Now.AddSeconds(10);
            while (DateTime.Now < endTime)
            {
                var modal = driver.FindElements(By.CssSelector(".dc-confirmation-modal.modal"))
                    .FirstOrDefault(el => el.Displayed);

                if (modal == null)
                {
                    _output.WriteLine("[INFO] Confirmation modal closed");
                    return;
                }

                System.Threading.Thread.Sleep(200);
            }

            throw new TimeoutException("Delete confirmation modal did not close in time.");
        }

        private DeleteVerificationResult WaitForDeleteResult(IPookieWebDriver driver, int initialRowCount, string removedSignature)
        {
            var endTime = DateTime.Now.AddSeconds(20);
            while (DateTime.Now < endTime)
            {
                var table = TryFindReferralsWaitingScreenTable(driver, requireDisplayed: false);
                if (table == null)
                {
                    return DeleteVerificationResult.WaitingSectionHidden;
                }

                var rows = GetReferralRows(table);
                if (rows.Count == 0)
                {
                    if (initialRowCount == 1)
                    {
                        return DeleteVerificationResult.TableNowEmpty;
                    }
                }

                var match = rows.FirstOrDefault(row =>
                {
                    try
                    {
                        return string.Equals(CaptureRowSignature(row), removedSignature, StringComparison.OrdinalIgnoreCase);
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
                });

                if (match == null)
                {
                    if (rows.Count <= Math.Max(0, initialRowCount - 1))
                    {
                        return rows.Count == 0
                            ? DeleteVerificationResult.TableNowEmpty
                            : DeleteVerificationResult.RowRemoved;
                    }
                }

                System.Threading.Thread.Sleep(400);
                driver.WaitForReady(2);
            }

            throw new TimeoutException("Timed out waiting for referral row deletion.");
        }

        private enum DeleteVerificationResult
        {
            RowRemoved,
            TableNowEmpty,
            WaitingSectionHidden
        }
    }
}

