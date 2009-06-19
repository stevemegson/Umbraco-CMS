﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Configuration;
using umbraco.BasePages;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.cache;
using umbraco.cms.businesslogic.contentitem;
using umbraco.cms.businesslogic.datatype;
using umbraco.cms.businesslogic.language;
using umbraco.cms.businesslogic.media;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.property;
using umbraco.cms.businesslogic.web;
using umbraco.interfaces;
using umbraco.DataLayer;
using umbraco.cms.presentation.Trees;
using umbraco.BusinessLogic.Actions;


namespace umbraco
{
    /// <summary>
    /// Handles loading the content tree into umbraco's application tree
    /// </summary>
    public class loadContent : BaseContentTree
    {

        public loadContent(string application) : base(application) { }        

        private Document m_document;

        /// <summary>
        /// Returns the Document object of the starting node for the current User. This ensures
        /// that the Document object is only instantiated once.
        /// </summary>
        protected Document StartNode
        {
            get
            {
                return (m_document == null ? m_document = new Document(StartNodeID) : m_document);
            }
        }

        /// <summary>
        /// Creates the root node context menu for the content tree.
        /// Depending on the current User's permissions, this menu will change.
        /// If the current User's starting node is not -1 (the normal root content tree node)
        /// then the menu will be built based on the permissions of the User's start node.
        /// </summary>
        /// <param name="actions"></param>
        protected override void CreateRootNodeActions(ref List<IAction> actions)
		{
			actions.Clear();

            if (StartNodeID != -1)
            {
                //get the document for the start node id
                Document doc = StartNode;
                //get the allowed actions for the user for the current node
                List<IAction> nodeActions = GetUserActionsForNode(doc);
                //get the allowed actions for the tree based on the users allowed actions
                List<IAction> allowedMenu = GetUserAllowedActions(AllowedActions, nodeActions);
                actions.AddRange(allowedMenu);
            }
            else
            {
                ///add the default actions to the content tree
                actions.Add(ActionNew.Instance);
                actions.Add(ActionSort.Instance);
                actions.Add(ContextMenuSeperator.Instance);
                actions.Add(ActionRePublish.Instance);
                actions.Add(ContextMenuSeperator.Instance);
                actions.Add(ActionRefresh.Instance);
            }			
		}

        protected override void CreateAllowedActions(ref List<IAction> actions)
		{
			actions.Clear();
            actions.Add(ActionNew.Instance);
            actions.Add(ActionLiveEdit.Instance);
            actions.Add(ContextMenuSeperator.Instance);
            actions.Add(ActionDelete.Instance);
			actions.Add(ContextMenuSeperator.Instance);
			actions.Add(ActionMove.Instance);
			actions.Add(ActionCopy.Instance);
			actions.Add(ContextMenuSeperator.Instance);
			actions.Add(ActionSort.Instance);
			actions.Add(ActionRollback.Instance);
			actions.Add(ContextMenuSeperator.Instance);
			actions.Add(ActionPublish.Instance);
            actions.Add(ActionToPublish.Instance);
			actions.Add(ActionAssignDomain.Instance);
			actions.Add(ActionRights.Instance);
			actions.Add(ContextMenuSeperator.Instance);
			actions.Add(ActionProtect.Instance);
			actions.Add(ContextMenuSeperator.Instance);
			actions.Add(ActionUnPublish.Instance);
			actions.Add(ContextMenuSeperator.Instance);
			actions.Add(ActionNotify.Instance);
			actions.Add(ActionSendToTranslate.Instance);
            actions.Add(ContextMenuSeperator.Instance);
            actions.Add(ActionRefresh.Instance);
		}

        /// <summary>
        /// Creates the root node for the content tree. If the current User does
        /// not have access to the actual content tree root, then we'll display the 
        /// node that correlates to their StartNodeID
        /// </summary>
        /// <param name="rootNode"></param>
        protected override void CreateRootNode(ref XmlTreeNode rootNode)
        {
            if (StartNodeID != -1)
            {
                Document doc = StartNode;
                rootNode = CreateNode(doc, RootNodeActions);
            }
            else
            {
                if (IsDialog)
                    rootNode.Action = "javascript:openContent(-1);";
            }
            
        }

		public override int StartNodeID
		{
			get
			{
                return CurrentUser.StartNodeId;
			}
		}        

        /// <summary>
        /// Adds the recycling bin node. This method should only actually add the recycle bin node when the tree is initially created and if the user
        /// actually has access to the root node.
        /// </summary>
        /// <returns></returns>
        protected XmlTreeNode CreateRecycleBin()
        {
            if (m_id == -1 && !this.IsDialog)
            {
                //create a new content recycle bin tree, initialized with it's startnodeid
                ContentRecycleBin bin = new ContentRecycleBin(this.m_app);
                bin.ShowContextMenu = this.ShowContextMenu;
                bin.id = bin.StartNodeID; 
                return bin.RootNode;               
            }
            return null;
        }


        /// <summary>
        /// Override the render method to add the recycle bin to the end of this tree
        /// </summary>
        /// <param name="Tree"></param>
        public override void Render(ref XmlTree Tree)
        {
            base.Render(ref Tree);
			XmlTreeNode recycleBin = CreateRecycleBin();
			if (recycleBin != null)
				Tree.Add(recycleBin);
        }
       
        

        

    }
}
