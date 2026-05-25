namespace HandoraApplication.DTOs.SellerDTOs
{
    public class SellerProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ProfileImage { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsVerified { get; set; }
        public DateTime MemberSince { get; set; }
    }
}