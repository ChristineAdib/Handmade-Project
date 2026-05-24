using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.NotificationEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.WishListEntoties;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace HandoraInfrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options):IdentityDbContext(options)
{
    public DbSet<Shop> Shops { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<DeliveryMethod> DeliveryMethods { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<WishList> WishLists { get; set; }
    public DbSet<WishListItem> WishListItems { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<OrderShippingAddress> OrderShippingAddresses { get; set; }

    public DbSet<SellerBalanceTransaction> SellerBalanceTransactions { get; set; }

    public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.Entity<OrderItem>()
            .OwnsOne(o => o.Product);
    }
}
