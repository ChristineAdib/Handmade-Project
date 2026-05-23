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
    public sealed class AuthRepository : IAuthRepository
    {
        private readonly UserManager<User> _userManager;

        public AuthRepository(UserManager<User> userManager)
        {
            _userManager = userManager;
        }
        public Task AddToRoleAsync(User user, string role)
            => _userManager.AddToRoleAsync(user, role);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
            => await _userManager.FindByEmailAsync(email);

        public async Task<IdentityResult> CreateAsync(User user, string password, CancellationToken ct = default)
            => await _userManager.CreateAsync(user, password);

        public Task<bool> CheckPasswordAsync(User user, string password)
            => _userManager.CheckPasswordAsync(user, password);

        public Task<IList<string>> GetRolesAsync(User user)
            => _userManager.GetRolesAsync(user);

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken ct = default)
            => await _userManager.UpdateAsync(user);
    }
}
