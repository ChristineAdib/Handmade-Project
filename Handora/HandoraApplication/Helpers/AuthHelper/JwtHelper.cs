using HandoraDomain.Models.AppUser;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Helpers.AuthHelper
{
    public sealed class JwtHelper
    {
        private readonly JwtOptions _options;

        public JwtHelper(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public (string Token, DateTime Expiry) GenerateToken(User user, IList<string> roles)
        {
            var claims = BuildClaims(user, roles);
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddDays(_options.DurationInDays);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiry,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(tokenDescriptor), expiry);
        }

        private static List<Claim> BuildClaims(User user, IList<string> roles)
        {
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            return claims;
        }
    }
}
