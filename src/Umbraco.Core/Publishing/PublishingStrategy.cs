using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Umbraco.Core.Publishing
{
    /// <summary>
    /// Currently acts as an interconnection between the new public api and the legacy api for publishing
    /// </summary>
    internal class PublishingStrategy : BasePublishingStrategy
    {
        internal PublishingStrategy()
        {
        }

        /// <summary>
        /// Publishes a single piece of Content
        /// </summary>
        /// <param name="content"><see cref="IContent"/> to publish</param>
        /// <param name="userId">Id of the User issueing the publish operation</param>
        /// <returns>True if the publish operation was successfull and not cancelled, otherwise false</returns>
        public override bool Publish(IContent content, int userId)
        {
            var e = new PublishingEventArgs();
            //Fire Publishing event
            OnPublish(content, e);

            if (!e.Cancel)
            {
                //Check if the Content is Expired to verify that it can in fact be published
                if(content.Status == ContentStatus.Expired)
                {
                    LogHelper.Info<PublishingStrategy>(
                        string.Format("Content '{0}' with Id '{1}' has expired and could not be published.",
                                      content.Name, content.Id));
                    return false;
                }

                //Check if the Content is Awaiting Release to verify that it can in fact be published
                if (content.Status == ContentStatus.AwaitingRelease)
                {
                    LogHelper.Info<PublishingStrategy>(
                        string.Format("Content '{0}' with Id '{1}' is awaiting release and could not be published.",
                                      content.Name, content.Id));
                    return false;
                }

                //Check if the Content is Trashed to verify that it can in fact be published
                if (content.Status == ContentStatus.Trashed)
                {
                    LogHelper.Info<PublishingStrategy>(
                        string.Format("Content '{0}' with Id '{1}' is trashed and could not be published.",
                                      content.Name, content.Id));
                    return false;
                }

                content.ChangePublishedState(true);

                LogHelper.Info<PublishingStrategy>(
                    string.Format("Content '{0}' with Id '{1}' has been published.",
                                  content.Name, content.Id));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Publishes a list of Content
        /// </summary>
        /// <param name="content">An enumerable list of <see cref="IContent"/></param>
        /// <param name="userId">Id of the User issueing the publish operation</param>
        /// <returns>True if the publish operation was successfull and not cancelled, otherwise false</returns>
        public override bool PublishWithChildren(IEnumerable<IContent> content, int userId)
        {
            var e = new PublishingEventArgs();

            /* Only update content thats not already been published - we want to loop through
             * all unpublished content to write skipped content (expired and awaiting release) to log.
             */
            foreach (var item in content.Where(x => x.Published == false))
            {
                //Fire Publishing event
                OnPublish(item, e);
                if (e.Cancel)
                    return false;

                //Check if the Content is Expired to verify that it can in fact be published
                if (item.Status == ContentStatus.Expired)
                {
                    LogHelper.Info<PublishingStrategy>(
                        string.Format("Content '{0}' with Id '{1}' has expired and could not be published.",
                                      item.Name, item.Id));
                    continue;
                }

                //Check if the Content is Awaiting Release to verify that it can in fact be published
                if (item.Status == ContentStatus.AwaitingRelease)
                {
                    LogHelper.Info<PublishingStrategy>(
                        string.Format("Content '{0}' with Id '{1}' is awaiting release and could not be published.",
                                      item.Name, item.Id));
                    continue;
                }

                //Check if the Content is Trashed to verify that it can in fact be published
                if (item.Status == ContentStatus.Trashed)
                {
                    LogHelper.Info<PublishingStrategy>(
                        string.Format("Content '{0}' with Id '{1}' is trashed and could not be published.",
                                      item.Name, item.Id));
                    continue;
                }

                item.ChangePublishedState(true);
                
                LogHelper.Info<PublishingStrategy>(
                    string.Format("Content '{0}' with Id '{1}' has been published.",
                                  item.Name, item.Id));
            }

            return true;
        }

        /// <summary>
        /// Unpublishes a single piece of Content
        /// </summary>
        /// <param name="content"><see cref="IContent"/> to unpublish</param>
        /// <param name="userId">Id of the User issueing the unpublish operation</param>
        /// <returns>True if the unpublish operation was successfull and not cancelled, otherwise false</returns>
        public override bool UnPublish(IContent content, int userId)
        {
            var e = new PublishingEventArgs();
            //Fire BeforeUnPublish event
            OnUnPublish(content, e);

            if (!e.Cancel)
            {
                //If Content has a release date set to before now, it should be removed so it doesn't interrupt an unpublish
                //Otherwise it would remain released == published
                if (content.ReleaseDate.HasValue && content.ReleaseDate.Value <= DateTime.UtcNow)
                {
                    content.ReleaseDate = null;

                    LogHelper.Info<PublishingStrategy>(
                        string.Format(
                            "Content '{0}' with Id '{1}' had its release date removed, because it was unpublished.",
                            content.Name, content.Id));
                }

                content.ChangePublishedState(false);

                LogHelper.Info<PublishingStrategy>(
                    string.Format("Content '{0}' with Id '{1}' has been unpublished.",
                                  content.Name, content.Id));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unpublishes a list of Content
        /// </summary>
        /// <param name="content">An enumerable list of <see cref="IContent"/></param>
        /// <param name="userId">Id of the User issueing the unpublish operation</param>
        /// <returns>True if the unpublish operation was successfull and not cancelled, otherwise false</returns>
        public override bool UnPublish(IEnumerable<IContent> content, int userId)
        {
            var e = new PublishingEventArgs();

            //Only update content thats already been published
            foreach (var item in content.Where(x => x.Published == true))
            {
                //Fire UnPublished event
                OnUnPublish(item, e);
                if (e.Cancel)
                    return false;

                //If Content has a release date set to before now, it should be removed so it doesn't interrupt an unpublish
                //Otherwise it would remain released == published
                if (item.ReleaseDate.HasValue && item.ReleaseDate.Value <= DateTime.UtcNow)
                {
                    item.ReleaseDate = null;

                    LogHelper.Info<PublishingStrategy>(
                        string.Format("Content '{0}' with Id '{1}' had its release date removed, because it was unpublished.",
                                      item.Name, item.Id));
                }

                item.ChangePublishedState(false);

                LogHelper.Info<PublishingStrategy>(
                    string.Format("Content '{0}' with Id '{1}' has been unpublished.",
                                  item.Name, item.Id));
            }

            return true;
        }

        /// <summary>
        /// Call to fire event that updating the published content has finalized.
        /// </summary>
        /// <remarks>
        /// This seperation of the OnPublished event is done to ensure that the Content
        /// has been properly updated (committed unit of work) and xml saved in the db.
        /// </remarks>
        /// <param name="content"><see cref="IContent"/> thats being published</param>
        public override void PublishingFinalized(IContent content)
        {
            OnPublished(content, new PublishingEventArgs());
        }

        /// <summary>
        /// Call to fire event that updating the published content has finalized.
        /// </summary>
        /// <param name="content">An enumerable list of <see cref="IContent"/> thats being published</param>
        /// <param name="isAllRepublished">Boolean indicating whether its all content that is republished</param>
        public override void PublishingFinalized(IEnumerable<IContent> content, bool isAllRepublished)
        {
            OnPublished(content, new PublishingEventArgs{ IsAllRepublished = isAllRepublished});
        }

        /// <summary>
        /// Call to fire event that updating the unpublished content has finalized.
        /// </summary>
        /// <param name="content"><see cref="IContent"/> thats being unpublished</param>
        public override void UnPublishingFinalized(IContent content)
        {
            OnUnPublished(content, new PublishingEventArgs());
        }

        /// <summary>
        /// Call to fire event that updating the unpublished content has finalized.
        /// </summary>
        /// <param name="content">An enumerable list of <see cref="IContent"/> thats being unpublished</param>
        public override void UnPublishingFinalized(IEnumerable<IContent> content)
        {
            OnUnPublished(content, new PublishingEventArgs());
        }
    }
}