namespace HandoraApplication.DTOs.FollowDTOs
{
    public class FollowDto
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string? ShopLogo { get; set; }
        public decimal Rating { get; set; }
        public bool IsVerified { get; set; }
        public DateTime FollowedAt { get; set; }
    }
}