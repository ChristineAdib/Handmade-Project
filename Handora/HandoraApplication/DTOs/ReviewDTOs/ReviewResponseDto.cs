using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ReviewDTOs
{
    public class ReviewResponseDto
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

    }
}
