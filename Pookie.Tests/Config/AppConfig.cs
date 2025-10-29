using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public ServiceProvider ServiceProvider { get; }

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
        }

        public void Dispose()
        {
        }
    }
}