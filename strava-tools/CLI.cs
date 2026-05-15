using StravaTools.Utilities.CommandLine;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace StravaTools
{
    internal class CLI
    {
        internal enum CommandType
        {
            DownloadRange,
            DownloadID,
            Delete,
            Upload,
            FixLocal,
            FixRemote,
            ListActivities,
            DumpFit,
            Unknown
        }

        internal struct CommandResult
        {
            public long ActivityID { get; set; }
            public FileInfo Filename { get; set; }
            public int Steps { get; set; }
            public DateTime RangeStart { get; set; }
            public DateTime RangeEnd { get; set; }
            public int EntriesNum { get; set; }
        }

        public static CommandType ProvidedCommandType { get; private set; }
        public static CommandResult ProvidedCommandData { get; set; }

        internal static RootCommand RootCom { get; set; }

        internal static Command DownloadCom { get; set; }
        internal static Argument<long> ActivityIDArg { get; set; }
        internal static Command DownloadRangeCom { get; set; }
        internal static Option<string> RangeBeforeOpt { get; set; }
        internal static Option<string> RangeAfterOpt { get; set; }

        internal static Command UploadCom { get; set; }
        internal static Argument<FileInfo> FileArg { get; set; }

        internal static Command DeleteCom { get; set; }

        internal static Command FixCom { get; set; }

        internal static Command LocalCom { get; set; }

        internal static Command RemoteCom { get; set; }

        internal static Argument<int> StepsArg { get; set; }

        internal static Command ListActivitiesCom { get; set; }
        internal static Option<int> ListActivitiesAmountOpt { get; set; }

        internal static Command DumpCom { get; set; }


        //internal static Option<DirectoryInfo> DownloadFolderOpt { get; set; }

        public static void Initialize()
        {
            RootCom = new RootCommand("Strava Tools ");

            {//Common
                {//Arguments
                    ActivityIDArg = new Argument<long>("activity-id")
                    {
                        Description = "ID for the activity",
                        Arity = ArgumentArity.ExactlyOne
                    };

                    FileArg = new Argument<FileInfo>("filepath")
                    {
                        Arity = ArgumentArity.ExactlyOne,
                        Description = "Filepath to an Activity .fit",
                    };
                }

                //Argument Validation
                {
                    ActivityIDArg.AcceptOnlyValidNumbers();
                    FileArg.AcceptLegalFullPathsOnly<FileInfo>();
                    FileArg.AcceptExtensions<FileInfo>(".fit");
                }

                {//Options
                    RangeBeforeOpt = new Option<string>("--before", "-b")
                    {
                        Description = "DateTime, we accept activities before it.",
                        Arity = ArgumentArity.ZeroOrOne,
                    };

                    RangeAfterOpt = new Option<string>("--after", "-a")
                    {
                        Description = "DateTime, we accept activities after it.",
                        Arity = ArgumentArity.ZeroOrOne
                    };
                }

                //Option Validation
                {
                    RangeBeforeOpt.AcceptOnlyValidDateTimes();
                    RangeAfterOpt.AcceptOnlyValidDateTimes();
                }
            }

            {//upload
                UploadCom = new Command("upload")
                {
                    Description = "Uploads an Activity .fit file to the server",
                };

                //Register Commands, Arguments and Options with parent entities
                {
                    UploadCom.Arguments.Add(FileArg);
                    RootCom.Subcommands.Add(UploadCom);
                }
            }

            {//delete
                DeleteCom = new Command("delete")
                {
                    Description = "Deletes an activity on the server, given its id"
                };

                //Register Commands, Arguments and Options with parent entities
                {
                    DeleteCom.Arguments.Add(ActivityIDArg);
                    RootCom.Subcommands.Add(DeleteCom);
                }
            }

            {//dump
                DumpCom = new Command("dump")
                {
                    Description = "Dumps the contents of a local file"
                };

                //Register Commands, Arguments and Options with parent entities
                {
                    DumpCom.Arguments.Add(FileArg);
                    RootCom.Subcommands.Add(DumpCom);
                }
            }

            {//download
                DownloadCom = new Command("download")
                {
                    Description = "Downloads an Activity .fit file from the server for activities either by id or timeframe",
                };

                DownloadRangeCom = new Command("range")
                {
                    Description = "Downloads Activity .fit files based on range information"
                };

                //Register Commands, Arguments and Options with parent entities
                {
                    DownloadCom.Arguments.Add(ActivityIDArg);
                    DownloadRangeCom.Options.Add(RangeBeforeOpt);
                    DownloadRangeCom.Options.Add(RangeAfterOpt);
                    DownloadCom.Subcommands.Add(DownloadRangeCom);
                    RootCom.Subcommands.Add(DownloadCom);
                }

                //Command Validators
                {
                    DownloadRangeCom.Validators.Add(commandResult =>
                    {
                        OptionResult after_opt = commandResult.GetResult(RangeAfterOpt);
                        OptionResult before_opt = commandResult.GetResult(RangeBeforeOpt);

                        int aggregate_token_count = ((after_opt != null) ? after_opt.Tokens.Count : 0)
                                                    + ((before_opt != null) ? before_opt.Tokens.Count : 0);

                        if (aggregate_token_count < 1)
                        {
                            commandResult.AddError("At least one of before or after DateTimes is needed."); //, or a comma-separated list of numeric IDs.
                        }
                    });
                }
            }

            {//fix
                FixCom = new Command("fix")
                {
                    Description = "Fixes an activity's lack of steps, provided the step count"
                };

                RemoteCom = new Command("remote")
                {
                    Description = "Fixes an activity straight from the server, using an activity id"
                };

                LocalCom = new Command("local")
                {
                    Description = "Fixes an activity .fit file locally"
                };

                StepsArg = new Argument<int>("steps")
                {
                    Description = "# of steps to add",
                    Arity = ArgumentArity.ExactlyOne
                };

                //Argument Validation
                {
                    StepsArg.AcceptOnlyValidNumbers();
                }

                //Register Commands, Arguments and Options with parent entities
                {
                    RemoteCom.Arguments.Add(ActivityIDArg);
                    RemoteCom.Arguments.Add(StepsArg);
                    LocalCom.Arguments.Add(FileArg);
                    LocalCom.Arguments.Add(StepsArg);
                    FixCom.Subcommands.Add(LocalCom);
                    FixCom.Subcommands.Add(RemoteCom);
                    RootCom.Subcommands.Add(FixCom);
                }
            }

            {//list-activities
                ListActivitiesCom = new Command("list-activities")
                {
                    Description = "Lists the activities on the server including some relevant info"
                };

                ListActivitiesAmountOpt = new Option<int>("--num", "-n")
                {
                    Description = "# of latest activities to list",
                    Arity = ArgumentArity.ExactlyOne,
                    HelpName = "num",
                    DefaultValueFactory = arg_res => 5
                };

                //Register Commands, Arguments and Options with parent entities
                {
                    ListActivitiesCom.Options.Add(ListActivitiesAmountOpt);
                    ListActivitiesCom.Options.Add(RangeBeforeOpt);
                    ListActivitiesCom.Options.Add(RangeAfterOpt);
                    RootCom.Subcommands.Add(ListActivitiesCom);
                }
            }
        }

        private static bool Execute(string[] args)
        {
            ParseResult parseResult = RootCom.Parse(args);

            {
                List<Command> commands = new List<Command>();
                commands.AddRange(RootCom.Subcommands);
                foreach (Command command in RootCom.Subcommands)
                {
                    commands.AddRange(command.Subcommands);
                }

                if (commands.Any(x => parseResult.UnmatchedTokens.Contains(x.Name)))
                {
                    parseResult.CommandResult.AddError($"Only one subcommand can be used at a time.");
                }

                commands.Clear();
            }

            int exit_code = parseResult.Invoke();
            if (exit_code != 0)
            {
                return false;
            }

            return true;
        }

        public static bool Process(string[] args)
        {
            Initialize();

            {//Define actions for data extraction
                {//Delete Action
                    DeleteCom.SetAction(async (parseResult) =>
                    {
                        ProvidedCommandType = CommandType.Delete;

                        long input = parseResult.GetResult(ActivityIDArg).GetValueOrDefault<long>();

                        ProvidedCommandData = new CommandResult()
                        {
                            ActivityID = input
                        };
                    });
                }

                {//Dump Action
                    DumpCom.SetAction(async (parseResult) =>
                    {
                        ProvidedCommandType = CommandType.DumpFit;

                        FileInfo input = parseResult.GetResult(FileArg).GetValueOrDefault<FileInfo>();

                        ProvidedCommandData = new CommandResult()
                        {
                            Filename = input
                        };
                    });
                }

                {//ListActivities Action
                    ListActivitiesCom.SetAction(async (parseResult) =>
                    {
                        ProvidedCommandType = CommandType.ListActivities;
                        OptionResult opt_res = parseResult.GetResult(ListActivitiesAmountOpt);
                        //int input = (opt_res != null) ? opt_res.GetValueOrDefault<int>() : 5;
                        int input = opt_res.GetValueOrDefault<int>();

                        string before = "";
                        string after = "";

                        if (parseResult.GetResult(RangeBeforeOpt) != null)
                        {
                            before = parseResult.GetResult(RangeBeforeOpt).GetValueOrDefault<string>();
                        }

                        if (parseResult.GetResult(RangeAfterOpt) != null)
                        {
                            after = parseResult.GetResult(RangeAfterOpt).GetValueOrDefault<string>();
                        }

                        ProvidedCommandData = new CommandResult()
                        {
                            EntriesNum = input,
                            RangeStart = (!string.IsNullOrEmpty(after)) ? DateTime.Parse(after) : DateTime.MinValue,
                            RangeEnd = (!string.IsNullOrEmpty(before)) ? DateTime.Parse(before) : DateTime.UtcNow,
                        };
                    });
                }

                {//Upload Action
                    UploadCom.SetAction(async (parseResult) =>
                    {
                        ProvidedCommandType = CommandType.Upload;

                        FileInfo file_fi = parseResult.GetResult(FileArg).GetValueOrDefault<FileInfo>();

                        ProvidedCommandData = new CommandResult()
                        {
                            Filename = file_fi
                        };
                    });
                }

                {//Download Action
                    DownloadCom.SetAction(async (parseResult) =>
                    {
                        ProvidedCommandType = CommandType.DownloadID;

                        long id = parseResult.GetResult(ActivityIDArg).GetValueOrDefault<long>();

                        ProvidedCommandData = new CommandResult()
                        {
                            ActivityID = id
                        };
                    });

                    DownloadRangeCom.SetAction(async (parseResult) =>
                    {
                        ProvidedCommandType = CommandType.DownloadRange;

                        string before = "";
                        string after = "";

                        if (parseResult.GetResult(RangeBeforeOpt) != null)
                        {
                            before = parseResult.GetResult(RangeBeforeOpt).GetValueOrDefault<string>();
                        }

                        if (parseResult.GetResult(RangeAfterOpt) != null)
                        {
                            after = parseResult.GetResult(RangeAfterOpt).GetValueOrDefault<string>();
                        }

                        ProvidedCommandData = new CommandResult()
                        {
                            RangeStart = (!string.IsNullOrEmpty(after)) ? DateTime.Parse(after) : DateTime.MinValue,
                            RangeEnd = (!string.IsNullOrEmpty(before)) ? DateTime.Parse(before) : DateTime.MaxValue,
                        };
                    });
                }

                {//Fix Action
                    RemoteCom.SetAction(async (parseResult) =>
                    {
                        ProvidedCommandType = CommandType.FixRemote;

                        long id = parseResult.GetResult(ActivityIDArg).GetValueOrDefault<long>();

                        int steps = parseResult.GetResult(StepsArg).GetValueOrDefault<int>();

                        ProvidedCommandData = new CommandResult()
                        {
                            ActivityID = id,
                            Steps = steps,
                        };
                    });

                    LocalCom.SetAction(async (parseResult) =>
                    {
                        ProvidedCommandType = CommandType.FixLocal;

                        FileInfo file_fi = parseResult.GetResult(FileArg).GetValueOrDefault<FileInfo>();

                        int steps = parseResult.GetResult(StepsArg).GetValueOrDefault<int>();

                        ProvidedCommandData = new CommandResult()
                        {
                            Filename = file_fi,
                            Steps = steps,
                        };
                    });
                }
            }

            return Execute(args);
        }
    }
}
