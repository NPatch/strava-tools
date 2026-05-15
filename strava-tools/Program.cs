using com.strava.v3.api.Api;
using com.strava.v3.api.Athletes;
using com.strava.v3.api.Authentication;
using com.strava.v3.api.Clients;
using Serilog;
using StravaTools.Utilities.Configuration;
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
                            await FixRemoteActivity(client, CLI.ProvidedCommandData.ActivityID, CLI.ProvidedCommandData.Steps, ctx.CancellationTokenSource);
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

        public static async Task FixRemoteActivity(StravaClient client, long activity_id, int steps, CancellationTokenSource cancellation_source)
        {
            Log.Information($"Fixing activity {activity_id}");
            try
            {
                cancellation_source.Token.ThrowIfCancellationRequested();
                StravaHelper.DownloadedFit files = await StravaHelper.DownloadActivity(client, activity_id, cancellation_source.Token);
                if (!files.Main.Exists)
                {
                    files.Backup.CopyTo(files.Main.FullName);
                }
                cancellation_source.Token.ThrowIfCancellationRequested();
                FitHelper.FixLocalActivity(files.Main, steps, cancellation_source);
                cancellation_source.Token.ThrowIfCancellationRequested();
                await StravaHelper.DeleteActivity(client, activity_id, cancellation_source.Token);
                cancellation_source.Token.ThrowIfCancellationRequested();
                Log.Information("Waiting for 4sec to allow system to flush the delete.");
                await Task.Delay(4000);
                cancellation_source.Token.ThrowIfCancellationRequested();
                await StravaHelper.UploadActivity(client, files.Main, cancellation_source.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in FixRemoteActivity Message: " + ex.Message);
            }
        }

        //static async Task FixActivitiesUsingXiaomiData(FileInfo xiaomi_sport_recs_csv, DirectoryInfo fit_dir)
        //{
        //    List<XiaomiSportRecord> xiaomi_records = XiaomiHelper.ExtractSportRecords(xiaomi_sport_recs_csv);

        //    FileInfo[] fits = fit_dir.GetFiles("*.fit", SearchOption.TopDirectoryOnly);
        //    for (int i = 0; i < fits.Length; i++)
        //    {
        //        try
        //        {
        //            // Attempt to open .FIT file
        //            FitMessages fitMessages = FitHelper.Decode(fits[i]);
        //            SysDateTime dt = fitMessages.FileIdMesgs[0].GetTimeCreated().GetDateTime();
        //            Log.Information($"{fits[i].Name} was created on {dt.ToString()}");

        //            long activity_ft = dt.ToFileTimeUtc();

        //            int index = XiaomiHelper.FindRelevantRecord(ref xiaomi_records, activity_ft);
        //            if (index != -1)
        //            {
        //                XiaomiSportRecord xiaomi_record = xiaomi_records[index];

        //                int steps_ts = XiaomiHelper.GetStepsTotalStrides(xiaomi_record);
        //                if (steps_ts > 0)
        //                {
        //                    uint strides = Convert.ToUInt32(steps_ts);
        //                    fitMessages.SessionMesgs[0].SetTotalStrides(strides);
        //                    //fitMessages.SessionMesgs[0].SetTotalCycles(strides);
        //                }
        //                else
        //                {
        //                    int steps = XiaomiHelper.ApproximateSteps(xiaomi_record);
        //                    uint strides = Convert.ToUInt32(steps / 2);
        //                    fitMessages.SessionMesgs[0].SetTotalStrides(strides);
        //                    fitMessages.SessionMesgs[0].SetTotalCycles(strides);
        //                }

        //                fitMessages.SessionMesgs[0].SetEvent(Event.Session);
        //                fitMessages.SessionMesgs[0].SetEventType(EventType.Stop);


        //                // Create file encode object
        //                FitHelper.Encode(new FileInfo(fits[i].FullName.Replace(".fit","_modified.fit")), ref fitMessages);
        //            }
        //        }
        //        catch (FitException ex)
        //        {
        //            Log.Error("A FitException occurred when trying to decode the FIT file. Message: " + ex.Message);
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("Exception occurred when trying to decode the FIT file. Message: " + ex.Message);
        //        }
        //    }
        //}

        //static async Task CSVRead(DirectoryInfo export_dir, FileInfo fit, SysDateTime rstart = default, SysDateTime rend = default)
        //{
        //    FileInfo fitness_data = export_dir.GetFiles("*center_fitness_data.csv", SearchOption.TopDirectoryOnly)[0];

        //    SysDateTime timeCreated = new SysDateTime();

        //    List<XiaomiFitnessRecord> xfit_events = XiaomiHelper.ExtractFitnessRecords(fitness_data);

        //    XiaomiHelper.GetStepsMessagesInRange(xfit_events);
        //    List<XiaomiFitnessRecord> waypoint_data = XiaomiHelper.GetStepsMessagesInRange(xfit_events);

        //    xfit_events.Clear();

        //    {
        //        try
        //        {
        //            FitMessages fitMessages = FitHelper.Decode(fit);

        //            if (timeCreated.Year < 2000)
        //            {
        //                timeCreated = fitMessages.FileIdMesgs[0].GetTimeCreated().GetDateTime();
        //            }

        //            Log.Information($"    File Activity created {timeCreated.ToLocalTime().ToString()}");

        //            {
        //                int steps = 0;
        //                float cadence = 0.0f;
        //                SysDateTime prev_min = timeCreated;
        //                SysDateTime next_min = prev_min + TimeSpan.FromSeconds(60);
        //                int count = fitMessages.RecordMesgs.Count;
        //                for (int i = 0; i < count; i++)
        //                {
        //                    RecordMesg rm = fitMessages.RecordMesgs[i];
        //                    SysDateTime cur_dt = rm.GetTimestamp().GetDateTime();
        //                    if (cur_dt == next_min)
        //                    {
        //                        steps = XiaomiHelper.GetAggregateStepsInRange(ref waypoint_data, prev_min, cur_dt);
        //                        if (cadence == 0)
        //                        {
        //                            cadence = (float)steps;
        //                        }
        //                        else
        //                        {
        //                            float new_cadence = cadence * 0.98f + (steps * 0.02f);
        //                            cadence = new_cadence;
        //                        }
        //                        steps = 0;
        //                        prev_min = cur_dt;
        //                        next_min = prev_min + TimeSpan.FromSeconds(60);
        //                    }

        //                    Log.Information($"T:{rm.GetTimestamp().GetDateTime().ToString()} Cadence:{cadence}");
        //                    int cad_spm = (int)Math.Floor(cadence / 2.0f);
        //                    rm.SetCadence((byte)cad_spm);
        //                }
        //            }

        //            //if(false)
        //            {
        //                Log.Information($"    Overwriting file");
        //                // Create file encode object
        //                FileInfo fi = new FileInfo(Path.Combine(fit.DirectoryName, fit.Name.Replace("Walk_1", "Walk_2")));
        //                FitHelper.Encode(fi, ref fitMessages);
        //            }
        //        }
        //        catch (FitException ex)
        //        {
        //            Log.Error($"  A FitException occurred when trying to decode the FIT file. Message: " + ex.Message);
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error($"  Exception occurred when trying to decode the FIT file. Message: " + ex.Message);
        //        }
        //        finally
        //        {
        //            Log.Information($"    Finished overwriting file");
        //        }
        //    }
        //}
    }
}
