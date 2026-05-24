using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<User?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<IdentityResult> CreateAsync(User user, string password, CancellationToken ct = default);
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<IList<string>> GetRolesAsync(User user);
        Task<IdentityResult> UpdateAsync(User user, CancellationToken ct = default);
        Task AddToRoleAsync(User user, string role);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default);
        Task<IdentityResult> DeleteAsync(User user, CancellationToken ct = default);
    }
}
