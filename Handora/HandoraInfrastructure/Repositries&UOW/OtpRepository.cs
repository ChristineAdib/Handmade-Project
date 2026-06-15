using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HandoraInfrastructure.Repositries_UOW
{
    public sealed class OtpRepository : IOtpRepository
    {
        private readonly AppDbContext _context;

        public OtpRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OtpVerification> CreateAsync(OtpVerification otp, CancellationToken ct = default)
        {
            await _context.OtpVerifications.AddAsync(otp, ct);
            await _context.SaveChangesAsync(ct);
            return otp;
        }

        public async Task<OtpVerification?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _context.OtpVerifications
                .Where(o => o.Email == email && !o.IsVerified)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<OtpVerification?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return await _context.OtpVerifications.FindAsync(new object[] { id }, cancellationToken: ct);
        }

        public async Task UpdateAsync(OtpVerification otp, CancellationToken ct = default)
        {
            _context.OtpVerifications.Update(otp);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var otp = await GetByIdAsync(id, ct);
            if (otp is not null)
            {
                _context.OtpVerifications.Remove(otp);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task DeleteExpiredAsync(CancellationToken ct = default)
        {
            var expiredOtps = await _context.OtpVerifications
                .Where(o => o.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(ct);

            if (expiredOtps.Any())
            {
                _context.OtpVerifications.RemoveRange(expiredOtps);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
