using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Umbraco.Core.Sync;
using System.Net;
using System.Threading;

namespace Umbraco.Core.IO
{
    public class SyncFileSystemWrapper : IFileSystem
    {
		private readonly IFileSystem _wrapped;

        public SyncFileSystemWrapper(string virtualRoot)
        {
            _wrapped = new PhysicalFileSystem(virtualRoot);
        }

		public IEnumerable<string> GetDirectories(string path)
		{
			return _wrapped.GetDirectories(path);
		}

		public void DeleteDirectory(string path)
		{
			_wrapped.DeleteDirectory(path);

            SendMessages("deleteDirectory", GetUrl(path), null);
		}

		public void DeleteDirectory(string path, bool recursive)
		{
			_wrapped.DeleteDirectory(path, recursive);

            SendMessages("deleteDirectory", GetUrl(path), null);
		}

		public bool DirectoryExists(string path)
		{
			return _wrapped.DirectoryExists(path);
		}

		public void AddFile(string path, Stream stream)
		{
            var memoryCopy = new MemoryStream();
            stream.CopyTo(memoryCopy);

            _wrapped.AddFile(path, memoryCopy);

            SendMessages("add", GetUrl(path), memoryCopy);
		}

		public void AddFile(string path, Stream stream, bool overrideIfExists)
		{
            var memoryCopy = new MemoryStream();
            stream.CopyTo(memoryCopy);

			_wrapped.AddFile(path, memoryCopy, overrideIfExists);
            
            SendMessages("add", GetUrl(path), memoryCopy);
		}

		public IEnumerable<string> GetFiles(string path)
		{
			return _wrapped.GetFiles(path);
		}

		public IEnumerable<string> GetFiles(string path, string filter)
		{
			return _wrapped.GetFiles(path, filter);
		}

		public Stream OpenFile(string path)
		{
			return _wrapped.OpenFile(path);
		}

		public void DeleteFile(string path)
		{
			_wrapped.DeleteFile(path);

            SendMessages("deleteFile", GetUrl(path), null);
		}

		public bool FileExists(string path)
		{
			return _wrapped.FileExists(path);
		}

		public string GetRelativePath(string fullPathOrUrl)
		{
			return _wrapped.GetRelativePath(fullPathOrUrl);
		}

		public string GetFullPath(string path)
		{
			return _wrapped.GetFullPath(path);
		}

		public string GetUrl(string path)
		{
			return _wrapped.GetUrl(path);
		}

		public DateTimeOffset GetLastModified(string path)
		{
			return _wrapped.GetLastModified(path);
		}

		public DateTimeOffset GetCreated(string path)
		{
			return _wrapped.GetCreated(path);
		}

        private void SendMessages(string action, string path, Stream stream)
        {
            if (!Umbraco.Core.Configuration.UmbracoSettings.UseDistributedCalls)
            {
                return;
            }

            var servers = ServerRegistrarResolver.Current.Registrar.Registrations;
            
            byte[] streamHash = new byte[]{};
            if ( stream != null )
            {
                stream.Seek(0, SeekOrigin.Begin);
                streamHash = new System.Security.Cryptography.SHA256Managed().ComputeHash(stream);
            }

            var key = System.Web.Security.MachineKey.Encode(Encoding.UTF8.GetBytes(path + "|" + action + "|").Concat(streamHash).ToArray(), System.Web.Security.MachineKeyProtection.Validation);
            string url = "/umbraco/webservices/syncfile.ashx?a=" + action + "&p=" + System.Web.HttpUtility.UrlEncode(path) + "&k=" + key;

            var asyncResultsList = new List<IAsyncResult>();
            foreach (var n in servers)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(new Uri(n.ServerAddress), url).ToString());
                if (stream != null)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    request.Method = "POST";
                    using (Stream s = request.GetRequestStream())
                    {
                        stream.CopyTo(s);
                    }
                }
                asyncResultsList.Add(request.BeginGetResponse(null, request));
            }

            var waitHandlesList = asyncResultsList.Select(x => x.AsyncWaitHandle).ToArray();

            var errorCount = 0;

            WaitHandle.WaitAll(waitHandlesList.ToArray());

            foreach (var t in asyncResultsList)
            {
                try
                {
                    var req = t.AsyncState as HttpWebRequest;
                    req.EndGetResponse(t);
                }
                catch (Exception ex)
                {
                    Logging.LogHelper.Error<SyncFileSystemWrapper>(action + " on path " + path + " failed", ex);
                    errorCount++;
                }
            }

        }
    }
}
