using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.AI.DTOs
{
    public class GiftChatRequestDto
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
