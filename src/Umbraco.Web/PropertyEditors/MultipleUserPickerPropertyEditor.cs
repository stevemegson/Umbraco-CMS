using System.ComponentModel;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Web.PropertyEditors
{
    [DataEditor(Constants.PropertyEditors.Aliases.MultipleUserPicker, "Multiple user picker", "entitypicker", ValueType = ValueTypes.String, Group = "People", Icon = Constants.Icons.User)]
    public class MultipleUserPickerPropertyEditor : DataEditor
    {
        public MultipleUserPickerPropertyEditor(ILogger logger)
            : base(logger)
        { }

        protected override IConfigurationEditor CreateConfigurationEditor() => new UserPickerConfiguration(isMultiple:true);
    }
}
