using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Core;

namespace umbraco.editorControls.tinyMCE3
{
    public class NoXmlTinyMCE3dataType : tinyMCE3dataType
    {
        public override Guid Id
        {
            get { return new Guid(Constants.PropertyEditors.NoXmlTinyMCE); }
        }

        public override string DataTypeName
        {
            get { return "TinyMCE v3 wysiwyg, No XML"; }
        }
    }
}
