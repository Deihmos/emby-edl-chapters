// EDL import additions, GPL-3.0-or-later, 2026.
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EdlChapters.lib;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
namespace EdlChapters.Tasks
{
    public class TaskImportEdl : IScheduledTask
    {
        private readonly ILibraryManager library;
        private readonly EdlImporter importer;
        private readonly ILogger log;
        public TaskImportEdl(ILibraryManager libraryManager,IItemRepository repository,ILogManager logger)
        { library=libraryManager; importer=new EdlImporter(libraryManager,repository); log=logger.GetLogger("EdlChapters EDL"); }
        public string Name => "Import commercial EDL chapters";
        public string Key => "EdlChaptersImportEdl";
        public string Description => "Import chapters from adjacent EDL files. Enable scheduled imports in the EDL Chapters plugin settings. Set the interval using this task's triggers.";
        public string Category => "EDL Chapters";
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        { return new[] { new TaskTriggerInfo { Type=TaskTriggerInfo.TriggerInterval, IntervalTicks=TimeSpan.FromMinutes(15).Ticks } }; }
        public Task Execute(CancellationToken cancellationToken,IProgress<double> progress)
        {
            return Task.Run(()=> {
                if (!Plugin.Instance.Configuration.EdlAutomatic) { log.Info("Automatic EDL import is disabled."); return; }
                var items=library.GetItemList(new InternalItemsQuery { IsVirtualItem=false, Recursive=true, IncludeItemTypes=new[] {"Movie","Episode"} });
                int done=0, changed=0;
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Plugin.Instance.Configuration.EdlAutomatic) break;
                    try
                    {
                        // Local library paths only. Never search the SMB network or guess file matches.
                        var localPath=EdlImporter.ResolvePath(item.Path);
                        var root=Path.GetFullPath(Plugin.Instance.Configuration.EdlRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                        if (!string.IsNullOrEmpty(localPath) && Path.IsPathRooted(localPath) && Path.GetFullPath(localPath).StartsWith(root,StringComparison.Ordinal)
                            && File.Exists(Path.ChangeExtension(localPath,".edl")))
                        {
                            var result=importer.Import(item.InternalId,null,true,true);
                            if (result.Status=="Imported and verified") { changed++; log.Info("EDL imported for item "+item.InternalId+" : "+item.Name); }
                        }
                    }
                    catch (Exception ex) { log.Warn("EDL item "+item.InternalId+": "+ex.Message); }
                    progress.Report(100.0 * ++done / Math.Max(1,items.Length));
                }
                log.Info("EDL scan complete. Updated items: "+changed);
            },cancellationToken);
        }
    }
}
