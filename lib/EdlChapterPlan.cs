// EDL import additions, GPL-3.0-or-later, 2026.
using System;
using System.Linq;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;
namespace EdlChapters.lib
{
    public static class EdlChapterPlan
    {
        public static List<ChapterInfo> Build(List<ChapterInfo> current, List<EdlRange> ranges, long duration, bool replaceOrdinary, out int removed)
        {
            var retained = new List<ChapterInfo>(); removed = 0;
            foreach (var c in current)
            {
                var generated = c.MarkerType == MarkerType.Chapter && (c.Name == "Advertisement" || c.Name == "Programme");
                var affected = ranges.Any(x=> c.StartPositionTicks >= x.Start && c.StartPositionTicks <= x.End);
                if (affected && c.MarkerType != MarkerType.Chapter)
                    throw new ArgumentException("An intro/credits marker overlaps a commercial range; nothing changed.");
                if (generated) continue;
                if (affected)
                {
                    if (!replaceOrdinary) throw new ArgumentException("Ordinary chapters overlap a break. Enable Replace ordinary chapter points to import; no backups are created.");
                    removed++;
                }
                else retained.Add(c);
            }
            foreach (var range in ranges)
            {
                retained.Add(new ChapterInfo { Name="Advertisement", MarkerType=MarkerType.Chapter, StartPositionTicks=range.Start });
                if (range.End <= duration)
                    retained.Add(new ChapterInfo { Name="Programme", MarkerType=MarkerType.Chapter, StartPositionTicks=range.End });
            }
            if (ranges[0].Start > 0 && !retained.Any(x=>x.StartPositionTicks==0))
                retained.Add(new ChapterInfo { Name="Programme",MarkerType=MarkerType.Chapter,StartPositionTicks=0 });
            retained.Sort((a,b)=>a.StartPositionTicks.CompareTo(b.StartPositionTicks));
            return retained;
        }
    }
}
