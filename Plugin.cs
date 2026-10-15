using System;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Logging;
using EdlChapters.lib;
using System.IO;
using MediaBrowser.Model.Drawing;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
namespace EdlChapters
{
    public class Configuration : BasePluginConfiguration
    {
        public string EdlRoot { get; set; } = "/mnt/Medialibrary/Recorded TV";
        public bool ExtractThumbnailsAfterImport { get; set; } = false;
        public bool EdlAutomatic { get; set; } = false;
    }
    public class Plugin : BasePlugin<Configuration>, IHasWebPages, IHasThumbImage, IDisposable
    {
        public Stream GetThumbImage() => GetType().Assembly.GetManifestResourceStream("EdlChapters.Media.icon.png");
        public ImageFormat ThumbImageFormat => ImageFormat.Png;
        public static Plugin Instance { get; private set; }
        public override string Name => "EDL Chapters";
        public override string Description => "Import adjacent commercial EDL files as Emby database chapters. Includes post-processing trigger.";
        public override Guid Id => new Guid("d7bc31d5-923f-4356-ace3-6b155f21d5bd");
        public ThumbnailTrigger Thumbnails { get; private set; }
        public Plugin(IApplicationPaths paths, IXmlSerializer serializer, ITaskManager tasks, ILogManager logs) : base(paths,serializer)
        {
            Instance=this;
            Thumbnails=new ThumbnailTrigger(tasks,logs.GetLogger("EDL Chapters"),()=>Configuration.ExtractThumbnailsAfterImport);
        }
        public void Dispose() { Thumbnails.Dispose(); }
        public IEnumerable<PluginPageInfo> GetPages() => new[] {
            new PluginPageInfo {Name="edlchapters",DisplayName="EDL Chapters",EmbeddedResourcePath="EdlChapters.Pages.edl.html",EnableInMainMenu=false, IsMainConfigPage=true},
            new PluginPageInfo {Name="edlchapters.js",EmbeddedResourcePath="EdlChapters.Pages.edl.js"}
        };
    }
}
