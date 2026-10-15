// EDL import additions, GPL-3.0-or-later, 2026.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EdlChapters.lib
{
    public class EdlRange { public long Start; public long End; }
    public static class EdlParser
    {
        public static List<EdlRange> Parse(IEnumerable<string> lines)
        {
            var ranges = new List<EdlRange>(); int number = 0;
            foreach (var text in lines)
            {
                number++; var line = text.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                decimal start, end; int action;
                if (parts.Length != 3 || !decimal.TryParse(parts[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out start)
                    || !decimal.TryParse(parts[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out end)
                    || !int.TryParse(parts[2], out action) || start < 0 || end <= start || action < 0 || action > 3)
                    throw new ArgumentException("Invalid EDL line " + number + ". Expected: start-seconds end-seconds action.");
                if (action == 0 || action == 3)
                    ranges.Add(new EdlRange { Start = checked((long)(start * TimeSpan.TicksPerSecond)), End = checked((long)(end * TimeSpan.TicksPerSecond)) });
            }
            ranges.Sort((a,b) => a.Start.CompareTo(b.Start));
            var merged = new List<EdlRange>();
            foreach (var range in ranges)
            {
                if (merged.Count > 0 && range.Start <= merged[merged.Count-1].End)
                    merged[merged.Count-1].End = Math.Max(merged[merged.Count-1].End, range.End);
                else merged.Add(range);
            }
            if (merged.Count == 0) throw new ArgumentException("EDL has no cut/commercial ranges (actions 0 or 3).");
            return merged;
        }
    }
}
