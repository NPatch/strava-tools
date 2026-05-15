using System;

namespace StravaTools
{
    public static class CommonUtilities
    {
        public static DateTime GetTimestamp(long t)
        {
            return DateTimeOffset.FromUnixTimeSeconds(t).DateTime;
        }

        public static long GetTimestampFromCustomEpoch(DateTime dt, DateTime epoch)
        {
            return (long)(dt - epoch).TotalSeconds;
        }

        public static bool IsTimestampInRange(DateTime dt, DateTime start, DateTime end)
        {
            return dt >= start && dt <= end;
        }

        public static bool IsTimestampInRange(long dt, long start, long end)
        {
            return dt >= start && dt <= end;
        }

        public static string GetDistanceInKm(float distance)
        {
            int km;
            km = (int)(distance / 1000.0f);
            distance -= km * 1000;
            distance = (float)Math.Floor(distance);

            return $"{km}.{(distance * 100.0f) / 100.0f}km\t";
        }

        public static string GetTotalSecondsInDuration(int total_seconds)
        {
            int h, m, s;

            h = total_seconds / 3600;
            total_seconds -= (h * 3600);
            m = total_seconds / 60;
            total_seconds -= (m * 60);
            s = total_seconds;

            return $"{h}:{m}:{s}";
        }

        public static int GetMinOrNonzero(int a, int b)
        {
            if (a == 0 && b == 0) return 0;
            else if (a == 0 && b != 0) return b;
            else if (a != 0 && b == 0) return a;
            else return int.Min(a, b);
        }
    }
}
