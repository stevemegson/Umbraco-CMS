<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="../masterpages/umbracoPage.Master" CodeBehind="PermissionEditor.aspx.cs" Inherits="umbraco.cms.presentation.user.PermissionEditor" %>

<%@ Register Src="../controls/TreeControl.ascx" TagName="TreeControl" TagPrefix="umbraco" %>
<%@ Register Src="NodePermissions.ascx" TagName="NodePermissions" TagPrefix="user" %>
<%@ Register Namespace="umbraco.presentation.controls" Assembly="umbraco" TagPrefix="tree" %>
<%@ Register TagPrefix="ui" Namespace="umbraco.uicontrols" Assembly="controls" %>
<%@ Register TagPrefix="umb" Namespace="umbraco.presentation.ClientDependency.Controls" Assembly="umbraco.presentation.ClientDependency" %>
<asp:Content ContentPlaceHolderID="head" runat="server">
		
	<umb:CssInclude ID="CssInclude2" runat="server" FilePath="css/permissionsEditor.css" PathNameAlias="UmbracoRoot" />
	<umb:CssInclude ID="CssInclude1" runat="server" FilePath="css/umbracoGui.css" PathNameAlias="UmbracoRoot" />
	<umb:JsInclude ID="JsInclude1"  runat="server" FilePath="PermissionsEditor.js" />
	
</asp:Content>
<asp:Content ContentPlaceHolderID="body" runat="server">

	<ui:UmbracoPanel ID="pnlUmbraco" runat="server" hasMenu="true" Text="Content Tree Permissions" Width="608px">
		<ui:Pane ID="pnl1" Style="padding: 10px; text-align: left;" runat="server" Text="Select pages to modify their permissions">
			<div id="treeContainer">				
				<umbraco:TreeControl runat="server" ID="JTree" TreeType="Checkbox" CustomContainerId="permissionsTreeContainer"></umbraco:TreeControl>
			</div>
			<div id="permissionsPanel">
				<user:NodePermissions ID="nodePermissions" runat="server" />
			</div>			
			
			<script type="text/javascript" language="javascript">				
				jQuery(document).ready(function() {
					jQuery("#treeContainer .umbTree").PermissionsEditor({
						userId: <%=Request.QueryString["id"] %>,
						pPanelSelector: "#permissionsPanel",
						replacePChkBoxSelector: "#chkChildPermissions"});						
				});
				function SavePermissions() {
					jQuery("#treeContainer .umbTree").PermissionsEditorAPI().beginSavePermissions();
				}
			</script>
    
		</ui:Pane>
	</ui:UmbracoPanel>
	
</asp:Content>
