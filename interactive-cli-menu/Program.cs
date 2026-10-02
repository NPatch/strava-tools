using Spectre.Console;
using SpectreConsoleExtensions;

namespace interactive_cli_menu
{
    public enum QueryType
    {
        LAST_COUNT,
        DATE_RANGE,
        UNKNOWN
    }

    internal class UIContext
    {
        internal Stack<UIMenu> menu_stack = new Stack<UIMenu>();
        internal List<string> fix_queue = new List<string>();
        internal DateRange date_range;

        internal string GetBreadcrumb()
        {
            string title = "";
            if (menu_stack.Count == 0) return "";
            UIMenu[] menus = menu_stack.ToArray();
            title = menus[0].Title;
            for(int i = 1; i < menus.Length; i++)
            {
                title += "->" + menus[i].Title;
            }
            return title;
        }

        internal void TransitionTo(UIMenu menu)
        {
            menu_stack.Push(menu);
            menu.OnEnter();
        }

        internal void TransitionFrom()
        {
            UIMenu removed = menu_stack.Pop();
            removed.OnExit();
            UIMenu curr = menu_stack.Peek();
            curr.OnReEnter(removed);
        }
    }

    interface IUIMenu
    {
        void OnEnter();
        void Show();
        void OnExit();
        void OnReEnter(UIMenu prev);
    }

    internal abstract class UIMenu : IUIMenu
    {
        public string Title = "";
        public bool RequestExit;
        public bool RequestBacktrack;

        public Action Initialize { get; set; } = null!;

        public Action<UIMenu> NavigateToAction;
        public UIContext ctx;

        public virtual void OnEnter() {}
        public virtual void OnExit() {}
        public virtual void OnReEnter(UIMenu prev) { }
        public abstract void Show();

    }

    internal class DownloadMenu : UIMenu
    {
        //private DirectoryInfo GetLocalSubDir(string folder)
        //{
        //    string user_dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        //    string backups_dir = Path.Combine(user_dir, @"AppData\Local\strava-tools\Backups");
        //    return new DirectoryInfo(Path.Combine(backups_dir, folder));
        //}

        public override void Show()
        {
            AnsiConsole.Clear();
            if (ctx.fix_queue.Count > 0)
            {
                AnsiConsole.MarkupLine($"[green]THe following activities are selected:[/]");
                foreach (string id in ctx.fix_queue)
                {
                    AnsiConsole.WriteLine($" - {id}");
                }

                bool confirmed = AnsiConsole.Confirm($"[green]Are you sure you want to?[/]:");
                if (confirmed)
                {
                    AnsiConsole.Status()
                    .Start($"Downloading remote activities", ctx =>
                    {
                        foreach (string id in this.ctx.fix_queue)
                        {
                            ctx.Status($"Downloading [green]{id}[/]");
                            // Simulate grinding
                            Thread.Sleep(2000);
                        }

                        ctx.Status($"[green]Finished downloading![/]");
                        // Simulate grinding
                        Thread.Sleep(5000);
                    });

                }
                ctx.fix_queue.Clear();
                RequestBacktrack = true;
            }
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
                            .Title(ctx.GetBreadcrumb() + "->Fix Local")
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
                                                        .Title(ctx.GetBreadcrumb() + "->Local Selection")
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
            RequestBacktrack = true;
        }

        void FixLocalMenu()
        {
            string choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title(ctx.GetBreadcrumb() + "->Fix Local")
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
                        DirectoryInfo dir = GetLocalSubDir("Modified");
                        fi = SelectFileFromDirectory(dir);
                    }
                    break;
                case "Cancel":
                default:
                    //RequestBacktrack = true;
                    return;
            }

            if (fi != null)
            {
                FixLocal(fi);
            }
            else
            {
                return; //Go back when SelectFileFromDirectory gives back null
            }
        }

        private FileInfo SelectFileFromDirectory(DirectoryInfo dir)
        {
            FileInfo[] fit_files = dir.GetFiles("*.fit", SearchOption.TopDirectoryOnly);
            SelectionPrompt<FileInfo> selection = new SelectionPrompt<FileInfo>()
                                                        .Title(ctx.GetBreadcrumb() + "->Local Selection")
                                                        .AddChoices(fit_files)
                                                        .PageSize(10)
                                                        .WrapAround()
                                                        .EnableSearch();

            selection.AddChoice(null!);
            selection.AddCancelResult((FileInfo)null!);

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
                                                        .Title(ctx.GetBreadcrumb() + "->Remote Selection")
                                                        .AddChoices(activity_ids);

            selection.UseConverter(x => (x != 0) ? x.ToString() : "Back");
            selection.AddCancelResult(0);

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
            RequestBacktrack = true;
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
                            .Title(ctx.GetBreadcrumb() + "Fix")
                            .AddCancelResult("Cancel")
                            .AddChoices("Local", "Remote", "Back"));

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
                        if (activity_id != 0)
                        {
                            FixRemote(activity_id);
                        }
                    }
                    break;
                case "Cancel":
                case "Back":
                default:
                    {
                        RequestBacktrack = true;
                        return;
                    }
            }
        }
    }

    internal class ListQueryTypeMenu : UIMenu
    {
        public QueryType query_type = QueryType.UNKNOWN;

        public override void Show()
        {
            AnsiConsole.Clear();

            if (query_type == QueryType.UNKNOWN)
            {
                query_type = AnsiConsole.Prompt<QueryType>(new SelectionPrompt<QueryType>()
                                .Title(Title)
                                .AddCancelResult(QueryType.UNKNOWN)
                                .UseConverter(x => ((x == QueryType.UNKNOWN) ? "Back" : x.ToString()))
                                .AddChoices(new QueryType[] { QueryType.LAST_COUNT, QueryType.DATE_RANGE, QueryType.UNKNOWN }));
            }
        }
    }

    internal class ListActivitiesMenu : UIMenu
    {
        const int LAST_ACTIVITIES_NUM = 5;

        public QueryType query_type = QueryType.UNKNOWN;

        public override void OnEnter()
        {
            ListQueryTypeMenu lqtm = new ListQueryTypeMenu()
            {
                ctx = ctx,
                Title = "Query Type Selection"
            };

            ctx.TransitionTo(lqtm);
        }

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
                            .Title(ctx.GetBreadcrumb()+"->Activities")
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

        public override void OnReEnter(UIMenu prev)
        {
            if(prev is ListQueryTypeMenu)
            {
                query_type = (prev as ListQueryTypeMenu)!.query_type;
            }
        }
    }

    internal class MainMenu : UIMenu
    {       
        public override void Show()
        {
            AnsiConsole.Clear();

            string choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title(ctx.GetBreadcrumb())
                            .AddChoices("Fix", "Download", "Dump", "List", "Exit")
                            .AddCancelResult("Cancel"));

            switch (choice)
            {
                case "Fix":
                    {
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
                            Initialize = () =>
                            {
                                ListActivitiesMenu lam = new ListActivitiesMenu()
                                {
                                    Title = "Download Menu",
                                    ctx = ctx
                                };

                                ctx.menu_stack.Push(lam);
                            }
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
            ctx.TransitionTo(m);
        }

        public void Show()
        {
            UIMenu m = ctx.menu_stack.Peek();
            m.Show();

            if (m.RequestBacktrack)
            {
                ctx.TransitionFrom();
            }

            if (m.RequestExit)
            {
                ctx.TransitionFrom();
                RequestExit = m.RequestExit;
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var today = DateTime.Now;

            UIContext uctx = new UIContext();
            UIMenuController ui = new UIMenuController(uctx);

            while (!ui.RequestExit)
            {
                ui.Show();
            }
        }
    }
}
