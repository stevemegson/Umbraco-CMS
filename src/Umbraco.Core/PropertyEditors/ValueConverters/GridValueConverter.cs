using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core.Configuration;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration.Grid;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Models;

namespace Umbraco.Core.PropertyEditors.ValueConverters
{
    /// <summary>
    /// This ensures that the grid config is merged in with the front-end value
    /// </summary>
    [DefaultPropertyValueConverter(typeof(JsonValueConverter))] //this shadows the JsonValueConverter
    public class GridValueConverter : JsonValueConverter
    {
        private readonly IGridConfig _config;
        private readonly Lazy<PropertyValueConverterCollection> _propertyValueConverters;

        public GridValueConverter(PropertyEditorCollection propertyEditors, IGridConfig config, Lazy<PropertyValueConverterCollection> propertyValueConverters)
            : base(propertyEditors)
        {
            _config = config;
            _propertyValueConverters = propertyValueConverters;
        }

        public override bool IsConverter(IPublishedPropertyType propertyType)
            => propertyType.EditorAlias.InvariantEquals(Constants.PropertyEditors.Aliases.Grid);

        public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
            => typeof (JToken);

        public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
            => PropertyCacheLevel.Snapshot;

        public override object ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object inter, bool preview)
        {
            if (inter == null) return null;
            var interString = inter.ToString();

            if (interString.DetectIsJson())
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<JObject>(interString);

                    //so we have the grid json... we need to merge in the grid's configuration values with the values
                    // we've saved in the database so that when the front end gets this value, it is up-to-date.

                    var sections = GetArray(obj, "sections");
                    foreach (var section in sections.Cast<JObject>())
                    {
                        var rows = GetArray(section, "rows");
                        foreach (var row in rows.Cast<JObject>())
                        {
                            var areas = GetArray(row, "areas");
                            foreach (var area in areas.Cast<JObject>())
                            {
                                var controls = GetArray(area, "controls");
                                foreach (var control in controls.Cast<JObject>())
                                {
                                    var editor = control.Value<JObject>("editor");
                                    if (editor != null)
                                    {
                                        var alias = editor.Value<string>("alias");
                                        if (alias.IsNullOrWhiteSpace() == false)
                                        {
                                            //find the alias in config
                                            var found = _config.EditorsConfig.Editors.FirstOrDefault(x => x.Alias == alias);
                                            if (found != null)
                                            {
                                                //add/replace the editor value with the one from config

                                                var serialized = new JObject();
                                                serialized["name"] = found.Name;
                                                serialized["alias"] = found.Alias;
                                                serialized["view"] = found.View;
                                                serialized["render"] = found.Render;
                                                serialized["icon"] = found.Icon;
                                                serialized["config"] = JObject.FromObject(found.Config);

                                                control["editor"] = serialized;
                                            }

                                            if (found.EditorAlias != null)
                                            {
                                                var propertyValueConverter = GetPropertyValueConverter(found.EditorAlias);
                                                if ( propertyValueConverter != null)
                                                {
                                                    var interValue = propertyValueConverter.ConvertSourceToIntermediate(null, null, control.Value<string>("value"), preview);
                                                    var objValue = propertyValueConverter.ConvertIntermediateToObject(null, null, PropertyCacheLevel.None, interValue, preview);

                                                    if (objValue is IHtmlString)
                                                        objValue = objValue.ToString();
                                                    
                                                    control["value"] = (objValue == null) ? null : JToken.FromObject(objValue);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return obj;
                }
                catch (Exception ex)
                {
                    Current.Logger.Error<GridValueConverter>(ex, "Could not parse the string '{JsonString}' to a json object", interString);
                }
            }

            //it's not json, just return the string
            return interString;
        }

        private JArray GetArray(JObject obj, string propertyName)
        {
            JToken token;
            if (obj.TryGetValue(propertyName, out token))
            {
                var asArray = token as JArray;
                return asArray ?? new JArray();
            }
            return new JArray();
        }

        private IPropertyValueConverter GetPropertyValueConverter(string editorAlias)
        {
            var propertyType = new DummyPublishedPropertyType(editorAlias);

            IPropertyValueConverter result = null;
            var isdefault = false;

            foreach (var converter in _propertyValueConverters.Value)
            {
                if (!converter.IsConverter(propertyType))
                    continue;

                if (result == null)
                {
                    result = converter;
                    isdefault = _propertyValueConverters.Value.IsDefault(converter);
                    continue;
                }

                if (isdefault)
                {
                    if (_propertyValueConverters.Value.IsDefault(converter))
                    {
                        // previous was default, and got another default
                        if (_propertyValueConverters.Value.Shadows(result, converter))
                        {
                            // previous shadows, ignore
                        }
                        else if (_propertyValueConverters.Value.Shadows(converter, result))
                        {
                            // shadows previous, replace
                            result = converter;
                        }
                        else
                        {
                            // no shadow - bad                            
                        }
                    }
                    else
                    {
                        // previous was default, replaced by non-default
                        result = converter;
                        isdefault = false;
                    }
                }
                else
                {
                    if (_propertyValueConverters.Value.IsDefault(converter))
                    {
                        // previous was non-default, ignore default
                    }
                    else
                    {
                        // previous was non-default, and got another non-default - bad
                    }
                }
            }

            return result;
        }

        private class DummyPublishedPropertyType : IPublishedPropertyType
        {
            private readonly string _editorAlias;

            public DummyPublishedPropertyType(string editorAlias)
            {
                _editorAlias = editorAlias;
            }

            public string EditorAlias => _editorAlias;

            public IPublishedContentType ContentType => null;

            public PublishedDataType DataType => null;

            public string Alias => null;


            public bool IsUserProperty => false;

            public ContentVariation Variations => ContentVariation.Nothing;

            public PropertyCacheLevel CacheLevel => PropertyCacheLevel.Unknown;

            public Type ModelClrType => typeof(object);

            public Type ClrType => typeof(object);

            public object ConvertInterToObject(IPublishedElement owner, PropertyCacheLevel referenceCacheLevel, object inter, bool preview) => inter;

            public object ConvertInterToXPath(IPublishedElement owner, PropertyCacheLevel referenceCacheLevel, object inter, bool preview) => inter;

            public object ConvertSourceToInter(IPublishedElement owner, object source, bool preview) => source;

            public bool? IsValue(object value, PropertyValueLevel level) => null;
        }
    }
}
