using System;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AFUT.Tests.UnitTests.CaseHome
{
    public class CaseHomeTabsTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;

        public CaseHomeTabsTests(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void SelectingEachTabDisplaysCorrespondingContent()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = CaseHomeTestHelper.NavigateToCaseHome(driver, _config);

            Assert.NotNull(caseHomePage);
            Assert.True(caseHomePage.IsLoaded, "Case home page did not load successfully.");

            var tabs = caseHomePage.GetTabs();

            Assert.Equal(CaseHomePage.DefaultTabDisplayNames.Count, tabs.Count);

            var tabsByName = tabs.ToDictionary(tab => tab.DisplayName, tab => tab, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < CaseHomePage.DefaultTabDisplayNames.Count; i++)
            {
                var expectedDisplayName = CaseHomePage.DefaultTabDisplayNames[i];

                Assert.True(tabsByName.TryGetValue(expectedDisplayName, out var tab),
                    $"Expected to find tab '{expectedDisplayName}' on the case home page.");

                Assert.Equal(expectedDisplayName, tabs[i].DisplayName);

                tab.Activate();

                Assert.True(tab.IsActive, $"Tab '{tab.DisplayName}' was not active after selection.");
                Assert.True(tab.IsContentDisplayed, $"Content for tab '{tab.DisplayName}' was not visible after selection.");
            }
        }

    }
}

