// GPL-3.0-or-later. Trigger chapter import after Comskip has completed.
using System;
using System.IO;
using System.Linq;
using EdlChapters.lib;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Services;
namespace EdlChapters
{
    [Route("/edl_chapters/import-path", "POST")]
    [Authenticated(Roles="Admin")]
    public class ImportEdlPath : IReturn<object>
    {
        public string LocalMediaPath {get;set;}
        public bool Apply {get;set;} = true;
    }
    public class TriggerEndpoint : IService
    {
        private readonly ILibraryManager library;
        private readonly EdlImporter importer;
        public TriggerEndpoint(ILibraryManager manager,IItemRepository repository)
        {library=manager;importer=new EdlImporter(manager,repository);}
        public object Post(ImportEdlPath request)
        {
            var path=request.LocalMediaPath;
            if(string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || path.Contains("://"))
                throw new ArgumentException("Pass the absolute local VIDEO path, not an EDL or SMB URL.");
            path=Path.GetFullPath(path);
            var root=Path.GetFullPath(Plugin.Instance.Configuration.EdlRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!path.StartsWith(root,StringComparison.Ordinal))throw new ArgumentException("Video is outside the configured recordings folder.");
            if(!File.Exists(path))throw new FileNotFoundException("Local video was not found.",path);
            var edl=Path.ChangeExtension(path,".edl");
            if(!File.Exists(edl))return new {Status="Pending",Message="EDL is not present yet."};
            var items=library.GetItemList(new InternalItemsQuery {IsVirtualItem=false,Recursive=true,IncludeItemTypes=new[]{"Movie","Episode"}});
            var matches=items.Where(item=> {
                var candidate=EdlImporter.ResolvePath(item.Path);
                return !string.IsNullOrEmpty(candidate) && Path.IsPathRooted(candidate) && Path.GetFullPath(candidate)==path;
            }).ToList();
            if(matches.Count==0)return new {Status="Pending",Message="Emby has not indexed this recording yet."};
            if(matches.Count!=1)throw new ArgumentException("Multiple Emby items resolve to this path; use the item-ID test page.");
            var match=matches[0];
            if(match.IsActiveRecording())return new {Status="Pending",Message="Emby still marks this recording as active."};
            // An empty EDL is a legitimate no-commercial result. Do not alter existing chapters.
            if(File.ReadAllLines(edl).All(line=>string.IsNullOrWhiteSpace(line)||line.TrimStart().StartsWith("#")))
                return new {Status="No commercials",Message="The EDL has no commercial ranges; no chapters changed."};
            return importer.Import(match.InternalId,path,request.Apply,true);
        }
    }
}
