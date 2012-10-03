using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Examine;
using Umbraco.Core.Dynamics;
using Umbraco.Core.Models;

namespace Umbraco.Web
{
	/// <summary>
	/// Extension methods for Examine
	/// </summary>
	internal static class ExamineExtensions
	{
		internal static DynamicPublishedContentList ConvertSearchResultToDynamicDocument(
			this IEnumerable<SearchResult> results,
			IPublishedStore store)
		{
			//TODO: The search result has already returned a result which SHOULD include all of the data to create an IPublishedContent, 
			// however thsi is currently not the case: 
			// http://examine.codeplex.com/workitem/10350

			var list = new DynamicPublishedContentList();
			var xd = new XmlDocument();

			foreach (var result in results.OrderByDescending(x => x.Score))
			{
				var doc = store.GetDocumentById(
					UmbracoContext.Current,
					result.Id);
				if (doc == null) continue; //skip if this doesn't exist in the cache				
				doc.Properties.Add(
					new PropertyResult("examineScore", result.Score.ToString(), Guid.Empty, PropertyResultType.CustomProperty));
				var dynamicDoc = new DynamicPublishedContentBase(doc);
				list.Add(dynamicDoc);
			}
			return list;
		}
	}
}