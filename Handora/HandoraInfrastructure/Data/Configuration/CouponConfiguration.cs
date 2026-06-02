using HandoraDomain.Models.CouponEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

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

            builder.Property(c => c.DiscountValue)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(c => c.DiscountType)
                .IsRequired()
                .HasConversion<string>()  // store "Percentage" / "FixedAmount" not 0/1
                .HasMaxLength(50);

            builder.Property(c => c.ExpiryDate)
                .IsRequired();

            builder.Property(c => c.MinOrderValue)
                .HasPrecision(18, 2);     // nullable — no minimum if null

            builder.Property(c => c.MaxUsageCount)
                .IsRequired(false);       // nullable — unlimited if null

            builder.Property(c => c.CurrentUsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Coupon belongs to a Seller (User)
            builder.HasOne(c => c.Seller)
                .WithMany()
                .HasForeignKey(c => c.SellerId)
                .OnDelete(DeleteBehavior.Cascade); // if seller is deleted, coupons are deleted too

            // Orders that used this coupon — one coupon can be used by many orders.
            builder.HasMany(c => c.Orders)
                .WithOne(o => o.Coupon)
                .HasForeignKey(o => o.CouponId)
                .OnDelete(DeleteBehavior.SetNull); // if coupon deleted, order keeps its data but CouponId → null

            // Fast lookup: "get all active coupons for this seller"
            builder.HasIndex(c => new { c.SellerId, c.IsActive });

            // Fast expiry check: "get all non-expired coupons"
            builder.HasIndex(c => c.ExpiryDate);
        }
    }
}
