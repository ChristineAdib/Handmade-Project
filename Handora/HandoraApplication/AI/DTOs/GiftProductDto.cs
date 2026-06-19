namespace HandoraApplication.AI.DTOs
{
    public class GiftProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string WhyRecommended { get; set; } = string.Empty;
    }
}
