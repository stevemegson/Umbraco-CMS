using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;

using Umbraco.Core.Models;
using Umbraco.Web.Routing;

namespace Umbraco.Web.Mvc
{
    public class ContentExistsRouteConstraint : IRouteConstraint
    {
        private static HashSet<string> _failedMatches = new HashSet<string>();

        public static void UncacheFailure(string path )
        {
            _failedMatches.Remove(path);
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            UmbracoContext umbracoContext = UmbracoContext.Current;
            if (umbracoContext == null)
            {
                return false;
            }
            
            if (_failedMatches.Contains(umbracoContext.CleanedUmbracoUrl.GetLeftPart(UriPartial.Path)))
            {
                return false;
            }

            var pcr = new PublishedContentRequest(umbracoContext.CleanedUmbracoUrl, umbracoContext.RoutingContext);
            var engine = new PublishedContentRequestEngine(pcr);
            engine.FindPublishedContent();

            if (pcr.HasPublishedContent)
            {
                return ConstraintShouldMatchForPage(pcr.PublishedContent);
            }
            else
            {
                _failedMatches.Add(umbracoContext.CleanedUmbracoUrl.GetLeftPart(UriPartial.Path));

                return false;
            }
        }

        public virtual bool ConstraintShouldMatchForPage(IPublishedContent c)
        {
            return true;
        }
    }
}