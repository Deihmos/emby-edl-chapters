// GPL-3.0-or-later. Coalesce extraction requests following verified chapter writes.
using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Logging;
namespace EdlChapters.lib
{
    public sealed class ThumbnailTrigger : IDisposable
    {
        private readonly ITaskManager tasks;
        private readonly ILogger log;
        private readonly Func<bool> enabled;
        private readonly Timer timer;
        private readonly object gate = new object();
        private bool disposed;
        public ThumbnailTrigger(ITaskManager tasks, ILogger log, Func<bool> enabled)
        { this.tasks=tasks; this.log=log; this.enabled=enabled; timer=new Timer(Run,null,Timeout.Infinite,Timeout.Infinite); }
        public static bool ShouldRequest(bool enabled,bool apply,bool changed) => enabled && apply && changed;
        public void Request()
        {
            lock(gate)
            {
                if(disposed || !enabled())return;
                // One extraction request after a batch, rather than one task per recording.
                timer.Change(5000,Timeout.Infinite);
            }
        }
        private void Run(object state)
        {
            lock(gate)
            {
                if(disposed || !enabled())return;
                try
                {
                    var worker=tasks.ScheduledTasks.FirstOrDefault(x=>x.ScheduledTask.GetType().Name=="ChapterImagesTask");
                    if(worker==null) { log.Warn("Thumbnail extraction task ChapterImagesTask was not found. Chapters remain imported."); return; }
                    tasks.QueueScheduledTask(worker.ScheduledTask,new TaskOptions());
                    log.Info("Queued Emby's thumbnail extraction task after chapter import. Extraction follows each library's thumbnail settings.");
                }
                catch(Exception ex) {log.Error("Could not queue thumbnail extraction; chapters remain imported: "+ex.Message);}
            }
        }
        public void Dispose() {lock(gate){disposed=true;timer.Dispose();}}
    }
}
