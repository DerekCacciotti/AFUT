using OpenQA.Selenium.Chrome;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.IO.Compression;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using AFUT.Tests.Config;
using System.Text.Json;

namespace AFUT.Tests.Driver
{
    public class PookieDriverFactory : IPookieDriverFactory
    {
        private const string ChromeInstallPath = "c:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
        private const string ChromeDriverMetadataUrl = "https://googlechromelabs.github.io/chrome-for-testing/latest-versions-per-milestone-with-downloads.json";
        private const string ChromeDriverPlatformWin64 = "win64";
        private const string ChromeDriverPlatformWin32 = "win32";
        private const string ChromeDriverDirectory = "ChromeDriver";

        private static readonly string[] ChromeOptions = new[]
        {
            "--incognito",
        };

        private static readonly string[] ChromeDriverOptions = new[]
            {
                "--headless",
                "--auth-server-allowlist=\"_\"",
                 "--auth-server-whitelist=\"_\"",
            };

        private static readonly string[] MiscChromeOptions = new[]
        {
            "--disable-extensions",
            "--disable-infobars",
            "--window-size=1280,960",
            "--disable-gpu",
            "--use-gl=\"\""
        };

        private static ChromeDriverService? ChromeDriverService;
        private static readonly HttpClient HttpClient = new();

        private static async Task<ChromeDriverService> GetChormeDriverServiceAsync()
        {
            if (ChromeDriverService is not null)
            {
                return ChromeDriverService;
            }

            var productVersion = FileVersionInfo.GetVersionInfo(ChromeInstallPath).ProductVersion;
            var milestone = productVersion?.Split('.')[0] ?? throw new InvalidOperationException("Unable to determine installed Chrome version.");
            var driverLocation = await DownloadChromeDriverAsync(milestone);
            ChromeDriverService = ChromeDriverService.CreateDefaultService(driverLocation);
            ChromeDriverService.Start();
            return ChromeDriverService;
        }

        private readonly bool hasCredentails;

        public PookieDriverFactory(IAppConfig config)
        {
            hasCredentails = !string.IsNullOrEmpty(config.UserName);
        }

        public IPookieWebDriver CreateDriver()
        {
            var options = new ChromeOptions();
            options.AddArguments(MiscChromeOptions);

            var driver = Task.Run(GetChormeDriverServiceAsync).GetAwaiter().GetResult();
            return new DriverWrapper(driver, options);
        }


        private static async Task<string> DownloadChromeDriverAsync(string milestone)
        {
            var (driverVersion, downloadUrl) = await ResolveChromeDriverDownloadAsync(milestone);
            var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location ?? Assembly.GetExecutingAssembly().Location;
            var baseLocation = Path.GetDirectoryName(entryAssemblyLocation) ?? throw new InvalidOperationException("Unable to determine assembly directory.");
            var location = Path.Combine(baseLocation, ChromeDriverDirectory, driverVersion);

            if (HasChromeDriver(location))
            {
                return location;
            }

            Directory.CreateDirectory(location);

            using var driverResponse = await HttpClient.GetStreamAsync(downloadUrl);
            using var zip = new ZipArchive(driverResponse, ZipArchiveMode.Read);
            var driverEntry = zip.Entries.FirstOrDefault(entry => entry.Name.Equals("chromedriver.exe", StringComparison.OrdinalIgnoreCase));

            if (driverEntry is null)
            {
                throw new InvalidDataException("Downloaded ChromeDriver archive did not contain chromedriver.exe");
            }

            var driverlocation = Path.Combine(location, driverEntry.Name);

            await using var file = File.Create(driverlocation);
            await using var entryStream = driverEntry.Open();
            await entryStream.CopyToAsync(file);

            CleanUpOldChromeDrivers();

            return location;
        }

        private static async Task<(string Version, string Url)> ResolveChromeDriverDownloadAsync(string milestone)
        {
            var response = await HttpClient.GetStringAsync(ChromeDriverMetadataUrl);
            using var document = JsonDocument.Parse(response);

            if (!document.RootElement.TryGetProperty("milestones", out var milestones) ||
                !milestones.TryGetProperty(milestone, out var milestoneElement))
            {
                throw new InvalidDataException($"Unable to find ChromeDriver metadata for milestone {milestone}.");
            }

            var version = milestoneElement.GetProperty("version").GetString() ?? milestone;
            var downloads = milestoneElement.GetProperty("downloads").GetProperty("chromedriver");

            string? url = null;
            foreach (var entry in downloads.EnumerateArray())
            {
                var platform = entry.GetProperty("platform").GetString();
                if (string.Equals(platform, ChromeDriverPlatformWin64, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(platform, ChromeDriverPlatformWin32, StringComparison.OrdinalIgnoreCase))
                {
                    url = entry.GetProperty("url").GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidDataException($"Unable to locate a ChromeDriver download url for milestone {milestone}.");
            }

            return (version, url);
        }


        private static void CleanUpOldChromeDrivers()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                return;
            }

            var location = Path.Combine(assemblyLocation, ChromeDriverDirectory);

            var driverDirectory = new DirectoryInfo(location);
            var tobeRemoved = driverDirectory.GetDirectories()
                .OrderByDescending(f => f.CreationTime)
                .Skip(3); // lets keep the last three version of the driver

            foreach (var driver in tobeRemoved)
            {
                driver.Delete(true);
            }
        }

        private static bool HasChromeDriver(string location)
        {
            if (Directory.Exists(location) && Directory.GetFiles(location, "chromedriver.exe").Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}