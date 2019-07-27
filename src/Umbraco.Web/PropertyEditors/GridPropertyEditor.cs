using System.Linq;
using Umbraco.Core.Logging;
using Examine;
using Lucene.Net.Documents;
using Umbraco.Core;
using Umbraco.Core.PropertyEditors;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Umbraco.Core.Configuration.Grid;
using System;

namespace Umbraco.Web.PropertyEditors
{
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.Editors;
    using Umbraco.Core.Services;
    using Examine = global::Examine;

    /// <summary>
    /// Represents a grid property and parameter editor.
    /// </summary>
    [DataEditor(Constants.PropertyEditors.Aliases.Grid, "Grid layout", "grid", HideLabel = true, ValueType = ValueTypes.Json, Group="rich content", Icon="icon-layout")]
    public class GridPropertyEditor : DataEditor
    {
        private readonly Lazy<PropertyEditorCollection> _propertyEditors;
        private readonly IGridConfig _gridConfig;

        public GridPropertyEditor(ILogger logger, Lazy<PropertyEditorCollection> propertyEditors, IGridConfig gridConfig)
            : base(logger)
        {
            _propertyEditors = propertyEditors;
            _gridConfig = gridConfig;
        }

        public override IPropertyIndexValueFactory PropertyIndexValueFactory => new GridPropertyIndexValueFactory();

        /// <summary>
        /// Overridden to ensure that the value is validated
        /// </summary>
        /// <returns></returns>
        protected override IDataValueEditor CreateValueEditor() => new GridPropertyValueEditor(Attribute, _propertyEditors.Value, _gridConfig);

        protected override IConfigurationEditor CreateConfigurationEditor() => new GridConfigurationEditor();

        internal class GridPropertyValueEditor : DataValueEditor
        {
            private readonly IGridConfig _gridConfig;
            private readonly PropertyEditorCollection _propertyEditors;

            public GridPropertyValueEditor(DataEditorAttribute attribute, PropertyEditorCollection propertyEditors, IGridConfig gridConfig)
                : base(attribute)
            {
                _propertyEditors = propertyEditors;
                _gridConfig = gridConfig;
            }

            public override object FromEditor(ContentPropertyData editorValue, object currentValue)
            {
                if (editorValue.Value == null || string.IsNullOrWhiteSpace(editorValue.Value.ToString()))
                    return null;

                var obj = JsonConvert.DeserializeObject<JObject>(editorValue.Value.ToString());
                if (obj == null)
                    return null;

                try
                {
                    return TransformGridValues(obj, (value, editor) =>
                    {
                        var tempConfig = editor.GetConfigurationEditor().DefaultConfigurationObject;
                        var tempPropData = new ContentPropertyData(value, tempConfig);
                        return editor.GetValueEditor().FromEditor(tempPropData, value);
                    });
                }
                catch
                {
                    return editorValue.Value;
                }
            }

            public override object ToEditor(Property property, IDataTypeService dataTypeService, string culture = null, string segment = null)
            {
                var val = property.GetValue(culture, segment);
                if (val == null || string.IsNullOrWhiteSpace(val.ToString()))
                    return string.Empty;

                var obj = JsonConvert.DeserializeObject<JObject>(val.ToString());
                if (obj == null)
                    return string.Empty;

                try
                {
                    var transformed = TransformGridValues(obj, (value, editor) =>
                    {
                        var tempProp = new Property(new PropertyType(new DataType(editor, -1)));
                        tempProp.SetValue(value);
                        return editor.GetValueEditor().ToEditor(tempProp, dataTypeService);
                    });

                    return transformed;
                }
                catch
                {
                    return val;
                }
            }

            private JObject TransformGridValues(JObject obj, Func<string, IDataEditor, object> transform)
            {
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
                                        var editorConfig = _gridConfig.EditorsConfig.Editors.FirstOrDefault(x => x.Alias == alias);
                                        if (editorConfig?.EditorAlias != null && _propertyEditors.TryGet(editorConfig.EditorAlias, out var propertyEditor))
                                        {
                                            var newValue = transform(control.Value<string>("value"), propertyEditor);
                                            control["value"] = (newValue == null) ? null : JToken.FromObject(newValue);   
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return obj;
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
        }
    }
}
