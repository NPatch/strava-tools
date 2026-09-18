using Spectre.Console;
using SpectreConsoleExtensions;
using static interactive_cli_menu.Program;

namespace interactive_cli_menu
{
    internal class UIContext
    {
        internal Stack<UIMenu> menu_stack = new Stack<UIMenu>();
        internal List<string> fix_queue = new List<string>();
        internal DateRange date_range;
    }

    interface IUIMenu
    {
        void Show();
    }

    internal abstract class UIMenu : IUIMenu
    {
        public string Title = "";
        public bool RequestExit;
        public bool RequestBacktrack;

        public Action<UIMenu> NavigateToAction;
        public UIContext ctx;

        public abstract void Show();
    }

    internal class DownloadMenu : UIMenu
    {
        internal DownloadMenu()
        {
            ListActivitiesMenu lam = new ListActivitiesMenu()
            {
                Title = "Download Menu",
                ctx = ctx
            };

            ctx.menu_stack.Push(lam);
        }


        private void DownloadRemote(long activity_id)
        {
            AnsiConsole.Status()
                .Start($"Downloading remote activity {activity_id}", ctx =>
                {
                    // Simulate grinding
                    Thread.Sleep(5000);
                });
        }

        private long SelectActivityFromRemote()
        {
            long[] activity_ids = new long[]
            {
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                0
            };
            SelectionPrompt<long> selection = new SelectionPrompt<long>()
                                                        .Title("Select activity from Strava:")
                                                        .AddChoices(activity_ids);

            selection.UseConverter(x => (x != 0l) ? x.ToString() : "Back");

            long choice = AnsiConsole.Prompt(selection);

            return choice;
        }

        private DirectoryInfo GetLocalSubDir(string folder)
        {
            string user_dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string backups_dir = Path.Combine(user_dir, @"AppData\Local\strava-tools\Backups");
            return new DirectoryInfo(Path.Combine(backups_dir, folder));
        }

        public override void Show()
        {
            
        }
    }

    internal class DumpMenu : UIMenu
    {
        private void DumpLocal(FileInfo fi)
        {
            AnsiConsole.Status()
                .Start($"Dumping local activity {fi.FullName}", ctx =>
                {
                    // Simulate grinding
                    Thread.Sleep(5000);
                });
            RequestBacktrack = true;
        }

        void FixLocalMenu()
        {
            string choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Fix Local menu")
                            .AddCancelResult("Cancel")
                            .AddChoices("Original", "Modified", "Custom", "Back"));

            FileInfo fi = null;
            switch (choice)
            {
                case "Original":
                    {
                        DirectoryInfo dir = GetLocalSubDir("Original");
                        fi = SelectFileFromDirectory(dir);
                    }
                    break;
                case "Modified":
                    {
                        DirectoryInfo dir = GetLocalSubDir("Modified");
                        fi = SelectFileFromDirectory(dir);
                    }
                    break;
                case "Custom":
                    {
                        //
                    }
                    break;
                default:
                    RequestBacktrack = true;
                    return;
            }

            if (fi != null)
            {
                DumpLocal(fi);
            }
            else
            {
                RequestBacktrack = true;
                return;
            }
        }

        private FileInfo SelectFileFromDirectory(DirectoryInfo dir)
        {
            FileInfo[] fit_files = dir.GetFiles("*.fit", SearchOption.TopDirectoryOnly);
            SelectionPrompt<FileInfo> selection = new SelectionPrompt<FileInfo>()
                                                        .Title("Select file from directory:")
                                                        .AddChoices(fit_files)
                                                        .PageSize(10)
                                                        .WrapAround()
                                                        .EnableSearch()
                                                        .AddCancelResult((FileInfo)null!);

            selection.AddChoice(null);

            selection.UseConverter(x => (x != null) ? x.FullName : "Back");

            selection.SearchHighlightStyle = new Style(Color.Yellow, decoration: Decoration.Underline | Decoration.Bold);


            FileInfo choice = AnsiConsole.Prompt(selection);

            if (choice == null)
            {
                return null!;
            }

            return choice;
        }

        private DirectoryInfo GetLocalSubDir(string folder)
        {
            string user_dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string backups_dir = Path.Combine(user_dir, @"AppData\Local\strava-tools\Backups");
            return new DirectoryInfo(Path.Combine(backups_dir, folder));
        }

        public override void Show()
        {
            FixLocalMenu();
        }
    }

    internal class FixMenu : UIMenu
    {
        private void FixRemote(long activity_id)
        {
            AnsiConsole.Status()
                .Start($"Fixing remote activity {activity_id}", ctx =>
                {
                    // Simulate grinding
                    Thread.Sleep(5000);
                });
        }

        void FixLocalMenu()
        {
            string choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Fix Local menu")
                            .AddChoices("Original", "Modified", "Custom", "Back"));

            FileInfo fi = null;
            switch (choice)
            {
                case "Original":
                    {
                        DirectoryInfo dir = GetLocalSubDir("Original");
                        fi = SelectFileFromDirectory(dir);
                    }
                    break;
                case "Modified":
                    {
                        DirectoryInfo dir = GetLocalSubDir("Modified");
                        fi = SelectFileFromDirectory(dir);
                    }
                    break;
                case "Custom":
                    {
                        //
                    }
                    break;
                default:
                    RequestBacktrack = true;
                    return;
            }

            if (fi != null)
            {
                FixLocal(fi);
            }
        }

        private FileInfo SelectFileFromDirectory(DirectoryInfo dir)
        {
            FileInfo[] fit_files = dir.GetFiles("*.fit", SearchOption.TopDirectoryOnly);
            SelectionPrompt<FileInfo> selection = new SelectionPrompt<FileInfo>()
                                                        .Title("Select file from directory:")
                                                        .AddChoices(fit_files)
                                                        .PageSize(10)
                                                        .WrapAround()
                                                        .EnableSearch();

            selection.AddChoice(null);

            selection.UseConverter(x => (x!= null) ? x.FullName : "Back");

            selection.SearchHighlightStyle = new Style(Color.Yellow, decoration: Decoration.Underline | Decoration.Bold);


            FileInfo choice = AnsiConsole.Prompt(selection);

            if (choice == null)
            {
                return null;
            }

            return choice;
        }

        private long SelectActivityFromRemote()
        {
            long[] activity_ids = new long[]
            {
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                Random.Shared.Next(170000000, 190000000),
                0
            };
            SelectionPrompt<long> selection = new SelectionPrompt<long>()
                                                        .Title("Select activity from Strava:")
                                                        .AddChoices(activity_ids);

            selection.UseConverter(x => (x != 0l) ? x.ToString() : "Back");

            long choice = AnsiConsole.Prompt(selection);

            return choice;
        }

        private void FixLocal(FileInfo fi)
        {
            AnsiConsole.Status()
                .Start($"Fixing local activity {fi.FullName}", ctx =>
                {
                    // Simulate grinding
                    Thread.Sleep(5000);
                });

            return;
        }

        private DirectoryInfo GetLocalSubDir(string folder)
        {
            string user_dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string backups_dir = Path.Combine(user_dir, @"AppData\Local\strava-tools\Backups");
            return new DirectoryInfo(Path.Combine(backups_dir, folder));
        }

        public override void Show()
        {
            string choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Fix menu")
                            .AddChoices("Local", "Remote"));

            switch (choice)
            {
                case "Local":
                    {
                        FixLocalMenu();
                    }
                    break;
                case "Remote":
                    {
                        long activity_id = SelectActivityFromRemote();
                        FixRemote(activity_id);
                    }
                    break;
            }
        }
    }

    internal class ListActivitiesMenu : UIMenu
    {
        const int LAST_ACTIVITIES_NUM = 5;

        QueryType query_type = QueryType.UNKNOWN;

        void ActivityMenu(string activity_id)
        {
            AnsiConsole.Clear();
            SelectionPrompt<string> activity_menu_selection
                = new SelectionPrompt<string>()
                .Title("Activity Menu")
                .AddCancelResult("Cancel");

            if (!ctx.fix_queue.Contains(activity_id))
            {
                activity_menu_selection.AddChoice("Enqueue");
            }
            else
            {
                activity_menu_selection.AddChoice("Dequeue");
            }
            activity_menu_selection.AddChoice("Back");

            string choice = AnsiConsole.Prompt(activity_menu_selection);

            switch (choice)
            {
                case "Enqueue":
                    {
                        ctx.fix_queue.Add(activity_id);
                    }
                    break;
                case "Dequeue":
                    {
                        ctx.fix_queue.Remove(activity_id);
                    }
                    break;
                case "Cancel":
                    {
                        
                    }
                    break;
            }
        }

        public override void Show()
        {
            AnsiConsole.Clear();

            if (query_type == QueryType.UNKNOWN)
            {
                query_type = AnsiConsole.Prompt<QueryType>(new SelectionPrompt<QueryType>()
                                .Title("Query Type")
                                .AddCancelResult(QueryType.UNKNOWN)
                                .UseConverter(x => ((x == QueryType.UNKNOWN) ? "Back" : x.ToString()))
                                .AddChoices(new QueryType[] { QueryType.LAST_COUNT, QueryType.DATE_RANGE, QueryType.UNKNOWN }));
            }

            List<string> choices = new List<string>();
            switch (query_type)
            {
                case QueryType.LAST_COUNT:
                    {
                        for (int i = 0; i < LAST_ACTIVITIES_NUM; i++)
                        {
                            DateTime dt = DateTime.Now;
                            dt = dt.AddDays(i);
                            dt = dt.AddHours(Random.Shared.Next(-10, -1));

                            bool is_queued = ctx.fix_queue.Contains(i.ToString());
                            choices.Add($"{dt.ToString()} {((is_queued) ? "Queued" : "")}");
                        }
                    }
                    break;
                case QueryType.DATE_RANGE:
                    {
                        CalendarRangePrompt calendar_prompt = new CalendarRangePrompt(DateTime.Now);
                        calendar_prompt.AddCancelResult(DateRange.Empty);
                        ctx.date_range = AnsiConsole.Prompt(calendar_prompt);
                        if (ctx.date_range.Start != DateTime.MinValue
                            && ctx.date_range.End != DateTime.MinValue)
                        {
                            for (int i = 0; i < LAST_ACTIVITIES_NUM; i++)
                            {
                                DateTime dt = ctx.date_range.Start;

                                int month = Random.Shared.Next(ctx.date_range.Start.Month, ctx.date_range.End.Month);

                                int days = (ctx.date_range.End - ctx.date_range.Start).Days;
                                int day_offset = Random.Shared.Next(0, days);

                                dt = dt.AddDays(day_offset);

                                bool is_queued = ctx.fix_queue.Contains(i.ToString());
                                choices.Add($"{dt.ToString()} {((is_queued) ? "Queued" : "")}");
                            }
                        }
                        else
                        {
                            RequestBacktrack = true;
                            return;
                        }
                    }
                    break;
                default:
                    RequestBacktrack = true;
                    return;
            }
            choices.Add("Back");

            SelectionPrompt<string> activity_menu_selection
                = new SelectionPrompt<string>()
                            .Title("Activities menu")
                            .AddChoices(choices)
                            .AddCancelResult("Cancel");

            string activity_choice = "";
            do
            {
                AnsiConsole.Clear();

                if (ctx.date_range.Start != DateTime.MinValue
                                && ctx.date_range.End != DateTime.MinValue)
                {
                    AnsiConsole.MarkupLine($"DateRange {{ {ctx.date_range.Start.ToString()}, {ctx.date_range.End.ToString()} }} ");
                }

                activity_choice = AnsiConsole.Prompt(
                        activity_menu_selection);

                if (activity_choice == "Back"
                    || activity_choice == "Cancel")
                {
                    RequestBacktrack = true;
                    break;
                }
                else
                {
                    string actual_choice = activity_choice.Substring(0, activity_choice.IndexOf(' '));
                    ActivityMenu(actual_choice);
                    activity_choice = actual_choice;
                    break;
                }
            } while (activity_choice != "-1");
        }
    }

    internal class MainMenu : UIMenu
    {       
        public override void Show()
        {
            AnsiConsole.Clear();

            string choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title(Title)
                            .AddChoices("Fix", "Download", "Dump", "List", "Exit")
                            .AddCancelResult("Cancel"));

            switch (choice)
            {
                case "Fix":
                    {
                        //FixMenu();
                        FixMenu fm = new FixMenu()
                        {
                            Title = "Fix Menu",
                            ctx = ctx
                        };

                        ctx.menu_stack.Push(fm);
                    }
                    break;
                case "Download":
                    {
                        DownloadMenu dm = new DownloadMenu()
                        {
                            Title = "Download Menu",
                            ctx = ctx,
                        };

                        ctx.menu_stack.Push(dm);
                    }
                    break;
                case "Dump":
                    {
                        DumpMenu dm = new DumpMenu()
                        {
                            Title = "Dump Menu",
                            ctx = ctx
                        };

                        ctx.menu_stack.Push(dm);
                    }
                    break;
                case "List":
                    {
                        ListActivitiesMenu new_menu = new ListActivitiesMenu()
                        {
                            Title = "List Activities",
                            ctx = ctx
                        };

                        ctx.menu_stack.Push(new_menu);
                    }
                    break;
                case "Cancel":
                case "Exit":
                    {
                        RequestExit = true;
                    }
                    break;
            }
        }
    }

    internal class UIMenuController
    {
        public UIContext ctx;
        
        public bool RequestExit;

        public UIMenuController(UIContext _ctx)
        {
            ctx = _ctx;
            MainMenu m = new MainMenu()
            {
                Title = "Strava Tools",
                ctx = _ctx
            };
            ctx.menu_stack.Push(m);
        }

        public void Show()
        {
            UIMenu m = ctx.menu_stack.Peek();
            m.Show();

            if (m.RequestBacktrack)
            {
                ctx.menu_stack.Pop();
            }

            if (m.RequestExit)
            {
                RequestExit = m.RequestExit;
            }
        }
    }

    internal class Program
    {
        public enum QueryType
        {
            LAST_COUNT,
            DATE_RANGE,
            UNKNOWN
        }

        static void Main(string[] args)
        {
            var today = DateTime.Now;

            UIContext uctx = new UIContext();
            UIMenuController ui = new UIMenuController(uctx);

            while (!ui.RequestExit)
            {
                ui.Show();
            }


            //string choice = "";
            //while (true && choice != "Exit")
            //{
            //    AnsiConsole.Clear();

            //    //MultiSelectionPrompt<string> selection_prompt = new MultiSelectionPrompt<string>()
            //    //                    .Title("Activities Menu")
            //    //                    .NotRequired()
            //    //                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
            //    //                    .UseConverter(v => $"{values[values.IndexOf(v)]} {value_descrs[values.IndexOf(v)]}")
            //    //                    .AddChoices(values);

            //    choice = PrimaryMenu();

            //    //MultiSelectionPrompt<string> selection_prompt = new MultiSelectionPrompt<string>()
            //    //                    .Title("Activities Menu")
            //    //                    .NotRequired()
            //    //                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
            //    //                    .UseConverter(v => $"{values[values.IndexOf(v)]} {value_descrs[values.IndexOf(v)]}")
            //    //                    .AddChoices(values);

            //    //foreach (var val in fix_queue)
            //    //{
            //    //    selection_prompt.Select(val);
            //    //}

            //    //var features = AnsiConsole.Prompt(selection_prompt);

            //    //fix_queue.Clear();
            //    //fix_queue.AddRange(features);
            //    //choice = "";
            //}
        }      

        //private static string ListActivities()
        //{
        //    query_type = AnsiConsole.Prompt<QueryType>(new SelectionPrompt<QueryType>()
        //                    .Title("Query Type")
        //                    .UseConverter(x => ((x == QueryType.UNKNOWN) ? "Back" : x.ToString()))
        //                    .AddChoices(new QueryType[] { QueryType.LAST_COUNT, QueryType.DATE_RANGE, QueryType.UNKNOWN }));


        //    List<string> choices = new List<string>();
        //    DateRange dr = new DateRange();
        //    switch (query_type)
        //    {
        //        case QueryType.LAST_COUNT:
        //            {
        //                for (int i = 0; i < LAST_ACTIVITIES_NUM; i++)
        //                {
        //                    DateTime dt = DateTime.Now;
        //                    dt = dt.AddDays(i);
        //                    dt = dt.AddHours(Random.Shared.Next(-10, -1 ));

        //                    bool is_queued = fix_queue.Contains(i.ToString());
        //                    choices.Add($"{dt.ToString()} {((is_queued) ? "Queued" : "")}");
        //                }
        //            }
        //            break;
        //        case QueryType.DATE_RANGE:
        //            {
        //                dr = AnsiConsole.Prompt(new CalendarRangePrompt(DateTime.Now));
        //                if (dr.Start != DateTime.MinValue
        //                    && dr.End != DateTime.MinValue)
        //                {
        //                    for (int i = 0; i < LAST_ACTIVITIES_NUM; i++)
        //                    {
        //                        DateTime dt = dr.Start;

        //                        int month = Random.Shared.Next(dr.Start.Month, dr.End.Month);

        //                        int days = (dr.End - dr.Start).Days;
        //                        int day_offset = Random.Shared.Next(0, days);

        //                        dt = dt.AddDays(day_offset);

        //                        bool is_queued = fix_queue.Contains(i.ToString());
        //                        choices.Add($"{dt.ToString()} {((is_queued) ? "Queued" : "")}");
        //                    }
        //                }
        //            }
        //            break;
        //        default:
        //            return "-1";
        //    }
        //    choices.Add("Back");

        //    SelectionPrompt<string> activity_menu_selection
        //        = new SelectionPrompt<string>()
        //                    .Title("Activities menu")
        //                    .AddChoices(choices);

        //    string activity_choice = "";
        //    do
        //    {
        //        AnsiConsole.Clear();
                
        //        if (dr.Start != DateTime.MinValue
        //                        && dr.End != DateTime.MinValue)
        //        {
        //            AnsiConsole.MarkupLine($"DateRange {{ {dr.Start.ToString()}, {dr.End.ToString()} }} ");
        //        }

        //        activity_choice = AnsiConsole.Prompt(
        //                activity_menu_selection);

        //        if (activity_choice == "Back")
        //        {
        //            activity_choice = "-1";
        //            break;
        //        }
        //        else
        //        {
        //            string actual_choice = activity_choice.Substring(0, activity_choice.IndexOf(' '));
        //            ActivityMenu(actual_choice);
        //            activity_choice = actual_choice;
        //            break;
        //        }
        //    } while (activity_choice != "-1");
        //    return activity_choice;
        //}

        


    }
}
