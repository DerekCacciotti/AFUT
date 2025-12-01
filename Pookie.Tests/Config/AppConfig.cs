using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AFUT.Tests.Driver;

namespace AFUT.Tests.Config
{
    public class AppConfig : IDisposable, IAppConfig
    {
        private readonly IConfiguration _config;
        public string AppUrl => _config["AppUrl"];

        public string UserName => _config["UserName"];

        public string Password => _config["Password"];
        
        public string TestPc1Id => _config["TestPc1Id"] ?? "EC01001408989";
        
        public IReadOnlyList<string> TestPc1Ids => _config.GetSection("TestPc1Ids").Get<string[]>() ?? new[] { "EC01001408989" };
        
        public ServiceProvider ServiceProvider { get; }
        public IReadOnlyList<string> CaseHomeTabs { get; }

        public AppConfig()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets(typeof(AppConfig).Assembly)
                .AddEnvironmentVariables(prefix: "POOKIE_")
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IPookieDriverFactory, PookieDriverFactory>();
            services.AddSingleton<IAppConfig>(_ => this);
            this.ServiceProvider = services.BuildServiceProvider();

            CaseHomeTabs = LoadCaseHomeTabs();
        }

        public void Dispose()
        {
        }

        private IReadOnlyList<string> LoadCaseHomeTabs()
        {
            var configuredTabs = _config.GetSection("CaseHomeTabs").Get<string[]>();

            if (configuredTabs is null || configuredTabs.Length == 0)
            {
                return Array.Empty<string>();
            }

            var sanitized = configuredTabs
                .Select(tab => tab?.Trim())
                .Where(tab => !string.IsNullOrWhiteSpace(tab))
                .Select(tab => tab!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return sanitized.Length == 0
                ? Array.Empty<string>()
                : Array.AsReadOnly(sanitized);
        }
    }
}