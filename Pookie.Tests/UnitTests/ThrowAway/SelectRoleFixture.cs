using System;
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

            if (!string.IsNullOrWhiteSpace(config.UserName))
            {
                var loginPage = new LoginPage(Driver);
                loginPage.SignIn(config.UserName, config.Password);
            }

            await Task.CompletedTask;

            NavigateToSelectRolePage();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public SelectRolePage RefreshSelectRolePage()
        {
            return NavigateToSelectRolePage();
        }

        private SelectRolePage NavigateToSelectRolePage()
        {
            var selectRoleUri = new Uri(new Uri(config.AppUrl), "Pages/SelectProgram.aspx");
            Driver.Navigate().GoToUrl(selectRoleUri);
            Driver.WaitForReady(30);
            SelectRolePage = new SelectRolePage(Driver);
            return SelectRolePage;
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

