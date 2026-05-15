using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using StravaTools.Utilities.Processes;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace StravaTools.Utilities.Configuration
{
    public class ConfigurationUtilities
    {
        public const string StoreFile = "strava_app_info.dat";

        public static FileInfo GetLocalAppDataFile(DirectoryInfo lpd_dir, string preset_filename)
        {
            FileInfo preset_fi = new FileInfo(preset_filename);

            if (preset_fi.Exists)
            {
                return preset_fi;
            }

            preset_fi = new FileInfo(Path.Combine(lpd_dir.FullName, preset_filename));

            return preset_fi;
        }

        public static FileInfo[] FindAppInstallDirectories(string name, DirectoryInfo query_start_dir = null)
        {
            string query_start = (query_start_dir != null) ? $"/R {query_start_dir.FullName}": "";

            ProcessRunTask ptask = new ProcessRunTask()
            {
                FileName = "where",
                Arguments = $"{query_start} {name}",
                OnOutputReceived = null,
                OnErrorReceived = null
            };
            Task<int> task = ProcessUtilities.RunProcessAsync(ptask);
            int p_exitcode = task.Result;

            // Read the output from the 'where' command
            FileInfo[] fis = new FileInfo[ptask.OutputReceived.Count];
            for (int i = 0; i < ptask.OutputReceived.Count; i++)
            {
                fis[i] = new FileInfo(ptask.OutputReceived[i]);
            }

            ptask.Dispose();

            return fis;
        }

        public static async Task<FileInfo[]> FindAppInstallDirectoriesAsync(string name, DirectoryInfo query_start_dir = null, CancellationToken token = default)
        {
            string query_start = (query_start_dir != null) ? $"/R {query_start_dir.FullName}" : "";

            ProcessRunTask ptask = new ProcessRunTask()
            {
                FileName = "where",
                Arguments = $"{query_start} {name}",
                OnOutputReceived = null,
                OnErrorReceived = null
            };
            int p_exitcode = await ProcessUtilities.RunProcessAsync(ptask, token);
            // Read the output from the 'where' command
            FileInfo[] fis = new FileInfo[ptask.OutputReceived.Count];
            for (int i = 0; i < ptask.OutputReceived.Count; i++)
            {
                fis[i] = new FileInfo(ptask.OutputReceived[i]);
            }

            ptask.Dispose();

            return fis;
        }

        public static DirectoryInfo GetOrCreateLocalAppDataFolder(string optional_folder_name = "")
        {
            // Get the path to the AppData/Local folder
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string AppName = Assembly.GetCallingAssembly().GetName().Name;
            string folder_name = (!string.IsNullOrEmpty(optional_folder_name)) ? optional_folder_name : AppName;
            DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(localAppDataPath, folder_name));
            dir.Refresh();
            return dir;
        }

        public static DirectoryInfo GetOrCreateSubdir(DirectoryInfo dest, string folder_name = "")
        {
            if (string.IsNullOrEmpty(folder_name)) return null;
            DirectoryInfo sub_dir = new DirectoryInfo(Path.Combine(dest.FullName, folder_name));
            if (sub_dir.Exists)
            {
                return sub_dir;
            }
            else
            {
                sub_dir.Create();
                sub_dir.Refresh();
            }

            return sub_dir;
        }

        public static FileInfo GetOrCreateLog(DirectoryInfo dest, string filename = "_.log")
        {
            DirectoryInfo log_dir = new DirectoryInfo(Path.Combine(dest.FullName, "Logs"));
            log_dir.Create();
        
            FileInfo log = new FileInfo(Path.Combine(log_dir.FullName, filename));

            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(log.FullName, flushToDiskInterval: TimeSpan.FromSeconds(10), rollingInterval: RollingInterval.Day)
            .CreateLogger();

            //Console.SetOut(CLIContext.LoggerConsole.LoggerOutStreamWriter.Instance);
            //Console.SetError(CLIContext.LoggerConsole.LoggerErrorStreamWriter.Instance);

            return log;
        }

        public static FileInfo GetOrCreateCache(DirectoryInfo dest)
        {
            DirectoryInfo log_dir = new DirectoryInfo(dest.FullName);
            log_dir.Create();

            string filename = "settings.json";
            FileInfo settings_cache_fi = new FileInfo(Path.Combine(log_dir.FullName, filename));

            if (!settings_cache_fi.Exists) 
            {
                FileStream fs = settings_cache_fi.Create();

                JObject j = new JObject();

                using (StreamWriter sw = new StreamWriter(fs))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        j.WriteTo(writer);
                    }
                }

                fs.Close();
                fs.Dispose();
                settings_cache_fi.Refresh();
            }
            
            return settings_cache_fi;
        }

        public static void CacheSetting(FileInfo cache_fi, string key, string value)
        {
            string content = File.ReadAllText(cache_fi.FullName);

            dynamic cache = JsonConvert.DeserializeObject(content);

            cache[key] = value;

            content = JsonConvert.SerializeObject(cache);

            File.WriteAllText(cache_fi.FullName, content);
        }

        public static string GetCachedSetting(FileInfo cache_fi, string key)
        {
            string content = File.ReadAllText(cache_fi.FullName);

            dynamic cache = JsonConvert.DeserializeObject(content);

            if (cache.ContainsKey(key))
            {
                return cache[key];
            }

            return null;
        }

        public static string GetAppSetting(string key, string default_value = "")
        {
            return (!string.IsNullOrEmpty(ConfigurationManager.AppSettings.Get(key)))
                    ? ConfigurationManager.AppSettings[key]
                    : default_value;
        }

        public static bool StravaAppInfoExists(DirectoryInfo dest)
        {
            FileInfo strava_app_info_fi = new FileInfo(Path.Combine(dest.FullName, StoreFile));
            strava_app_info_fi.Refresh();
            return strava_app_info_fi.Exists;
        }

        public class StravaApplicationInfo
        {
            public string ClientId { get; set; } = "";
            public string ClientSecret { get; set; } = "";
        }

        public static StravaApplicationInfo LoadStravaApplicationInfo(DirectoryInfo dest)
        {
            FileInfo strava_app_info_fi = new FileInfo(Path.Combine(dest.FullName, StoreFile));
            if (!strava_app_info_fi.Exists)
                return null;

            try
            {
                byte[] encrypted = File.ReadAllBytes(strava_app_info_fi.FullName);

                byte[] decrypted = ProtectedData.Unprotect(
                    encrypted,
                    optionalEntropy: null,
                    scope: DataProtectionScope.CurrentUser);

                string json = Encoding.UTF8.GetString(decrypted);

                return JsonConvert.DeserializeObject<StravaApplicationInfo>(json);
            }
            catch
            {
                // Corrupted or unreadable store
                return null;
            }
        }

        public static bool PromptUserAndStoreStravaApplicationInfo(DirectoryInfo dest)
        {
            FileInfo strava_app_info_fi = new FileInfo(Path.Combine(dest.FullName, StoreFile));

            Console.WriteLine("First time running this app. This application operates on the assumption the user \nhas set up a Strava App and has the relevant info. If you don't have them, \nplease create the Strava App first and come back.\n");

            Console.Write("Enter Client ID: ");
            string client_id = Console.ReadLine();

            Console.Write("Enter Client Secret: ");
            string client_secret = Console.ReadLine();

            if (string.IsNullOrEmpty(client_id)
                || string.IsNullOrEmpty(client_secret))
            {
                Log.Error("You did not provide the required info.");
                return false;
            }

            StravaApplicationInfo sai = new StravaApplicationInfo
            {
                ClientId = client_id.Trim(),
                ClientSecret = client_secret.Trim()
            };

            try
            {
                string json = JsonConvert.SerializeObject(sai);
                byte[] plaintext = Encoding.UTF8.GetBytes(json);

                byte[] encrypted = ProtectedData.Protect(
                    plaintext,
                    optionalEntropy: null,
                    scope: DataProtectionScope.CurrentUser);

                File.WriteAllBytes(strava_app_info_fi.FullName, encrypted);
                return true;
            }
            catch(Exception e)
            {
                Log.Error($"Exception: {e.Message}");
                return false;
            }
        }
    }
}