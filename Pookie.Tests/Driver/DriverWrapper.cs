using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.DevTools.V104.Storage;
using System.Threading;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AFUT.Tests.Driver
{
    internal class DriverWrapper : ChromiumDriver, IPookieWebDriver
    {
        private static Dictionary<string, CommandInfo> chromeCustomCommands = new Dictionary<string, CommandInfo>()
        {
            { ExecuteCdp, new HttpCommandInfo(HttpCommandInfo.PostCommand, "/session/{sessionId}/goog/cdp/execute") },
            { GetCastSinksCommand, new HttpCommandInfo(HttpCommandInfo.GetCommand, "/session/{sessionId}/goog/cast/get_sinks") },
            { SelectCastSinkCommand, new HttpCommandInfo(HttpCommandInfo.PostCommand, "/session/{sessionId}/goog/cast/set_sink_to_use") },
            { StartCastTabMirroringCommand, new HttpCommandInfo(HttpCommandInfo.PostCommand, "/session/{sessionId}/goog/cast/start_tab_mirroring") },
            { StartCastDesktopMirroringCommand, new HttpCommandInfo(HttpCommandInfo.PostCommand, "/session/{sessionId}/goog/cast/start_desktop_mirroring") },
            { GetCastIssueMessageCommand, new HttpCommandInfo(HttpCommandInfo.GetCommand, "/session/{sessionId}/goog/cast/get_issue_message") },
            { StopCastingCommand, new HttpCommandInfo(HttpCommandInfo.PostCommand, "/session/{sessionId}/goog/cast/stop_casting") }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeDriver"/> class.
        /// </summary>
        public DriverWrapper()
            : this(new ChromeOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeDriver"/> class using the specified options.
        /// </summary>
        /// <param name="options">The <see cref="ChromeOptions"/> to be used with the Chrome driver.</param>
        public DriverWrapper(ChromeOptions options)
            : this(ChromeDriverService.CreateDefaultService(), options, RemoteWebDriver.DefaultCommandTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeDriver"/> class using the specified driver service.
        /// </summary>
        /// <param name="service">The <see cref="ChromeDriverService"/> used to initialize the driver.</param>
        public DriverWrapper(ChromeDriverService service)
            : this(service, new ChromeOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeDriver"/> class using the specified path
        /// to the directory containing ChromeDriver.exe.
        /// </summary>
        /// <param name="chromeDriverDirectory">The full path to the directory containing ChromeDriver.exe.</param>
        public DriverWrapper(string chromeDriverDirectory)
            : this(chromeDriverDirectory, new ChromeOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeDriver"/> class using the specified path
        /// to the directory containing ChromeDriver.exe and options.
        /// </summary>
        /// <param name="chromeDriverDirectory">The full path to the directory containing ChromeDriver.exe.</param>
        /// <param name="options">The <see cref="ChromeOptions"/> to be used with the Chrome driver.</param>
        public DriverWrapper(string chromeDriverDirectory, ChromeOptions options)
            : this(chromeDriverDirectory, options, RemoteWebDriver.DefaultCommandTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeDriver"/> class using the specified path
        /// to the directory containing ChromeDriver.exe, options, and command timeout.
        /// </summary>
        /// <param name="chromeDriverDirectory">The full path to the directory containing ChromeDriver.exe.</param>
        /// <param name="options">The <see cref="ChromeOptions"/> to be used with the Chrome driver.</param>
        /// <param name="commandTimeout">The maximum amount of time to wait for each command.</param>
        public DriverWrapper(string chromeDriverDirectory, ChromeOptions options, TimeSpan commandTimeout)
            : this(ChromeDriverService.CreateDefaultService(chromeDriverDirectory), options, commandTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeDriver"/> class using the specified
        /// <see cref="ChromeDriverService"/> and options.
        /// </summary>
        /// <param name="service">The <see cref="ChromeDriverService"/> to use.</param>
        /// <param name="options">The <see cref="ChromeOptions"/> used to initialize the driver.</param>
        public DriverWrapper(ChromeDriverService service, ChromeOptions options)
            : this(service, options, RemoteWebDriver.DefaultCommandTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromeDriver"/> class using the specified <see cref="ChromeDriverService"/>.
        /// </summary>
        /// <param name="service">The <see cref="ChromeDriverService"/> to use.</param>
        /// <param name="options">The <see cref="ChromeOptions"/> to be used with the Chrome driver.</param>
        /// <param name="commandTimeout">The maximum amount of time to wait for each command.</param>
        public DriverWrapper(ChromeDriverService service, ChromeOptions options, TimeSpan commandTimeout)
            : base(service, options, commandTimeout)
        {
            this.AddCustomChromeCommands();
        }

        /// <summary>
        /// Gets a read-only dictionary of the custom WebDriver commands defined for ChromeDriver.
        /// The keys of the dictionary are the names assigned to the command; the values are the
        /// <see cref="CommandInfo"/> objects describing the command behavior.
        /// </summary>
        public static IReadOnlyDictionary<string, CommandInfo> CustomCommandDefinitions
        {
            get
            {
                Dictionary<string, CommandInfo> customCommands = new Dictionary<string, CommandInfo>();
                foreach (KeyValuePair<string, CommandInfo> entry in ChromiumCustomCommands)
                {
                    customCommands[entry.Key] = entry.Value;
                }

                foreach (KeyValuePair<string, CommandInfo> entry in chromeCustomCommands)
                {
                    customCommands[entry.Key] = entry.Value;
                }

                return new ReadOnlyDictionary<string, CommandInfo>(customCommands);
            }
        }

        private volatile int requestCount = 0;
        public bool IsDriving => Interlocked.CompareExchange(ref requestCount, 0, 0) > 0;

        private void AddCustomChromeCommands()
        {
            foreach (KeyValuePair<string, CommandInfo> entry in CustomCommandDefinitions)
            {
                this.RegisterInternalDriverCommand(entry.Key, entry.Value);
            }
        }

        public void GotoUrl(string url)
        {
            Navigate().GoToUrl(url);
        }

        public void Login(string username, string password, string url)
        {
            if (url.StartsWith("http")) throw new ArgumentException("Url must be Https");

            var network = Manage().Network;
            network.ClearAuthenticationHandlers();
            network.AddAuthenticationHandler(new NetworkAuthenticationHandler()
            {
                UriMatcher = requestUrl =>
                {
                    return new Uri(url).DnsSafeHost == requestUrl.DnsSafeHost;
                },
                Credentials = new PasswordCredentials(username, password)
            });
        }

        public byte[] CaptureScreen()
        {
            try
            {
                return ((ITakesScreenshot)this).GetScreenshot().AsByteArray;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error capturing screen: {e.Message}");
                return null;
            }
        }
    }
}