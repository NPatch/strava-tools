using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectreConsoleExtensions
{
    public readonly struct DateRange
    {
        public DateTime Start { get; }
        public DateTime End { get; }

        public DateRange()
        {
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
        }

        public DateRange(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
    }

    public static class CalendarExtensions
    {
        public static DateRange PickCalendarRange(this IAnsiConsole console, Calendar calendar)
        {
            var picker = new CalendarRangePicker(console, calendar);
            return picker.Run();
        }
    }

    internal sealed class CalendarRangePicker
    {
        private readonly IAnsiConsole _console;
        private readonly Calendar _calendar;

        private DateTime _cursor;
        private DateTime? _start;
        private DateTime? _end;

        private Dictionary<int, (int x, int y)> _dayPositions = new();

        public CalendarRangePicker(IAnsiConsole console, Calendar calendar)
        {
            _console = console;
            _calendar = calendar;

            _cursor = new DateTime(calendar.Year, calendar.Month, 1);
        }

        public DateRange Run()
        {
            while (true)
            {
                Render();

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        MoveCursor(-1);
                        break;
                    case ConsoleKey.RightArrow:
                        MoveCursor(+1);
                        break;
                    case ConsoleKey.UpArrow:
                        MoveCursor(-7);
                        break;
                    case ConsoleKey.DownArrow:
                        MoveCursor(+7);
                        break;

                    case ConsoleKey.Spacebar:
                        HandleSelection();
                        if (_start != null && _end != null)
                            return new DateRange(_start.Value, _end.Value);
                        break;

                    case ConsoleKey.Escape:
                        throw new OperationCanceledException("Calendar range selection cancelled.");
                }
            }
        }

        private void MoveCursor(int days)
        {
            var next = _cursor.AddDays(days);

            if (next.Month == _calendar.Month && next.Year == _calendar.Year)
                _cursor = next;
        }

        private void HandleSelection()
        {
            if (_start == null)
            {
                _start = _cursor;
            }
            else if (_end == null)
            {
                if (_cursor < _start.Value)
                {
                    _end = _start;
                    _start = _cursor;
                }
                else
                {
                    _end = _cursor;
                }
            }
        }

        private void Render()
        {
            _console.Clear();

            // Apply highlights
            if (_start != null)
                _calendar.AddCalendarEvent(_start.Value);

            if (_end != null)
            {
                var d = _start.Value;
                while (d <= _end.Value)
                {
                    _calendar.AddCalendarEvent(d);
                    d = d.AddDays(1);
                }
            }

            //// Render calendar into segments
            //var ctx = new LiveDisplayContext(_console.Profile);
            //var segments = _calendar.Render(ctx).ToArray();

            //// Convert segments to a 2D buffer
            //var buffer = SegmentToBuffer(segments);

            //// Extract day positions
            //_dayPositions = ExtractDayPositions(buffer);

            //// Write calendar normally
            //_console.Write(_calendar);

            //// Draw cursor
            //if (_dayPositions.TryGetValue(_cursor.Day, out var pos))
            //{
            //    _console.Cursor.SetPosition(pos.x, pos.y);
            //    _console.Markup("[red]■[/]");
            //}

            //// Instructions
            //_console.Cursor.SetPosition(0, pos.y + 3);
            //_console.Markup("[grey]Arrows move | Space selects | Esc cancels[/]");
        }

        private static char[][] SegmentToBuffer(Segment[] segments)
        {
            var lines = new List<char[]>();

            foreach (var seg in segments)
            {
                var text = seg.Text;
                var parts = text.Split('\n');

                foreach (var p in parts)
                    lines.Add(p.ToCharArray());
            }

            return lines.ToArray();
        }

        private static Dictionary<int, (int x, int y)> ExtractDayPositions(char[][] buffer)
        {
            var dict = new Dictionary<int, (int x, int y)>();

            for (int y = 0; y < buffer.Length; y++)
            {
                var line = buffer[y];
                for (int x = 0; x < line.Length - 1; x++)
                {
                    // Look for day numbers: 1–31
                    if (char.IsDigit(line[x]))
                    {
                        // Try 1-digit
                        if (x + 1 < line.Length && !char.IsDigit(line[x + 1]))
                        {
                            int day = line[x] - '0';
                            dict[day] = (x, y);
                        }
                        // Try 2-digit
                        else if (x + 1 < line.Length && char.IsDigit(line[x + 1]))
                        {
                            int day = (line[x] - '0') * 10 + (line[x + 1] - '0');
                            if (day >= 1 && day <= 31)
                                dict[day] = (x, y);
                        }
                    }
                }
            }

            return dict;
        }
    }
}