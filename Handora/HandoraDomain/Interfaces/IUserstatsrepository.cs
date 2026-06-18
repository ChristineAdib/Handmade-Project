using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Interfaces
{


    public interface IUserStatsRepository
    {
        /// <summary>
        /// Total number of (non-deleted) users that belong to the given role.
        /// </summary>
        Task<int> GetTotalUsersInRoleAsync(string role);

        /// <summary>
        /// Number of users in the given role created on/after <paramref name="sinceUtc"/>.
        /// </summary>
        Task<int> GetNewUsersInRoleCountAsync(string role, DateTime sinceUtc);

        /// <summary>
        /// Returns the users that belong to the given role, including their CreatedAt date.
        /// Useful for further in-memory aggregation (e.g. growth charts).
        /// </summary>
        Task<IReadOnlyList<User>> GetUsersInRoleAsync(string role);
    }

}
