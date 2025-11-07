using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace AFUT.Tests.UnitTests.CaseHome.CaseFilters
{
    public class CaseFiltersTabTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;

        public CaseFiltersTabTests(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void CaseFiltersTabDisplaysExpectedFilters()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = CaseHomeTestHelper.NavigateToCaseHome(driver, _config);

            var tabs = caseHomePage.GetTabs();
            var caseFiltersTab = tabs.FirstOrDefault(tab =>
                string.Equals(tab.DisplayName, "Case Filters", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(caseFiltersTab);

            caseFiltersTab!.Activate();

            var filters = caseHomePage.GetCaseFilters();

            Assert.NotEmpty(filters);
            Assert.All(filters, filter =>
            {
                Assert.False(string.IsNullOrWhiteSpace(filter.Name));
                Assert.False(string.IsNullOrWhiteSpace(filter.Value), $"Filter '{filter.Name}' did not provide a value.");
            });

            Assert.Contains(filters, filter =>
                string.Equals(filter.Name, "Child Welfare Protocol", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(filter.Value, "(Missing)", StringComparison.OrdinalIgnoreCase));

            Assert.Contains(filters, filter =>
                string.Equals(filter.Value, "No", StringComparison.OrdinalIgnoreCase));
        }
    }
}

