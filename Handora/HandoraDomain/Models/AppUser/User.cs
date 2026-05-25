using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.FollowEntities;
using HandoraDomain.Models.NotificationEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.WishListEntoties;
using Microsoft.AspNetCore.Identity;

namespace HandoraDomain.Models.AppUser;

public class User : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool IsBanned { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public DateTime? EmailVerifiedAt { get; set; }

    // Navigation Properties
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public WishList? WishList { get; set; }
    public Cart? Cart { get; set; }
    public Shop? Shop { get; set; }







    public ICollection<SellerBalanceTransaction> SellerBalanceTransactions { get; set; } = [];
    public ICollection<Follow> Following { get; set; } = [];
}
