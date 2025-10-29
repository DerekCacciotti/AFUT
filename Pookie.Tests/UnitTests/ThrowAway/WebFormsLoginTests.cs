using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using Microsoft.Extensions.DependencyInjection;

namespace AFUT.Tests.UnitTests.ThrowAway
{
    public class WebFormsLoginTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig config;
        private readonly IPookieDriverFactory driverFactory;

        public WebFormsLoginTests(AppConfig config)
        {
            this.config = config;
            driverFactory = config.ServiceProvider.GetRequiredService<IPookieDriverFactory>();
        }

        [Fact]
        public void Login_Allows_User_To_Access_App()
        {
            using var driver = driverFactory.CreateDriver();
            var routine = new Routine.ThrowAway.WebFormsLogin(driver, config);
            var parameters = Routine.ThrowAway.WebFormsLogin.GetParameters();

            routine.Navigate(parameters);
            routine.SignIn(parameters);

            Assert.True(parameters.SignedIn);
        }
    }
}

