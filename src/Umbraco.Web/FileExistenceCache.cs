using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Umbraco
{
    public static class FileExistenceCache
    {
        private static Dictionary<string, bool> _cache = new Dictionary<string, bool>();

        public static bool FileExists(string virtualPath)
        {
            bool result;
            if (!_cache.TryGetValue(virtualPath, out result))
            {
                result = File.Exists(System.Web.Hosting.HostingEnvironment.MapPath(virtualPath));
                _cache[virtualPath] = result;
            }

            return result;
        }
    }
}
