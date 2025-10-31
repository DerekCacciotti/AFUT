using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AFUT.Tests.UnitTests.ThrowAway
{
    public class SelectRoleFixture : IAsyncLifetime, IDisposable
    {
        private readonly AppConfig config;
        private readonly IPookieDriverFactory driverFactory;

        public IPookieWebDriver Driver { get; private set; }
        public SelectRolePage SelectRolePage { get; private set; }

        public SelectRoleFixture()
        {
            config = new AppConfig();
            driverFactory = config.ServiceProvider.GetRequiredService<IPookieDriverFactory>();
        }

        public async Task InitializeAsync()
        {
            Driver = driverFactory.CreateDriver();

            Driver.Navigate().GoToUrl(config.AppUrl);
            Driver.WaitForReady(30);

            EnsureSignedIn();

            await Task.CompletedTask;

            NavigateToSelectRolePage(allowMissing: true);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public SelectRolePage RefreshSelectRolePage()
        {
            return NavigateToSelectRolePage();
        }

        public IAppLandingPage EnsureLandingPage(string? programName = null, string? roleName = null)
        {
            var selectRolePage = NavigateToSelectRolePage(allowMissing: true);

            if (selectRolePage is null)
            {
                return DetectLandingPage();
            }

            if (!string.IsNullOrWhiteSpace(programName) && !string.IsNullOrWhiteSpace(roleName))
            {
                return selectRolePage.SelectRole(programName, roleName);
            }

            return SelectFirstAvailableRole(selectRolePage);
        }

        private SelectRolePage? NavigateToSelectRolePage(bool allowMissing = false)
        {
            var selectRoleUri = new Uri(new Uri(config.AppUrl), "Pages/SelectProgram.aspx");
            Driver.Navigate().GoToUrl(selectRoleUri);
            Driver.WaitForReady(30);

            try
            {
                SelectRolePage = new SelectRolePage(Driver);
                return SelectRolePage;
            }
            catch (InvalidOperationException)
            {
                if (allowMissing)
                {
                    SelectRolePage = null;
                    return null;
                }

                throw;
            }
        }

        private void EnsureSignedIn()
        {
            if (!IsLoginFormVisible())
            {
                return;
            }

            var loginPage = new LoginPage(Driver);

            if (!string.IsNullOrWhiteSpace(config.UserName))
            {
                loginPage.SignIn(config.UserName, config.Password);
                return;
            }

            Console.WriteLine("[SelectRoleFixture] Manual login required. Please authenticate in the browser window.");
            WaitUntil(() => loginPage.IsSignedIn(5), TimeSpan.FromMinutes(5), "Timed out waiting for manual login.");
        }

        private bool IsLoginFormVisible()
        {
            return Driver.FindElements(OpenQA.Selenium.By.Id("Login1_LoginButton")).Any(element => element.Displayed);
        }

        private IAppLandingPage DetectLandingPage()
        {
            var exceptions = new List<Exception>();

            try
            {
                return new HomePage(Driver);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                return new AdminHomePage(Driver);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            throw new AggregateException("Unable to determine landing page after role selection.", exceptions);
        }

        private IAppLandingPage SelectFirstAvailableRole(SelectRolePage selectRolePage)
        {
            try
            {
                return selectRolePage.SelectFirstAvailableRole();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SelectRoleFixture] Automatic role selection failed: " + ex.Message);
                Console.WriteLine("[SelectRoleFixture] Please select a role manually.");
                return WaitForLandingPageAfterManualSelection();
            }
        }

        private IAppLandingPage WaitForLandingPageAfterManualSelection()
        {
            var timeout = DateTime.UtcNow.AddMinutes(5);

            while (DateTime.UtcNow <= timeout)
            {
                try
                {
                    return DetectLandingPage();
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }

            throw new TimeoutException("Timed out waiting for landing page after manual role selection.");
        }

        private void WaitUntil(Func<bool> condition, TimeSpan timeout, string failureMessage)
        {
            var end = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow <= end)
            {
                if (condition())
                {
                    return;
                }

                Thread.Sleep(500);
            }

            throw new TimeoutException(failureMessage);
        }

        public void Dispose()
        {
            SelectRolePage = null;
            Driver?.Dispose();
            config.Dispose();
        }
    }

    [CollectionDefinition("Select Role Collection")]
    public class SelectRoleCollection : ICollectionFixture<SelectRoleFixture>
    {
    }
}

