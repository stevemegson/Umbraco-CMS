﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Umbraco.Core.Models
{
    /// <summary>
    /// Represents the content type that a <see cref="Content"/> object is based on
    /// </summary>
    [Serializable]
    [DataContract(IsReference = true)]
    public class ContentType : ContentTypeCompositionBase, IContentType
    {
        private int _defaultTemplate;
        private IEnumerable<ITemplate> _allowedTemplates;
        
        /// <summary>
        /// Constuctor for creating a ContentType with the parent's id.
        /// </summary>
        /// <remarks>You usually only want to use this for creating ContentTypes at the root.</remarks>
        /// <param name="parentId"></param>
        public ContentType(int parentId) : base(parentId)
        {
            _allowedTemplates = new List<ITemplate>();
        }

        /// <summary>
        /// Constuctor for creating a ContentType with the parent as an inherited type.
        /// </summary>
        /// <remarks>Use this to ensure inheritance from parent.</remarks>
        /// <param name="parent"></param>
		public ContentType(IContentType parent) : base(parent)
		{
			_allowedTemplates = new List<ITemplate>();
		}

        private static readonly PropertyInfo DefaultTemplateSelector = ExpressionHelper.GetPropertyInfo<ContentType, int>(x => x.DefaultTemplateId);
        private static readonly PropertyInfo AllowedTemplatesSelector = ExpressionHelper.GetPropertyInfo<ContentType, IEnumerable<ITemplate>>(x => x.AllowedTemplates);
        
        /// <summary>
        /// Gets or sets the alias of the default Template.
        /// </summary>
        [IgnoreDataMember]
        public ITemplate DefaultTemplate
        {
            get { return AllowedTemplates.FirstOrDefault(x => x != null && x.Id == DefaultTemplateId); }
        }

        /// <summary>
        /// Internal property to store the Id of the default template
        /// </summary>
        [DataMember]
        internal int DefaultTemplateId
        {
            get { return _defaultTemplate; }
            set
            {
                SetPropertyValueAndDetectChanges(o =>
                {
                    _defaultTemplate = value;
                    return _defaultTemplate;
                }, _defaultTemplate, DefaultTemplateSelector);
            }
        }

        /// <summary>
        /// Gets or Sets a list of Templates which are allowed for the ContentType
        /// </summary>
        [DataMember]
        public IEnumerable<ITemplate> AllowedTemplates
        {
            get { return _allowedTemplates; }
            set
            {
                SetPropertyValueAndDetectChanges(o =>
                {
                    _allowedTemplates = value;
                    return _allowedTemplates;
                }, _allowedTemplates, AllowedTemplatesSelector);
            }
        }

        /// <summary>
        /// Sets the default template for the ContentType
        /// </summary>
        /// <param name="template">Default <see cref="ITemplate"/></param>
        public void SetDefaultTemplate(ITemplate template)
        {
            if (template == null)
            {
                DefaultTemplateId = 0;
                return;
            }

            DefaultTemplateId = template.Id;
            if(_allowedTemplates.Any(x => x != null && x.Id == template.Id) == false)
            {
                var templates = AllowedTemplates.ToList();
                templates.Add(template);
                AllowedTemplates = templates;
            }
        }

        /// <summary>
        /// Removes a template from the list of allowed templates
        /// </summary>
        /// <param name="template"><see cref="ITemplate"/> to remove</param>
        /// <returns>True if template was removed, otherwise False</returns>
        public bool RemoveTemplate(ITemplate template)
        {
            if (DefaultTemplateId == template.Id)
                DefaultTemplateId = default(int);

            var templates = AllowedTemplates.ToList();
            var remove = templates.FirstOrDefault(x => x.Id == template.Id);
            var result = templates.Remove(remove);
            AllowedTemplates = templates;

            return result;
        }
        
        /// <summary>
        /// Method to call when Entity is being saved
        /// </summary>
        /// <remarks>Created date is set and a Unique key is assigned</remarks>
        internal override void AddingEntity()
        {
            base.AddingEntity();

            if(Key == Guid.Empty)
                Key = Guid.NewGuid();
        }

        /// <summary>
        /// Method to call when Entity is being updated
        /// </summary>
        /// <remarks>Modified Date is set and a new Version guid is set</remarks>
        internal override void UpdatingEntity()
        {
            base.UpdatingEntity();
        }

        /// <summary>
        /// //TODO: REmove this as it's mostly just a shallow clone and not thread safe
        /// Creates a clone of the current entity
        /// </summary>
        /// <returns></returns>
        public IContentType Clone(string alias)
        {
            var clone = (ContentType)this.MemberwiseClone();
            clone.Alias = alias;
            clone.Key = Guid.Empty;
            var propertyGroups = this.PropertyGroups.Select(x => x.Clone()).ToList();
            clone.PropertyGroups = new PropertyGroupCollection(propertyGroups);
            clone.PropertyTypes = this.PropertyTypeCollection.Select(x => x.Clone()).ToList();
            clone.ResetIdentity();
            clone.ResetDirtyProperties(false);

            foreach (var propertyGroup in clone.PropertyGroups)
            {
                propertyGroup.ResetIdentity();
                foreach (var propertyType in propertyGroup.PropertyTypes)
                {
                    propertyType.ResetIdentity();
                }
            }

            foreach (var propertyType in clone.PropertyTypes.Where(x => x.HasIdentity))
            {
                propertyType.ResetIdentity();
            }

            return clone;
        }

        public override T DeepClone<T>()
        {
            var clone = base.DeepClone<T>();

            var contentType = (ContentType)(object)clone;
            contentType.AllowedTemplates = AllowedTemplates.Select(t => t.DeepClone<ITemplate>()).ToArray();
            contentType.ResetDirtyProperties(true);

            return clone;
        }
    }
}