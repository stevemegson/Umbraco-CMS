using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace umbraco.presentation.webservices
{
    public class SyncFile : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            string path = context.Request.QueryString["p"];
            string action = context.Request.QueryString["a"];
            string suppliedKey = context.Request.QueryString["k"];

            MemoryStream stream = null;
            if (context.Request.HttpMethod == "POST")
            {
                stream = new MemoryStream();
                context.Request.InputStream.CopyTo(stream);
            }

            if( ! ValidateRequest(path, action, stream, suppliedKey))
            {
                context.Response.StatusCode = 403;
                context.Response.StatusDescription = "Forbidden";
                
                return;
            }

            string targetPath = context.Server.MapPath(path);
            if ( action == "deleteFile" && File.Exists(targetPath ))
            {
                File.Delete(targetPath);
            }
            else if (action == "deleteDirectory" && Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath);
            }
            else if (action == "add" && stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);

                string directory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                using (var writer = File.Create(targetPath))
                {
                    stream.CopyTo(writer);
                }
            }
        }

        private bool ValidateRequest(string path, string action, Stream stream, string suppliedKey)
        {
            byte[] streamHash = new byte[] { };
            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);
                streamHash = new System.Security.Cryptography.SHA256Managed().ComputeHash(stream);
            }

            var expectedKey = System.Web.Security.MachineKey.Encode(Encoding.UTF8.GetBytes(path + "|" + action + "|").Concat(streamHash).ToArray(), System.Web.Security.MachineKeyProtection.Validation);

            return suppliedKey == expectedKey;
        }
    }
}
