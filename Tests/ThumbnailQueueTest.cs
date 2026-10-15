using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using EdlChapters.lib;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Logging;
public class QueueProxy : DispatchProxy
{
    public Func<MethodInfo,object[],object> Handler;
    protected override object Invoke(MethodInfo method,object[] args) => Handler(method,args);
    public static T Make<T>(Func<MethodInfo,object[],object> handler) where T:class
    { var value=Create<T,QueueProxy>();((QueueProxy)(object)value).Handler=handler;return value; }
}
public class ChapterImagesTask : IScheduledTask
{
    public string Name=>"Thumbnail extraction";
    public string Key=>"ChapterImagesTask";
    public string Description=>"test";
    public string Category=>"test";
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()=>new TaskTriggerInfo[0];
    public Task Execute(CancellationToken ct,IProgress<double> p)=>Task.CompletedTask;
}
public static class ThumbnailQueueTest
{
    public static void Run()
    {
        var task=new ChapterImagesTask();int count=0;var done=new ManualResetEventSlim();
        var worker=QueueProxy.Make<IScheduledTaskWorker>((m,a)=>m.Name=="get_ScheduledTask"?task:null);
        var tasks=QueueProxy.Make<ITaskManager>((m,a)=> {
            if(m.Name=="get_ScheduledTasks")return new[]{worker};
            if(m.Name=="QueueScheduledTask") {if(!object.ReferenceEquals(a[0],task))throw new Exception("Wrong task");Interlocked.Increment(ref count);done.Set();}
            return null;
        });
        var log=QueueProxy.Make<ILogger>((m,a)=>m.ReturnType==typeof(bool)?(object)false:null);
        bool enabled=false;
        using(var trigger=new ThumbnailTrigger(tasks,log,()=>enabled))
        {
            trigger.Request();if(count!=0)throw new Exception("Disabled trigger ran");
            enabled=true;trigger.Request();trigger.Request();trigger.Request();
            if(!done.Wait(TimeSpan.FromSeconds(10)) || count!=1)throw new Exception("Task request not coalesced/queued");
        }
        Console.WriteLine("PASS thumbnail queue: disabled setting, grouped requests, and correct Emby task selection");
    }
}
