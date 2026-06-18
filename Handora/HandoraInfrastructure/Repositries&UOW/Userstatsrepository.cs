using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Repositries_UOW
{

    public sealed class UserStatsRepository(UserManager<User> userManager) : IUserStatsRepository
    {
        private readonly UserManager<User> _userManager = userManager;

        public async Task<IReadOnlyList<User>> GetUsersInRoleAsync(string role)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            return users.Where(u => !u.IsDeleted).ToList();
        }

        public async Task<int> GetTotalUsersInRoleAsync(string role)
        {
            var users = await GetUsersInRoleAsync(role);
            return users.Count;
        }

        public async Task<int> GetNewUsersInRoleCountAsync(string role, DateTime sinceUtc)
        {
            var users = await GetUsersInRoleAsync(role);
            return users.Count(u => u.CreatedAt >= sinceUtc);
        }
    }
}
