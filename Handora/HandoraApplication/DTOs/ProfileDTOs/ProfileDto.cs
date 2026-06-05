using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ProfileDTOs
{
    public class ProfileDto
    {
        public string Id { get; set; } = default!;

        public string Name { get; set; } = default!;

        public string Email { get; set; } = default!;

        public string? PhoneNumber { get; set; }

        public string? Bio { get; set; }

        public string? ProfileImage { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
