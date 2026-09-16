using Spectre.Console;
using Spectre.Console.Rendering;

namespace SpectreConsoleExtensions
{
    public sealed class CalendarRangePrompt : IPrompt<DateRange>
    {
        private Calendar cal;

        DateTime cursor = DateTime.Today;
        DateTime? start = null;
        DateTime? end = null;

        public CalendarRangePrompt(int year, int month)
        {
            cal = new Calendar(year, month);
            cal.HighlightStyle(Color.Yellow);
        }

        public CalendarRangePrompt(DateTime dt)
        {
            cal = new Calendar(dt);
            cal.HighlightStyle(Color.Yellow);
        }

        public CalendarRangePrompt(Calendar _cal)
        {
            cal = _cal;
            cal.HighlightStyle(Color.Yellow);
        }

        public DateRange Show(IAnsiConsole console)
        {
            return ShowAsync(console, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<DateRange> ShowAsync(
            IAnsiConsole console,
            CancellationToken cancellationToken)
        {
             while (!cancellationToken.IsCancellationRequested)
            {
                console.Cursor.SetPosition(0,0);
                await Task.Yield();

                cal.Year = cursor.Year;
                cal.Month = cursor.Month;
                cal.Day = cursor.Day;

                if (start != null && end != null)
                {
                    for (int i = cal.CalendarEvents.Count - 1; i >= 0; i--)
                    {
                        CalendarEvent ce = cal.CalendarEvents[i];
                        DateTime dt = new DateTime(ce.Year, ce.Month, ce.Day);
                        if (dt < start || dt > end)
                        {
                            cal.CalendarEvents.Remove(ce);
                        }
                    }

                    DateTime actual_end = end.Value + TimeSpan.FromDays(1);

                    for (DateTime dt = start.Value; dt < actual_end; dt = dt.AddDays(1))
                    {
                        cal.AddCalendarEvent(dt);
                    }
                }

                console.Write(cal);


                // Instructions
                console.Markup("[grey]Arrows move, Space selects, Esc exits[/]");
                
                await Task.Yield();

                // Compute cursor position
                var (x, y) = GetCursorPosition(cursor);

                // Draw cursor overlay
                console.Cursor.SetPosition(x, y);
                console.Markup("[red]■[/]");

                await Task.Yield();

                var key = console.Input.ReadKey(true).Value.Key;

                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        cursor = cursor.AddDays(-1);
                        break;

                    case ConsoleKey.RightArrow:
                        cursor = cursor.AddDays(+1);
                        break;

                    case ConsoleKey.UpArrow:
                        cursor = cursor.AddDays(-7);
                        break;

                    case ConsoleKey.DownArrow:
                        cursor = cursor.AddDays(+7);
                        break;

                    case ConsoleKey.Spacebar:
                        HandleSelection();
                        break;

                    case ConsoleKey.Enter:
                        if (start.HasValue &&
                            end.HasValue)
                        {
                            return new DateRange(
                                start.Value,
                                end.Value
                            );
                        }
                        break;

                    case ConsoleKey.Escape:
                        throw new OperationCanceledException(
                            "Calendar range selection cancelled."
                        );
                }

                await Task.Yield();
                //await Task.Delay(1, cancellationToken);
            }

            throw new OperationCanceledException();
        }

        private void HandleSelection()
        {
            if (start == null && end == null)
            {
                start = cursor;
                end = cursor;
            }
            else if (end == start)
            {
                if (cursor < start)
                {
                    start = cursor;
                }
                else if (cursor > end)
                {
                    end = cursor;
                }
                else if (start == cursor || end == cursor)
                {
                    start = null;
                    end = null;
                }
            }
            else if (end != start)
            {
                if (start == cursor)
                {
                    start = end;
                    end = start;
                }
                else if (cursor < start)
                {
                    start = cursor;
                }
                else if (end == cursor)
                {
                    end = start;
                }
                else if (cursor > end)
                {
                    end = cursor;
                }
            }
        }

        // Map a date to screen coordinates inside the calendar
        private (int x, int y) GetCursorPosition(DateTime date)
        {
            // Calendar layout assumptions:
            // - Header is 3 lines
            // - Each cell is 4 chars wide
            // - Each row is 1 line tall

            int headerHeight = 3;
            int cellWidth = 6;

            int dayOfWeek = (int)date.DayOfWeek;
            int firstDayOfMonth = (int)new DateTime(date.Year, date.Month, 1).DayOfWeek;

            int offset = date.Day + firstDayOfMonth - 1;

            int row = offset / 7;
            int col = offset % 7;

            int x = col * cellWidth + 4;
            int y = headerHeight + row + 2;

            return (x, y);
        }
    }
}
