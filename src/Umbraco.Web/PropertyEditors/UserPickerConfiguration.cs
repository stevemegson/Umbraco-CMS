using System.Collections.Generic;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Web.PropertyEditors
{
    public class UserPickerConfiguration : ConfigurationEditor
    {
        private bool _isMultiple = false;

        public UserPickerConfiguration()
        {
        }

        public UserPickerConfiguration(bool isMultiple)
        {
            _isMultiple = isMultiple;
        }

        public override IDictionary<string, object> DefaultConfiguration => new Dictionary<string, object>
        {
            {"entityType", "User"},
            {"multiple", _isMultiple}
        };
    }
}
