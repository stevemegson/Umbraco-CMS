using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace umbraco.cms.presentation.Trees
{
    /// <summary>
    /// An empty tree with no functionality. This gets loaded when the requested tree cannot be loaded with the type specified.
    /// Should not be used directly in code.
    /// </summary>
    public class NullTree : BaseTree
    {

		public NullTree(string application) : base(application) { }

        public override void RenderJS(ref System.Text.StringBuilder Javascript) { }

        public override void Render(ref XmlTree tree)
        {
            XmlTreeNode xNode = XmlTreeNode.Create(this);
            xNode.Text = "Error";
            xNode.Menu = null;
            tree.Add(xNode);
        }

        protected override void CreateRootNode(ref XmlTreeNode rootNode)
        {            
            rootNode.Menu = null;
            rootNode.Text = "Error";
        }

        public override string TreeAlias
        {
            get
            {
                return "NullTree";
            }
        }
    }

}
