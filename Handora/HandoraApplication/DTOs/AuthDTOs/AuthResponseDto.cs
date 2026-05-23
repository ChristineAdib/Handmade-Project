using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.AuthDTOs
{
    public sealed record AuthResponseDto
    {
        public string UserId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
        public DateTime TokenExpiry { get; init; }
        public IList<string> Roles { get; init; } = [];
    }
}
