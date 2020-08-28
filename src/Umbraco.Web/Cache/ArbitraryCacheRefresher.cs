using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

using Umbraco.Core.Cache;
using Umbraco.Core.Logging;

namespace Umbraco.Web.Cache
{
    public class ArbitraryCacheRefresher : JsonCacheRefresherBase<ArbitraryCacheRefresher>
    {
        public ArbitraryCacheRefresher(AppCaches appCaches) : base(appCaches)
        { }

        public static readonly Guid RefresherTypeId = Guid.Parse("a65e1f93-dd89-4844-b729-bf524f101277");

        public override string Name => "Cache refresher for arbitrary keys";        

        protected override ArbitraryCacheRefresher This => this;

        public override Guid RefresherUniqueId => RefresherTypeId;

        public override void Refresh(string jsonPayload)
        {
            if (jsonPayload.EndsWith("*"))
            {
                string prefix = jsonPayload.TrimEnd('*');
                foreach (var key in HttpRuntime.Cache.OfType<DictionaryEntry>()
                                                    .Select(de => de.Key as string)
                                                    .Where(k => k.StartsWith(prefix)))
                {
                    Composing.Current.Logger.Debug<ArbitraryCacheRefresher>("Removed cache key {0}", key);
                    HttpRuntime.Cache.Remove(key);
                }
            }
            else
            {
                Composing.Current.Logger.Debug<ArbitraryCacheRefresher>("Removed cache key {0}", jsonPayload);
                HttpRuntime.Cache.Remove(jsonPayload);
            }

            base.Refresh(jsonPayload);
        }
    }
}
