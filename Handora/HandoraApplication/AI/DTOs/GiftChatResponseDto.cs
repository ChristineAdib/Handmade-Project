using System.Collections.Generic;

namespace HandoraApplication.AI.DTOs
{
    public class GiftChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
        public List<GiftProductDto> Products { get; set; } = new();
        public GiftRequestState State { get; set; } = new();
    }
}
