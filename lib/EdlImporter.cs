// EDL import additions, GPL-3.0-or-later, 2026.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;

namespace EdlChapters.lib
{
    public class EdlResult
    {
        public long ItemId { get; set; }
        public string Name { get; set; }
        public string EdlPath { get; set; }
        public string Status { get; set; }
        public bool ThumbnailExtractionRequested { get; set; }
        public int Breaks { get; set; }
        public int RemovedOrdinaryChapters { get; set; }
        public List<ChapterInfo> Chapters { get; set; }
    }
    public class EdlImporter
    {
        private readonly ILibraryManager library;
        private readonly IItemRepository repository;
        private static readonly object Gate = new object();
        public EdlImporter(ILibraryManager l, IItemRepository r) { library=l; repository=r; }
        public static string ResolvePath(string path)
        {
            return path;
        }
        public EdlResult Import(long id, string mediaOverride, bool apply, bool replaceOrdinary)
        {
            lock (Gate)
            {
                var item = library.GetItemById(id);
                if (item == null) throw new ArgumentException("Item not found.");
                if (!(item is MediaBrowser.Controller.Entities.TV.Episode) && !(item is MediaBrowser.Controller.Entities.Movies.Movie))
                    throw new ArgumentException("Select an episode or movie.");
                if (item.IsActiveRecording()) throw new ArgumentException("Recording is still active; wait until it finishes.");
                var path = string.IsNullOrWhiteSpace(mediaOverride) ? ResolvePath(item.Path) : mediaOverride;
                if (string.IsNullOrWhiteSpace(path) || path.Contains("://") || !Path.IsPathRooted(path))
                    throw new ArgumentException("A server-local media path is required, not an SMB URL. Enter Local recording path.");
                path = Path.GetFullPath(path);
                var root = Plugin.Instance.Configuration.EdlRoot;
                if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Set the recordings folder first.");
                root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!path.StartsWith(root, StringComparison.Ordinal)) throw new ArgumentException("Recording is outside the configured recordings folder.");
                // Overrides may resolve SMB metadata, but must refer to the same file name.
                var sourceName = (item.Path ?? "").Replace('\\','/').Split('/').Last();
                if (Path.GetFileName(path) != Uri.UnescapeDataString(sourceName))
                    throw new ArgumentException("Local recording filename does not match this Emby item.");
                if (!File.Exists(path)) throw new FileNotFoundException("Local recording not found.",path);
                var edl = Path.ChangeExtension(path,".edl");
                var info = new FileInfo(edl);
                if (!info.Exists) throw new FileNotFoundException("EDL not found.",edl);
                if (info.Length > 2*1024*1024) throw new ArgumentException("EDL exceeds the 2 MB test limit.");
                var ranges = EdlParser.Parse(File.ReadAllLines(edl));
                if (!item.RunTimeTicks.HasValue || item.RunTimeTicks.Value <= 0) throw new ArgumentException("Item has no known duration; refresh media information first.");
                if (ranges.Any(x=>x.End > item.RunTimeTicks.Value)) throw new ArgumentException("EDL extends beyond the recording duration; check this is the correct EDL.");
                var current = repository.GetChapters(item);
                int removed;
                var retained = EdlChapterPlan.Build(current,ranges,item.RunTimeTicks.Value,replaceOrdinary,out removed);
                bool same = current.Count == retained.Count && current.Zip(retained,(a,b)=>a.Name==b.Name && a.MarkerType==b.MarkerType && a.StartPositionTicks==b.StartPositionTicks).All(x=>x);
                if (apply && !same)
                {
                    repository.SaveChapters(id,retained);
                    var check = repository.GetChapters(item);
                    if (check.Count != retained.Count || !check.Zip(retained,(a,b)=>a.Name==b.Name && a.MarkerType==b.MarkerType && a.StartPositionTicks==b.StartPositionTicks).All(x=>x))
                        throw new InvalidOperationException("Chapter read-back failed. Inspect this item before retrying.");
                }
                bool requestThumbnails=ThumbnailTrigger.ShouldRequest(Plugin.Instance.Configuration.ExtractThumbnailsAfterImport,apply,!same);
                if(requestThumbnails)Plugin.Instance.Thumbnails.Request();
                return new EdlResult { ThumbnailExtractionRequested=requestThumbnails, ItemId=id, Name=item.Name, EdlPath=edl, Breaks=ranges.Count, RemovedOrdinaryChapters=removed,
                    Chapters=retained, Status=same?"Already imported":apply?"Imported and verified":"Preview only" };
            }
        }
    }
}
