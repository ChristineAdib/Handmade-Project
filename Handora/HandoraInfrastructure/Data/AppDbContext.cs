using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.ChatEntities;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.FollowEntities;
using HandoraDomain.Models.NotificationEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.WishListEntoties;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraInfrastructure.Data.Configuration;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace HandoraInfrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options):IdentityDbContext(options)
{
    public DbSet<Shop> Shops { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductReviewSummary> ProductReviewSummaries { get; set; }
    public DbSet<ProductDraft> ProductDrafts { get; set; }
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
    public DbSet<ShopReview> ShopReviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Coupon> Coupons { get; set; }

    public DbSet<HandoraDomain.Models.AppUser.Address> Addresses { get; set; }
    public DbSet<OtpVerification> OtpVerifications { get; set; }
    public DbSet<OrderShippingAddress> OrderShippingAddresses { get; set; }

    public DbSet<SellerBalanceTransaction> SellerBalanceTransactions { get; set; }

    public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }

    public DbSet<Follow> Follows { get; set; }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }

    // Handora Custom Studio
    public DbSet<CustomRequest> CustomRequests { get; set; }
    public DbSet<CustomConfiguration> CustomConfigurations { get; set; }
    public DbSet<GeneratedDesign> GeneratedDesigns { get; set; }
    public DbSet<SellerRecommendation> SellerRecommendations { get; set; }
    public DbSet<CustomOffer> CustomOffers { get; set; }
    public DbSet<ProjectWorkspace> ProjectWorkspaces { get; set; }
    public DbSet<CustomStudioSetting> CustomStudioSettings { get; set; }
    public DbSet<CustomStudioAuditLog> CustomStudioAuditLogs { get; set; }
    public DbSet<CustomService> CustomServices { get; set; }
    public DbSet<WorkspaceTimelineEntry> WorkspaceTimelineEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.Entity<OrderItem>()
            .OwnsOne(o => o.Product);
        modelBuilder.ApplyConfiguration(new FollowConfiguration());

        modelBuilder.Entity<CustomService>(entity =>
        {
            entity.HasOne(cs => cs.Buyer)
                .WithMany()
                .HasForeignKey(cs => cs.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cs => cs.Seller)
                .WithMany()
                .HasForeignKey(cs => cs.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cs => cs.Shop)
                .WithMany()
                .HasForeignKey(cs => cs.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cs => cs.CustomRequest)
                .WithOne(cr => cr.CustomService)
                .HasForeignKey<CustomService>(cs => cs.CustomRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cs => cs.Conversation)
                .WithMany()
                .HasForeignKey(cs => cs.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasOne(o => o.CustomOffer)
                .WithMany()
                .HasForeignKey(o => o.CustomOfferId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
