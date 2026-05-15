using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace StravaTools.Utilities.Browser
{
    public class ChromeUtilities
    {
        public static string QueryProfileDirectoryFromName(string user_data_dir, string profile_name)
        {
            FileInfo local_state_fi = new FileInfo(Path.Combine(user_data_dir,"Local State"));
            dynamic local_state = JsonConvert.DeserializeObject(File.ReadAllText(local_state_fi.FullName));

            string profile_directory = "";

            foreach (dynamic item in local_state.profile.info_cache)
            {
                dynamic item_value = item.Value;

                if (item_value.name == profile_name)
                {
                    profile_directory = item.Name;
                }
            }

            return profile_directory;
        }

        public static string QueryUBlockLitePath(string user_data_dir, string profile_directory, string extension_id)
        {
            string UBlockVersionPath = "";

            DirectoryInfo profile_path = new DirectoryInfo(Path.Combine(user_data_dir, profile_directory));
            FileInfo preferences_file = profile_path.GetFiles("Preferences", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (preferences_file != null)
            {
                dynamic preferences = JsonConvert.DeserializeObject(File.ReadAllText(preferences_file.FullName));

                dynamic extension = preferences.extensions.settings[extension_id];

                if (extension != null)
                {
                    UBlockVersionPath = extension.path;
                }
            }

            return UBlockVersionPath;
        }
    }
}
