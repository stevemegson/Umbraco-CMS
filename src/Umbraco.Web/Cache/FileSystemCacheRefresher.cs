using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.IO;
using Umbraco.Web.SyncFileSystem;
using Umbraco.Core.Persistence;

namespace Umbraco.Web.Cache
{
    public class FileSystemCacheRefresher : CacheRefresherBase<FileSystemCacheRefresher>
    {
        public FileSystemCacheRefresher(AppCaches appCaches) : base(appCaches)
        { }

        public static readonly Guid RefresherTypeId = Guid.Parse("3dbae2fa-6094-4d41-8a9a-8cb0f40c78ec");

        public override string Name => "Cache refresher for SyncFileSystemWrapper";

        protected override FileSystemCacheRefresher This => this;

        public override Guid RefresherUniqueId => RefresherTypeId;

        public override void Refresh(int id)
        {
            Composing.Current.Logger.Info<FileSystemCacheRefresher>("Received file update ID {0}", id);

            using (var scope = Composing.Current.ScopeProvider.CreateScope(autoComplete: true))
            {
                var sql = scope.SqlContext.Sql()
                    .Select("*")
                    .Where<CacheFileDto>(x => x.Id == id);

                var dto = scope.Database.SingleOrDefault<CacheFileDto>(sql);

                if (dto != null)
                {
                    Composing.Current.Logger.Info<FileSystemCacheRefresher>("Update for path {0}, action {1}, file is {2} bytes", dto.Path, dto.Action, dto.Data.Length);

                    string targetPath = System.Web.Hosting.HostingEnvironment.MapPath(dto.Path);
                    var action = (SyncFileSystemWrapper.Action)dto.Action;

                    if (action == SyncFileSystemWrapper.Action.DeleteFile && File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                    else if (action == SyncFileSystemWrapper.Action.DeleteDirectory && Directory.Exists(targetPath))
                    {
                        Directory.Delete(targetPath);
                    }
                    else if (action == SyncFileSystemWrapper.Action.Add)
                    {
                        string directory = Path.GetDirectoryName(targetPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);

                            //FileSystemProviderManager.Current.MediaFileSystem.ResetFolderCounter();
                        }
                        File.WriteAllBytes(targetPath, dto.Data);
                    }
                }
            }
        }
    }
}
