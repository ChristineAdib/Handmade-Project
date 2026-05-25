using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.AuthDTOs
{
    public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}
