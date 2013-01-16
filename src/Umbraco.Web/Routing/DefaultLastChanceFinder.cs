using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Web;
using System.Xml;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using umbraco.IO;
using umbraco.interfaces;

namespace Umbraco.Web.Routing
{
	/// <summary>
	/// Provides an implementation of <see cref="IPublishedContentFinder"/> to be used as a last chance finder,
	/// that handles backward compatilibty with legacy <c>INotFoundHandler</c>.
	/// </summary>
    internal class DefaultLastChanceFinder : IPublishedContentFinder
    {
		// notes
		//
		// at the moment we load the legacy INotFoundHandler
		// excluding those that have been replaced by proper lookups,
		// and run them.
		//
		// when we finaly obsolete INotFoundHandler, we'll have to move
		// over here code from legacy requestHandler.hande404, which
		// basically uses umbraco.library.GetCurrentNotFoundPageId();
		// which also would need to be refactored / migrated here.
		//
		// the best way to do this would be to create a DefaultLastChanceLookup2
		// that would do everything by itself, and let ppl use it if they
		// want, then make it the default one, then remove this one.

		/// <summary>
		/// Tries to find and assign an Umbraco document to a <c>PublishedContentRequest</c>.
		/// </summary>
		/// <param name="docRequest">The <c>PublishedContentRequest</c>.</param>
		/// <returns>A value indicating whether an Umbraco document was found and assigned.</returns>
		public bool TryFindDocument(PublishedContentRequest docRequest)
        {
			docRequest.PublishedContent = HandlePageNotFound(docRequest);
            return docRequest.HasPublishedContent;
        }

		#region Copied over from presentation.requestHandler

		//FIXME: this is temporary and should be obsoleted

		string GetLegacyUrlForNotFoundHandlers(PublishedContentRequest docRequest)
		{
			// that's not backward-compatible because when requesting "/foo.aspx"
			// 4.9  : url = "foo.aspx"
			// 4.10 : url = "/foo"
			//return docRequest.Uri.AbsolutePath;

			// so we have to run the legacy code for url preparation :-(

			// code from requestModule.UmbracoRewrite
			string tmp = HttpContext.Current.Request.Path.ToLower();
			
			// note: requestModule.UmbracoRewrite also does some confusing stuff
			// with stripping &umbPage from the querystring?! ignored.

			// code from requestHandler.cleanUrl
			string root = Umbraco.Core.IO.SystemDirectories.Root.ToLower();
			if (!string.IsNullOrEmpty(root) && tmp.StartsWith(root))
				tmp = tmp.Substring(root.Length);
			tmp = tmp.TrimEnd('/');
			if (tmp == "/default.aspx")
				tmp = string.Empty;
			else if (tmp == root)
				tmp = string.Empty;

			// code from UmbracoDefault.Page_PreInit
			if (tmp != "" && HttpContext.Current.Request["umbPageID"] == null)
			{
                string tryIntParse = tmp.Replace("/", "").Replace(".aspx", string.Empty);
                int result;
                if (int.TryParse(tryIntParse, out result))
                    tmp = tmp.Replace(".aspx", string.Empty);
			}
			else if (!string.IsNullOrEmpty(HttpContext.Current.Request["umbPageID"]))
			{
				int result;
				if (int.TryParse(HttpContext.Current.Request["umbPageID"], out result))
				{
					tmp = HttpContext.Current.Request["umbPageID"];
				}
			}

			// code from requestHandler.ctor
			if (tmp != "")
				tmp = tmp.Substring(1);

			return tmp;
		}

		IPublishedContent HandlePageNotFound(PublishedContentRequest docRequest)
        {
			LogHelper.Debug<DefaultLastChanceFinder>("Running for url='{0}'.", () => docRequest.Uri.AbsolutePath);
			
			//XmlNode currentPage = null;
			IPublishedContent currentPage = null;
			var url = GetLegacyUrlForNotFoundHandlers(docRequest);

            foreach (var handler in GetNotFoundHandlers())
            {
				if (handler.Execute(url) && handler.redirectID > 0)
                {
                    //currentPage = umbracoContent.GetElementById(handler.redirectID.ToString());
					currentPage = docRequest.RoutingContext.PublishedContentStore.GetDocumentById(
						docRequest.RoutingContext.UmbracoContext,
						handler.redirectID);

                    // FIXME - could it be null?

					LogHelper.Debug<DefaultLastChanceFinder>("Handler '{0}' found node with id={1}.", () => handler.GetType().FullName, () => handler.redirectID);                    

                    //// check for caching
                    //if (handler.CacheUrl)
                    //{
                    //    if (url.StartsWith("/"))
                    //        url = "/" + url;

                    //    var cacheKey = (currentDomain == null ? "" : currentDomain.Name) + url;
                    //    var culture = currentDomain == null ? null : currentDomain.Language.CultureAlias;
                    //    SetCache(cacheKey, new CacheEntry(handler.redirectID.ToString(), culture));

                    //    HttpContext.Current.Trace.Write("NotFoundHandler",
                    //        string.Format("Added to cache '{0}', {1}.", url, handler.redirectID));
                    //}

                    break;
                }
            }

            return currentPage;
        }

        static IEnumerable<Type> _customHandlerTypes = null;
        static readonly object CustomHandlerTypesLock = new object();

        IEnumerable<Type> InitializeNotFoundHandlers()
        {
            // initialize handlers
            // create the definition cache

			LogHelper.Debug<DefaultLastChanceFinder>("Registering custom handlers.");                    

            var customHandlerTypes = new List<Type>();

            var customHandlers = new XmlDocument();
			customHandlers.Load(Umbraco.Core.IO.IOHelper.MapPath(Umbraco.Core.IO.SystemFiles.NotFoundhandlersConfig));

            foreach (XmlNode n in customHandlers.DocumentElement.SelectNodes("notFound"))
            {
                var assemblyName = n.Attributes.GetNamedItem("assembly").Value;

                var typeName = n.Attributes.GetNamedItem("type").Value;
                string ns = assemblyName;
                var nsAttr = n.Attributes.GetNamedItem("namespace");
                if (nsAttr != null && !string.IsNullOrWhiteSpace(nsAttr.Value))
                    ns = nsAttr.Value;

				if (assemblyName == "umbraco" && (ns + "." + typeName) != "umbraco.handle404")
				{
					// skip those that are in umbraco.dll because we have replaced them with IDocumentLookups
					// but do not skip "handle404" as that's the built-in legacy final handler, and for the time
					// being people will have it in their config.
					continue;
				}

				LogHelper.Debug<DefaultLastChanceFinder>("Registering '{0}.{1},{2}'.", () => ns, () => typeName, () => assemblyName);

				Type type = null;
				try
                {
					//TODO: This isn't a good way to load the assembly, its already in the Domain so we should be getting the type
					// this loads the assembly into the wrong assembly load context!!

					var assembly = Assembly.LoadFrom(Umbraco.Core.IO.IOHelper.MapPath(Umbraco.Core.IO.SystemDirectories.Bin + "/" + assemblyName + ".dll"));
                    type = assembly.GetType(ns + "." + typeName);
                }
                catch (Exception e)
                {
					LogHelper.Error<DefaultLastChanceFinder>("Error registering handler, ignoring.", e);                       
                }

                if (type != null)
					customHandlerTypes.Add(type);
            }

        	return customHandlerTypes;
        }

        IEnumerable<INotFoundHandler> GetNotFoundHandlers()
        {
            // instanciate new handlers
            // using definition cache

            lock (CustomHandlerTypesLock)
            {
                if (_customHandlerTypes == null)
                    _customHandlerTypes = InitializeNotFoundHandlers();
            }

            var handlers = new List<INotFoundHandler>();

            foreach (var type in _customHandlerTypes)
            {
                try
                {
                    var handler = Activator.CreateInstance(type) as INotFoundHandler;
                    if (handler != null)
                        handlers.Add(handler);
                }
                catch (Exception e)
                {
					LogHelper.Error<DefaultLastChanceFinder>(string.Format("Error instanciating handler {0}, ignoring.", type.FullName), e);                         
                }
            }

            return handlers;
		}

		#endregion
	}
}