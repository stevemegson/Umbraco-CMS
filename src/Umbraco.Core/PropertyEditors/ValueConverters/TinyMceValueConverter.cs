using System;
using System.Linq;
using System.Web;
using Umbraco.Core.Cache;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Models.Rdbms;
using Umbraco.Core.Persistence;

namespace Umbraco.Core.PropertyEditors.ValueConverters
{
	/// <summary>
	/// Value converter for the RTE so that it always returns IHtmlString so that Html.Raw doesn't have to be used.
	/// </summary>
    // PropertyCacheLevel.Content is ok here because that version of RTE converter does not parse {locallink} nor executes macros
    [PropertyValueType(typeof(IHtmlString))]
    [PropertyValueCache(PropertyCacheValue.All, PropertyCacheLevel.Content)]
    public class TinyMceValueConverter : PropertyValueConverterBase
	{
        public override bool IsConverter(PublishedPropertyType propertyType)
        {
            return Guid.Parse(Constants.PropertyEditors.TinyMCEv3).Equals(propertyType.PropertyEditorGuid)
                || Guid.Parse(Constants.PropertyEditors.NoXmlTinyMCE).Equals(propertyType.PropertyEditorGuid);
        }

        public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
        {
            if ( source is string && source.ToString().StartsWith(">>"))
            {
                return LoadValueFromDatabase(Int32.Parse(source.ToString().Substring(2)));
            }

            // in xml a string is: string
            // in the database a string is: string
            // default value is: null
            return source;
        }

        public override object ConvertSourceToObject(PublishedPropertyType propertyType, object source, bool preview)
        {
            // source should come from ConvertSource and be a string (or null) already
            return new HtmlString(source == null ? string.Empty : (string)source);
        }

        public override object ConvertSourceToXPath(PublishedPropertyType propertyType, object source, bool preview)
        {
            // source should come from ConvertSource and be a string (or null) already
            return source;
        }

        protected string LoadValueFromDatabase(int id)
        {
            return ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem<string>(
                "cmsPropertyData-" + id.ToString(), () =>
                {

                var sql = new Sql();
                sql.Select("*")
                   .From<PropertyDataDto>()
                   .Where<PropertyDataDto>(x => x.Id == id);
                var dto = ApplicationContext.Current.DatabaseContext.Database.Fetch<PropertyDataDto>(sql).FirstOrDefault();

                if (dto != null)
                {
                    return dto.Text;
                }

                return null;
            }, TimeSpan.FromHours(1), true);
        }
    }
}