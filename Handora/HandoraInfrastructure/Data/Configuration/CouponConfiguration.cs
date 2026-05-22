using HandoraDomain.Models.CouponEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Data.Configuration
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.ToTable("Coupons");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(50);

            // Code must be unique — two coupons can't share the same code
            builder.HasIndex(c => c.Code)
                .IsUnique();

            builder.Property(c => c.Value)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(c => c.DiscountType)
                .IsRequired()
                .HasConversion<string>()  // store "Percentage" / "FixedAmount" not 0/1
                .HasMaxLength(50);

            builder.Property(c => c.ExpiresAt)
                .IsRequired();

            builder.Property(c => c.MinOrderAmount)
                .HasPrecision(18, 2);     // nullable — no minimum if null

            builder.Property(c => c.MaxUsageCount)
                .IsRequired(false);       // nullable — unlimited if null

            builder.Property(c => c.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);


            // Coupon belongs to a Shop — the shop owner creates coupons for their shop.
            // If you want platform-wide coupons (admin only), make ShopId nullable.
            builder.HasOne(c => c.Shop)
                .WithMany(s => s.Coupons)
                .HasForeignKey(c => c.ShopId)
                .OnDelete(DeleteBehavior.Cascade); // if shop is deleted, its coupons go too

            // Orders that used this coupon — one coupon can be used by many orders.
            // The FK lives on Order.CouponId (nullable), NOT on Coupon.
            builder.HasMany(c => c.Orders)
                .WithOne(o => o.Coupon)
                .HasForeignKey(o => o.CouponId)
                .OnDelete(DeleteBehavior.SetNull); // if coupon deleted, order keeps its data but CouponId → null


            // Fast lookup: "get all active coupons for this shop"
            builder.HasIndex(c => new { c.ShopId, c.IsActive });

            // Fast expiry check: "get all non-expired coupons"
            builder.HasIndex(c => c.ExpiresAt);
        }
    }
}
