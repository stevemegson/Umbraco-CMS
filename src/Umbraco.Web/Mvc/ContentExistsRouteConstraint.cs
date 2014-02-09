using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;

using Umbraco.Web.Routing;

namespace Umbraco.Web.Mvc
{
    public class ContentExistsRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            UmbracoContext current = UmbracoContext.Current;
            if (current == null)
            {
                return false;
            }
            PublishedContentRequest publishedContentRequest = new PublishedContentRequest(current.CleanedUmbracoUrl, current.RoutingContext);
            PublishedContentRequestBuilder builder = new PublishedContentRequestBuilder(publishedContentRequest);
            builder.LookupDomain();
            builder.LookupDocument();
            return publishedContentRequest.HasNode;
        }
    }
}