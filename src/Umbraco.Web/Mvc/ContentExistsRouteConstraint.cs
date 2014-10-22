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
        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            UmbracoContext umbracoContext = UmbracoContext.Current;
            if (umbracoContext == null)
            {
                return false;
            }

            var pcr = new PublishedContentRequest(umbracoContext.CleanedUmbracoUrl, umbracoContext.RoutingContext);
            umbracoContext.PublishedContentRequest = pcr;
            pcr.Prepare();

            return pcr.HasPublishedContent && ConstraintShouldMatchForPage(pcr.PublishedContent);
        }

        public virtual bool ConstraintShouldMatchForPage(IPublishedContent c)
        {
            return true;
        }
    }
}