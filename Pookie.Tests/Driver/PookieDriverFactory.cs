using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.IO.Compression;
using OpenQA.Selenium.DevTools.V104.Network;
using Microsoft.Extensions.Configuration;
using AFUT.Tests.Config;
using System.Net.Http.Headers;

namespace AFUT.Tests.Driver
{
    public class PookieDriverFactory : IPookieDriverFactory
    {
        private const string ChromeInstallPath = "c:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
        private const string ChromeDownloadUrl = "https://chromedriver.storage.googleapis.com/";
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

        private static ChromeDriverService ChromeDriverService;

        private static async Task<ChromeDriverService> GetChormeDriverServiceAsync()
        {
            if (ChromeDriverService is not null)
            {
                return ChromeDriverService;
            }

            var chromeversion = FileVersionInfo.GetVersionInfo(ChromeInstallPath).ProductVersion[..3];
            var driverLocation = await DownloadChromeDriverAsync(chromeversion);
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

        private static async Task<string> DownloadChromeDriverAsync(string version)
        {
            var driverKey = "";
            var location = Assembly.GetEntryAssembly().Location;
            //var location = @"C:\temp\";
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, ChromeDriverDirectory, version);

            if (HasChromeDriver(location))
            {
                return location;
            }
            else
            {
                Directory.CreateDirectory(location);
            }
            using var clinet = new HttpClient();
            var response = await clinet.GetStringAsync(ChromeDownloadUrl);
            var xml = new XmlDocument();
            xml.LoadXml(response.Replace("xmlns='http://doc.s3.amazon.com/2006-03-01'", string.Empty));
            //var driverKeyData = xml.ChildNodes[1]
            //    .OfType<XmlElement>()
            //    .Select(x => x.InnerText)
            //    .Where(x => x.StartsWith(version))
            //    .Where(x => x.Contains("win32"))
            //    .OrderByDescending(x => x)
            //    .First().Split('/');
            //var keyparts = driverKeyData[1].Split(@"\");
            //var driverKey = driverKeyData[0];
            XmlNodeList nodes = xml.DocumentElement.ChildNodes;
            foreach (XmlNode node in nodes)
            {
                if (node.InnerText.StartsWith(version) && node.InnerText.Contains("win32"))
                {
                    driverKey = node.ChildNodes.OfType<XmlElement>().First().InnerText;
                    break;
                }
            }

            using var driverResponse = await clinet.GetStreamAsync($"{ChromeDownloadUrl}{driverKey}");
            using var zip = new ZipArchive(driverResponse, ZipArchiveMode.Read);
            var driver = zip.Entries.First();

            var driverlocation = Path.Combine(location, driver.Name);

            using var file = File.Create(driverlocation);
            await driver.Open().CopyToAsync(file);

            CleanUpOldChromeDrivers();

            return location;
        }

        private static void CleanUpOldChromeDrivers()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, ChromeDriverDirectory);

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