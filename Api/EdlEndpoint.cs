// EDL import additions, GPL-3.0-or-later, 2026.
using System;
using EdlChapters.lib;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
namespace EdlChapters
{
    [Route("/edl_chapters/import", "POST")]
    [Authenticated(Roles = "Admin")]
    public class ImportEdl : IReturn<EdlResult>
    {
        public long ItemId { get; set; }
        public string LocalMediaPath { get; set; }
        public bool Apply { get; set; }
    }
    [Route("/edl_chapters/settings", "GET")]
    [Authenticated(Roles = "Admin")]
    public class GetEdlSettings : IReturn<object> { }
    [Route("/edl_chapters/settings", "POST")]
    [Authenticated(Roles = "Admin")]
    public class SaveEdlSettings : IReturn<object>
    {
        public bool ExtractThumbnailsAfterImport { get; set; }
        public string Root { get; set; }
        public bool Automatic { get; set; }
    }
    public class EdlEndpoint : IService
    {
        private readonly EdlImporter importer;
        public EdlEndpoint(ILibraryManager library, IItemRepository repository) { importer=new EdlImporter(library,repository); }
        public object Post(ImportEdl request) { return importer.Import(request.ItemId,request.LocalMediaPath,request.Apply,true); }
        public object Get(GetEdlSettings request)
        {
            var c=Plugin.Instance.Configuration;
            return new { ExtractThumbnailsAfterImport=c.ExtractThumbnailsAfterImport, Root=c.EdlRoot, Automatic=c.EdlAutomatic };
        }
        public object Post(SaveEdlSettings request)
        {
            if (string.IsNullOrWhiteSpace(request.Root) || !System.IO.Path.IsPathRooted(request.Root) || request.Root.Contains("://"))
                throw new ArgumentException("Use an absolute server-local recordings folder.");
            var c=Plugin.Instance.Configuration;
            c.ExtractThumbnailsAfterImport=request.ExtractThumbnailsAfterImport; c.EdlRoot=request.Root; c.EdlAutomatic=request.Automatic;
            Plugin.Instance.UpdateConfiguration(c);
            return Get(new GetEdlSettings());
        }
    }
}
