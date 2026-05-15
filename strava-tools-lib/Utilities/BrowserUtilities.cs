using PuppeteerSharp;
using PuppeteerSharp.BrowserData;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StravaTools.Utilities.Browser
{
    public class BrowserUtilities
    {
        public static async Task<IBrowser> LaunchOrConnect(BrowserTag browser_tag = BrowserTag.Dev, SupportedBrowser supported_browser = SupportedBrowser.Chrome, bool headless = true, bool full_viewport = false)
        {
            IBrowser browser = null;

            string singleFilePublishFilePathForBrowserExecutables = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PuppeteerSharp");
            DirectoryInfo cachedir_di = new DirectoryInfo(singleFilePublishFilePathForBrowserExecutables);
            if (!cachedir_di.Exists)
            {
                cachedir_di.Create();
                cachedir_di.Refresh();
            }

            BrowserFetcher fetcher = new BrowserFetcher(new BrowserFetcherOptions()
            {
                Browser = supported_browser,
                Path = cachedir_di.FullName
            });

            IEnumerable<InstalledBrowser> browsers = fetcher.GetInstalledBrowsers();
            InstalledBrowser installed_browser = null;

            foreach (InstalledBrowser br in browsers)
            {
                if (br.Browser == SupportedBrowser.Chrome)
                {
                    installed_browser = br;
                    break;
                }
            }

            if (installed_browser == null && browsers.Count() == 0)
            {
                installed_browser = await fetcher.DownloadAsync(browser_tag);
            }

            Log.Debug("Browser exists!");

            {//Browser Setup
                FileInfo browser_executable = new FileInfo(installed_browser.GetExecutablePath());

                string ChromeProfilePath = ConfigurationManager.AppSettings.Get("ChromeProfilePath") != null ? ConfigurationManager.AppSettings["ChromeProfilePath"] : @"%LOCALAPPDATA%\Google\Chrome For Testing\User Data";
                string ChromeProfileName = ConfigurationManager.AppSettings.Get("ChromeProfileName") != null ? ConfigurationManager.AppSettings["ChromeProfileName"] : @"Default";
                string ChromeProfileDirectory = "Default";

                string UBlockVersionPath = "";
                string UBlockLiteExtensionID = ConfigurationManager.AppSettings.Get("UBlockLiteExtensionID") != null ? ConfigurationManager.AppSettings["UBlockLiteExtensionID"] : @"ddkjiahejlhfcafbddmgiahcphecmpfh";


                {//Query Local State for profile name and query profile preferences for extension path
                    ChromeProfilePath = Environment.ExpandEnvironmentVariables(ChromeProfilePath);
                    ChromeProfileDirectory = ChromeUtilities.QueryProfileDirectoryFromName(ChromeProfilePath, ChromeProfileName);

                    UBlockVersionPath = ChromeUtilities.QueryUBlockLitePath(ChromeProfilePath, ChromeProfileDirectory, UBlockLiteExtensionID);
                }

                // Get all instances of Chrome running on the local computer.
                // This will return an empty array if Chrome isn't running.
                string process_name = Path.GetFileNameWithoutExtension(browser_executable.Name.ToLower());
                Process[] localByName = Process.GetProcessesByName(process_name);

                if (localByName != null
                    && localByName.Length > 1
                    && localByName.Any(x => x.MainModule.FileVersionInfo.FileDescription.ToLower().Contains("for testing")))
                {
                    browser = await Puppeteer.ConnectAsync(new ConnectOptions
                    {
                        BrowserURL = "http://localhost:2122"
                    });
                }

                if (browser == null) //no existing running chrome instance, we'll launch one.
                {

                    string[] Args = new string[]
                    {
                        "--disable-blink-features=AutomationControlled",
                        "--enable-features=dev-mode",
                        "--remote-debugging-port=2122",
                        "--disable-features=site-per-process",
                        $"--user-data-dir=\"{ChromeProfilePath}\"",
                        $"--profile-directory=\"{ChromeProfileDirectory}\"",
                        $"--disable-extensions-except=\"{ChromeProfilePath}\\{ChromeProfileDirectory}\\Extensions\\{UBlockVersionPath}\"",
                    };

                    LaunchOptions lo = new LaunchOptions()
                    {
                        ExecutablePath = installed_browser.GetExecutablePath(),
                        Headless = headless,
                        Timeout = 10000,
                        IgnoreDefaultArgs = false,
                        Args = Args,
                    };

                    if (full_viewport)
                    {
                        lo.DefaultViewport = null;
                    }

                    browser = await Puppeteer.LaunchAsync(lo);
                }

                return browser;
            }
        }


        public static async Task ShutdownBrowser(IBrowser browser)
        {
            if (browser != null)
            {
                var pages = await browser.PagesAsync();
                foreach (var page in pages)
                {
                    if (page != null)
                    {
                        await page.CloseAsync();
                    }
                }

                await browser.CloseAsync();
                await browser.DisposeAsync();
            }
        }
    }
}
