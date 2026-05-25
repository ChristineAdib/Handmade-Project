using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ChatDTOs
{
    public sealed record StartConversationDto
    {
        [Required]
        public string SellerId { get; init; } = string.Empty;
    }
}
