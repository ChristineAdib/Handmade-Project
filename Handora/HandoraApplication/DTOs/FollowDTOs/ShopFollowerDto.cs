namespace HandoraApplication.DTOs.FollowDTOs
{
    public class ShopFollowerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public DateTime FollowedAt { get; set; }
    }
}