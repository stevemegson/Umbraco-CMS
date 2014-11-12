using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Web.Models;
using Umbraco.Web.Routing;
using Umbraco.Web.Security;

namespace Umbraco.Web.Mvc
{
    public abstract class UmbracoWebFormsTemplatePage<TModel> : ViewPage<TModel>
        where TModel : RenderModel
    {
        /// <summary>
        /// Returns the current UmbracoContext
        /// </summary>
        public UmbracoContext UmbracoContext
        {
            get
            {
                //we should always try to return the context from the data tokens just in case its a custom context and not 
                //using the UmbracoContext.Current.
                //we will fallback to the singleton if necessary.
                if (ViewContext.RouteData.DataTokens.ContainsKey("umbraco-context"))
                {
                    return (UmbracoContext)ViewContext.RouteData.DataTokens.GetRequiredObject("umbraco-context");
                }
                //next check if it is a child action and see if the parent has it set in data tokens
                if (ViewContext.IsChildAction)
                {
                    if (ViewContext.ParentActionViewContext.RouteData.DataTokens.ContainsKey("umbraco-context"))
                    {
                        return (UmbracoContext)ViewContext.ParentActionViewContext.RouteData.DataTokens.GetRequiredObject("umbraco-context");
                    }
                }

                //lastly, we will use the singleton, the only reason this should ever happen is is someone is rendering a page that inherits from this
                //class and are rendering it outside of the normal Umbraco routing process. Very unlikely.
                return UmbracoContext.Current;
            }
        }

        /// <summary>
        /// Returns the current ApplicationContext
        /// </summary>
        public ApplicationContext ApplicationContext
        {
            get { return UmbracoContext.Application; }
        }

        /// <summary>
        /// Returns the current PublishedContentRequest
        /// </summary>
        internal PublishedContentRequest PublishedContentRequest
        {
            get
            {
                //we should always try to return the object from the data tokens just in case its a custom object and not 
                //using the UmbracoContext.Current.
                //we will fallback to the singleton if necessary.
                if (ViewContext.RouteData.DataTokens.ContainsKey("umbraco-doc-request"))
                {
                    return (PublishedContentRequest)ViewContext.RouteData.DataTokens.GetRequiredObject("umbraco-doc-request");
                }
                //next check if it is a child action and see if the parent has it set in data tokens
                if (ViewContext.IsChildAction)
                {
                    if (ViewContext.ParentActionViewContext.RouteData.DataTokens.ContainsKey("umbraco-doc-request"))
                    {
                        return (PublishedContentRequest)ViewContext.ParentActionViewContext.RouteData.DataTokens.GetRequiredObject("umbraco-doc-request");
                    }
                }

                //lastly, we will use the singleton, the only reason this should ever happen is is someone is rendering a page that inherits from this
                //class and are rendering it outside of the normal Umbraco routing process. Very unlikely.
                return UmbracoContext.Current.PublishedContentRequest;
            }
        }

        private UmbracoHelper _helper;
        private MembershipHelper _membershipHelper;

        /// <summary>
        /// Gets an UmbracoHelper
        /// </summary>
        /// <remarks>
        /// This constructs the UmbracoHelper with the content model of the page routed to
        /// </remarks>
        public virtual UmbracoHelper Umbraco
        {
            get
            {
                if (_helper == null)
                {
                    var model = ViewData.Model;
                    var content = model as IPublishedContent;
                    if (content == null && model is IRenderModel)
                        content = ((IRenderModel) model).Content;
                    _helper = content == null
                        ? new UmbracoHelper(UmbracoContext)
                        : new UmbracoHelper(UmbracoContext, content);
                }
                return _helper;
            }
        }

        /// <summary>
        /// Returns the MemberHelper instance
        /// </summary>
        public MembershipHelper Members
        {
            get { return _membershipHelper ?? (_membershipHelper = new MembershipHelper(UmbracoContext)); }
        }

        private object _currentPage;

        /// <summary>
        /// Returns the content as a dynamic object
        /// </summary>
        public dynamic CurrentPage
        {
            get
            {
                // it's invalid to create a DynamicPublishedContent around a null content anyway
                // fixme - should we return null or DynamicNull.Null?
                if (Model == null || Model.Content == null) return null;
                return _currentPage ?? (_currentPage = Model.Content.AsDynamic());
            }
        }
    }
}