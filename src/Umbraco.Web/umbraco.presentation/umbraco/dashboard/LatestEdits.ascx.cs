using umbraco.BusinessLogic;
using System;
using System.Linq;
using Umbraco.Core.IO;
using umbraco.cms.businesslogic.web;
using Umbraco.Web;

namespace dashboardUtilities
{
	/// <summary>
	///		Summary description for LatestEdits.
	/// </summary>
	public partial class LatestEdits : System.Web.UI.UserControl
	{

		// Find current user
		private System.Collections.ArrayList printedIds = new System.Collections.ArrayList();
		private int count = 0;
        public int MaxRecords { get; set; }

		protected void Page_Load(object sender, EventArgs e)
		{
			if (MaxRecords == 0)
		        MaxRecords = 30;

            var saves = Log.Instance.GetLogItems(User.GetCurrent(), LogTypes.Save, DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0, 0)));
            var publishes = Log.Instance.GetLogItems(User.GetCurrent(), LogTypes.Publish, DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0, 0)));

            Repeater1.DataSource = saves.Concat(publishes).OrderByDescending(l => l.Timestamp).ToArray();
			Repeater1.DataBind();
		}

        public string PrintNodeName(object nodeId, object date)
        {
            if (!printedIds.Contains(nodeId) && count < MaxRecords)
            {
                printedIds.Add(nodeId);
                try
                {
                    var services = UmbracoContext.Current.Application.Services;
                    var c = services.ContentService.GetById(int.Parse(nodeId.ToString()));                    
                    count++;

                    return String.Format("<a href=\"editContent.aspx?id={0}\" style=\"text-decoration: none\"><img src=\"{1}/images/forward.png\" align=\"absmiddle\" border=\"0\"/> {2}</a>. Edited {3}<br/>",
                        nodeId.ToString(),
                        IOHelper.ResolveUrl(SystemDirectories.Umbraco),
                        c.Name,
                        umbraco.library.ShortDateWithTimeAndGlobal(DateTime.Parse(date.ToString()).ToString(), umbraco.ui.Culture(UmbracoContext.Current.UmbracoUser))                        
                        );
                }
                catch
                {
                    return "";
                }

            }
            else
                return "";
        }

        #region Web Form Designer generated code
        override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		///		Required method for Designer support - do not modify
		///		the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{

		}
		#endregion
	}
}
