﻿using Umbraco.Core.Models.Membership;

namespace Umbraco.Web.Services
{
    /// <summary>
    /// Defines the UserService, which is an easy access to operations involving <see cref="IProfile"/> and eventually Users and Members.
    /// </summary>
    public interface IUserService : IService
    {
        /// <summary>
        /// Gets an <see cref="IProfile"/> for the current BackOffice User
        /// </summary>
        /// <returns><see cref="IProfile"/> containing the Name and Id of the logged in BackOffice User</returns>
        IProfile GetCurrentBackOfficeUser();
    }
}