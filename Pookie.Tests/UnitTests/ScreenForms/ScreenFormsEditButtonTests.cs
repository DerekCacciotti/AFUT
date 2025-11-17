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
    public class ScreenFormsEditButtonTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public ScreenFormsEditButtonTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void ReferralsWaitingScreen_ClickEditLink_NavigatesToReferralDetails()
        {
            using var driver = _driverFactory.CreateDriver();

            LoginAndNavigateToReferrals(driver);

            var editLink = FindVisibleEditReferralLink(driver);
            var editHref = editLink.GetAttribute("href") ?? string.Empty;

            Assert.False(string.IsNullOrWhiteSpace(editHref), "Edit link did not contain an href attribute.");
            _output.WriteLine($"[INFO] Edit link href: {editHref}");

            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -120);", editLink);
            System.Threading.Thread.Sleep(200);

            try
            {
                editLink.Click();
            }
            catch (ElementClickInterceptedException)
            {
                _output.WriteLine("[WARN] Edit link click intercepted, invoking JavaScript click");
                js.ExecuteScript("arguments[0].click();", editLink);
            }

            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(1000);

            var currentUrl = driver.Url;
            var normalizedHref = NormalizeHref(driver.Url, editHref);
            var expectedPath = ExtractPath(normalizedHref);
            var referralPkFragment = ExtractReferralPkFragment(normalizedHref);

            _output.WriteLine($"[INFO] Expected referral path: {expectedPath}");
            _output.WriteLine($"[INFO] Actual URL: {currentUrl}");

            Assert.Contains("Referral.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ReferralPK", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(expectedPath, currentUrl, StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(referralPkFragment))
            {
                Assert.Contains(referralPkFragment, currentUrl, StringComparison.OrdinalIgnoreCase);
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
            _output.WriteLine("[PASS] Successfully signed in");

            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");
            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded, "Landing page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully selected DataEntry role");

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

        private OpenQA.Selenium.IWebElement FindVisibleEditReferralLink(IPookieWebDriver driver)
        {
            var table = driver.FindElements(By.CssSelector("table[id*='grReferralsWaitingScreen']"))
                .FirstOrDefault(el => el.Displayed);

            if (table == null)
            {
                table = driver.FindElements(By.CssSelector(".table.table-responsive"))
                    .FirstOrDefault(el => el.Displayed &&
                                          (el.GetAttribute("id") ?? string.Empty)
                                              .Contains("WaitingScreen", StringComparison.OrdinalIgnoreCase));
            }

            if (table == null)
            {
                throw new InvalidOperationException("Unable to locate the Referrals Waiting Screen table.");
            }

            var editLink = table
                .FindElements(By.CssSelector("tbody tr a[id*='lnkEditReferral'], tbody tr a.btn.btn-default"))
                .FirstOrDefault(anchor =>
                {
                    if (!anchor.Displayed)
                    {
                        return false;
                    }

                    var id = anchor.GetAttribute("id") ?? string.Empty;
                    var text = anchor.Text?.Trim() ?? string.Empty;
                    var ariaLabel = anchor.GetAttribute("aria-label") ?? string.Empty;

                    return id.Contains("lnkEditReferral", StringComparison.OrdinalIgnoreCase) ||
                           text.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
                           ariaLabel.Contains("Edit", StringComparison.OrdinalIgnoreCase);
                });

            if (editLink == null)
            {
                throw new InvalidOperationException("Unable to locate a visible Edit link in the Referrals Waiting Screen table.");
            }

            _output.WriteLine($"[PASS] Found edit link with id '{editLink.GetAttribute("id")}'");
            return editLink;
        }

        private static string NormalizeHref(string currentUrl, string href)
        {
            if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
            {
                return absolute.ToString();
            }

            if (Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri))
            {
                return new Uri(currentUri, href).ToString();
            }

            return href;
        }

        private static string ExtractPath(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.GetLeftPart(UriPartial.Path);
            }

            var queryIndex = url.IndexOf('?');
            return queryIndex >= 0 ? url[..queryIndex] : url;
        }

        private static string ExtractReferralPkFragment(string url)
        {
            const string key = "ReferralPK=";
            var index = url.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return string.Empty;
            }

            var start = index;
            var end = url.IndexOf('&', index);
            return end > start ? url[start..end] : url[start..];
        }
    }
}

