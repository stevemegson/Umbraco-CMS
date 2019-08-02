using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Routing;

using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Routing;

namespace Umbraco.Web.Mvc
{
    public class ContentExistsRouteConstraint : IRouteConstraint
    {
        private static HashSet<string> _failedMatches = new HashSet<string>();
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public static void UncacheFailure(string path)
        {
            using (new WriteLock(_lock))
            {
                _failedMatches.Remove(path);
            }
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var umbracoContext = Composing.Current.UmbracoContext;
            if (umbracoContext == null)
            {
                return false;
            }

            if (!umbracoContext.InPreviewMode)
            {
                using (new ReadLock(_lock))
                {
                    if (_failedMatches.Contains(umbracoContext.CleanedUmbracoUrl.GetLeftPart(UriPartial.Path)))
                    {
                        return false;
                    }
                }
            }

            var publishedRouter = Composing.Current.Factory.GetInstance<IPublishedRouter>();            
            var request = publishedRouter.CreateRequest(umbracoContext);
            publishedRouter.PrepareRequest(request);

            if ( request.HasPublishedContent)
            {
                return ConstraintShouldMatchForPage(request.PublishedContent);
            }
            else if ( request.IsRedirect)
            {
                return true;
            }
            else
            {
                if (!umbracoContext.InPreviewMode)
                {
                    using (new WriteLock(_lock))
                    {
                        _failedMatches.Add(umbracoContext.CleanedUmbracoUrl.GetLeftPart(UriPartial.Path));
                    }
                }

                return false;
            }
        }

        public virtual bool ConstraintShouldMatchForPage(IPublishedContent c)
        {
            return true;
        }
    }
}
