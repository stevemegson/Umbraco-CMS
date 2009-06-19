using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.IO;
using umbraco;
using umbraco.BusinessLogic;
using System.Collections.Generic;
using umbraco.BasePages;
using umbraco.BusinessLogic.Actions;
using umbraco.interfaces;
using umbraco.cms.presentation.Trees;
using System.Xml.XPath;

namespace umbraco.cms.presentation.user
{

	public partial class PermissionEditor : UmbracoEnsuredPage
    {

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);

			if (!IsPostBack)
			{
				TreeService treeSvc = new TreeService();
				treeSvc.App = TreeDefinitionCollection.Instance.FindTree<loadContent>().Tree.ApplicationAlias;
				treeSvc.ShowContextMenu = false;
				treeSvc.IsDialog = true;

				JTree.SetTreeService(treeSvc);
			}
		}

        protected void Page_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Request.QueryString["id"]))
                return;

            CheckUser(Request.QueryString["id"]);

            ImageButton save = pnlUmbraco.Menu.NewImageButton();
            save.ID = "btnSave";
            save.ImageUrl = GlobalSettings.Path + "/images/editor/save.gif";
			save.OnClientClick = "SavePermissions(); return false;";

            nodePermissions.UserID = Convert.ToInt32(Request.QueryString["id"]);
            pnlUmbraco.Text = ui.Text("user", "userPermissions");
            pnl1.Text = ui.Text("user", "permissionSelectPages");

			if (!IsPostBack)
			{	
				ClientTools cTools = new ClientTools(this);
				cTools.SetActiveTreeType(TreeDefinitionCollection.Instance.FindTree<Trees.UserPermissions>().Tree.Alias)
					.SyncTree(Request.QueryString["id"], false);
			}
        }

        /// <summary>
        /// Since Umbraco stores users in cache, we'll use this method to retreive our user object by the selected id
        /// </summary>
        protected umbraco.BusinessLogic.User UmbracoUser
        {
            get
            {
                return BusinessLogic.User.GetUser(Convert.ToInt32(Request.QueryString["id"]));
            }
        }
      
        /// <summary>
        /// Makes sure the user exists with the id specified
        /// </summary>
        /// <param name="strID"></param>
        private void CheckUser(string strID)
        {
            int id;
            bool parsed = false;
            umbraco.BusinessLogic.User oUser = null;
            if (parsed = int.TryParse(strID, out id))
                oUser = umbraco.BusinessLogic.User.GetUser(id);

            if (oUser == null || oUser.UserType == null || !parsed)
                throw new Exception("No user found with id: " + strID);
        }

       
        protected override void OnPreRender(EventArgs e) {
            base.OnPreRender(e);
            ScriptManager.GetCurrent(Page).Services.Add(new ServiceReference("~/" + GlobalSettings.Path + "/users/PermissionsHandler.asmx"));
        }

    }
}
