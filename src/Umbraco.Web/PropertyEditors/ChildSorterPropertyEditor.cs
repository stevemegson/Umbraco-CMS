using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Web.PropertyEditors
{
    [DataEditor(
        "Umbraco.ChildSorter",
        "Child Sorter",
        "childsorter",
        ValueType = ValueTypes.Text,
        Group = Constants.PropertyEditors.Groups.Pickers,
        Icon = "icon-shuffle")]
    public class ChildSorterPropertyEditor : DataEditor
    {
        public ChildSorterPropertyEditor(ILogger logger)
            : base(logger)
        { }        
    }
}
