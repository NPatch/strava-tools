using com.strava.v3.api.Api;
using com.strava.v3.api.Athletes;
using com.strava.v3.api.Authentication;
using com.strava.v3.api.Clients;
using Serilog;
using StravaTools.Utilities.Configuration;
using StravaTools.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using SysDateTime = System.DateTime;

namespace StravaTools
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            WorkContext ctx = new WorkContext();

            ctx.localappdata_dir = ConfigurationUtilities.GetOrCreateLocalAppDataFolder();
            ctx.backup_dir = ConfigurationUtilities.GetOrCreateSubdir(ctx.localappdata_dir, "Backups");
            ctx.original_backup_dir = ConfigurationUtilities.GetOrCreateSubdir(ctx.backup_dir, "Original");
            ctx.modified_backup_dir = ConfigurationUtilities.GetOrCreateSubdir(ctx.backup_dir, "Modified");
            ConfigurationUtilities.GetOrCreateLog(ctx.localappdata_dir, "strava-tools_.log");

            Console.WriteLine(@"   _____ _                            _______            _        ");
            Console.WriteLine(@"  / ____| |                          |__   __|          | |       ");
            Console.WriteLine(@" | (___ | |_ _ __ __ _ _    __ __ _     | | ____   ____ | | ___   ");
            Console.WriteLine(@"  \___ \| __| '__/ _` | \  / // _` |    | |/ __ \ / __ \| |/ __|  ");
            Console.WriteLine(@"  ____) | |_| | | (_| |\ \/ /| (_| |    | | (__) | (__) | |\__ \  ");
            Console.WriteLine(@" |_____/ \__|_|  \__,_| \__/  \__,_|    |_|\____/ \____/|_||___/  ");
            Console.WriteLine(@"                                                      by npatch   ");
            Console.WriteLine();
            if (!ConfigurationUtilities.StravaAppInfoExists(ctx.localappdata_dir))
            {
                if (!ConfigurationUtilities.PromptUserAndStoreStravaApplicationInfo(ctx.localappdata_dir))
                {
                    Log.Error("Could not create StravaApplicationInfo file. Closing...");
                    return;
                }
            }
            
            {//Load StravaApplicationInfo
                ConfigurationUtilities.StravaApplicationInfo sai = ConfigurationUtilities.LoadStravaApplicationInfo(ctx.localappdata_dir);
                if (sai != null)
                {
                    ctx.ClientID = sai.ClientId;
                    ctx.ClientSecret = sai.ClientSecret;
                }
                else
                {
                    FileInfo store_file = ConfigurationUtilities.GetLocalAppDataFile(ctx.localappdata_dir, ConfigurationUtilities.StoreFile);
                    if (store_file != null
                        && store_file.Exists)
                    {
                        store_file.Delete();
                        store_file.Refresh();
                    }
                    Log.Error("No StravaApplicationInfo found. Restart to create them.");
                    return;
                }
            }

            /*
             * Modes:   1)fix local <filename> <steps>
             *          2)fix remote <activity-id> <steps>
             *          3)upload <filename> (--check-by-id=<activity-id> | --check-by-dates before after) (maybe internally instead?)
             *          4)download range <before> <after>
             *          5)download <activity-id>
             *          6)delete <activity-id>
             *          7)list-activities <how-many-latest> <before> <after>
             *          8)dump <filename>
             */

            if (!CLI.Process(args))
            {
                return;
            }

            try
            {
                StaticAuthentication auth = null;
                StravaClient client = null;

                StravaHelper.InitializeFilesAndDirectories(ctx.localappdata_dir);

                if (CLI.ProvidedCommandType != CLI.CommandType.DumpFit
                    && CLI.ProvidedCommandType != CLI.CommandType.FixLocal)
                {
                    await StravaHelper.InitializeAppCredentials(ctx.ClientID, ctx.ClientSecret, ctx.Token);
                    bool success = await StravaHelper.Authenticate(ctx.CancellationTokenSource);
                    if (!success)
                    {
                        return;
                    }

                    Limits.UsageChanged += delegate (object o, UsageChangedEventArgs ea)
                    {
                        Log.Information($"Usage Short: {ea.Usage.ShortTerm} Limit Short: {Limits.Limit.ShortTerm}\nUsage Long: {ea.Usage.LongTerm} Limit Long: {Limits.Limit.LongTerm}");
                    };

                    Log.Information($"Connecting to Strava");

                    auth = new StaticAuthentication(StravaHelper.Tokens.AccessToken);
                    client = new StravaClient(auth);
                    
                    //Making sure the RangeStart is valid for subsequent calls.
                    if (CLI.ProvidedCommandData.RangeStart == SysDateTime.MinValue)
                    {
                        Athlete me = client.Athletes.GetAthlete();

                        CLI.ProvidedCommandData = new CLI.CommandResult()
                        {
                            ActivityID = CLI.ProvidedCommandData.ActivityID,
                            Filename = CLI.ProvidedCommandData.Filename,
                            EntriesNum = CLI.ProvidedCommandData.EntriesNum,
                            Steps = CLI.ProvidedCommandData.Steps,
                            RangeStart = SysDateTime.Parse(me.CreatedAt),
                            RangeEnd = CLI.ProvidedCommandData.RangeEnd
                        };
                    }
                }

                switch (CLI.ProvidedCommandType)
                {
                    case CLI.CommandType.Delete:
                        {
                            Log.Information($"Deleting Activity {CLI.ProvidedCommandData.ActivityID}");
                            await StravaHelper.DeleteActivity(client, CLI.ProvidedCommandData.ActivityID, ctx.Token);
                        }
                        break;
                    case CLI.CommandType.Upload:
                        {
                            Log.Information($"Uploading Activity .fit {CLI.ProvidedCommandData.Filename}");
                            await StravaHelper.UploadActivity(client, CLI.ProvidedCommandData.Filename, ctx.CancellationTokenSource.Token);
                        }
                        break;
                    case CLI.CommandType.DownloadRange:
                        {
                            Log.Information($"Downloading .fit files from {CLI.ProvidedCommandData.RangeStart} to {CLI.ProvidedCommandData.RangeEnd}");
                            await StravaHelper.DownloadActivityRange(client, CLI.ProvidedCommandData.RangeStart, CLI.ProvidedCommandData.RangeEnd, ctx.CancellationTokenSource.Token);
                        }
                        break;
                    case CLI.CommandType.DownloadID:
                        {
                            Log.Information($"Downloading .fit for Activity #{CLI.ProvidedCommandData.ActivityID}");
                            await StravaHelper.DownloadActivity(client, CLI.ProvidedCommandData.ActivityID, ctx.CancellationTokenSource.Token);
                        }
                        break;
                    case CLI.CommandType.ListActivities:
                        {
                            Log.Information($"Listing the last #{CLI.ProvidedCommandData.EntriesNum} Activities");
                            StravaHelper.ListActivities(client, CLI.ProvidedCommandData.RangeStart, CLI.ProvidedCommandData.RangeEnd, CLI.ProvidedCommandData.EntriesNum, ctx.CancellationTokenSource.Token);
                        }
                        break;
                    case CLI.CommandType.FixLocal:
                        {
                            Log.Information($"Will add steps(#{CLI.ProvidedCommandData.Steps}) to the local activity .fit file {CLI.ProvidedCommandData.Filename}");
                            StravaHelper.DownloadedFit files = StravaHelper.GetDownloadedFitPair(CLI.ProvidedCommandData.Filename);
                            files.Backup.CopyTo(files.Main.FullName, overwrite: true);
                            files.Main.Refresh();
                            FitHelper.FixLocalActivity(files.Main, CLI.ProvidedCommandData.Steps, ctx.CancellationTokenSource);
                        }
                        break;
                    case CLI.CommandType.FixRemote:
                        {
                            Log.Information($"Will add steps(#{CLI.ProvidedCommandData.Steps}) the remote activity #{CLI.ProvidedCommandData.ActivityID}");
                            await FixRemoteActivity(client, CLI.ProvidedCommandData.ActivityID, CLI.ProvidedCommandData.Steps, FitHelper.FixLocalActivity, ctx.CancellationTokenSource);
                        }
                        break;
                    case CLI.CommandType.DumpFit:
                        {
                            Log.Information($"Dumping contents for Activity fit: {CLI.ProvidedCommandData.Filename.Name}");
                            FitHelper.DumpFit(CLI.ProvidedCommandData.Filename);
                        }
                        break;
                    default:
                        {
                            break;
                        }
                }

                Log.Information("Finished");
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception)
            {
                Console.ReadKey();
            }
        }
    }
}
