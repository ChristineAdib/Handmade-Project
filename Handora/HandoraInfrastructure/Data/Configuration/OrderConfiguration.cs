using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Data.Configuration
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            // =====================================================================
            // Scalar Properties
            // =====================================================================

            builder.Property(o => o.BuyerEmail)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.OrderDate)
                .IsRequired();

            builder.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>()   // stored as "Pending" not 0 → readable in DB
                .HasMaxLength(50);

            builder.Property(o => o.PaymentStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(o => o.SubTotal)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(o => o.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Property(o => o.Notes)
                .HasMaxLength(500);

            // =====================================================================
            // ShippingAddress — Owned Type
            // Address is not a standalone table; its columns live inside Orders.
            // This matches the snapshot pattern: even if the user later changes
            // their saved address, the order keeps the address used at checkout.
            // =====================================================================

            builder.OwnsOne(o => o.ShippingAddress, address =>
            {
                address.Property(a => a.Street)
                    .HasColumnName("ShippingAddressLine")
                    .IsRequired()
                    .HasMaxLength(300);

                address.Property(a => a.City)
                    .HasColumnName("ShippingCity")
                    .IsRequired()
                    .HasMaxLength(100);

                address.Property(a => a.Country)
                    .HasColumnName("ShippingCountry")
                    .IsRequired()
                    .HasMaxLength(100);

                
            });


            // Many Orders → One User
            builder.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);  // don't delete orders when user is deleted

            // Many Orders → One DeliveryMethod (each order locks in a delivery method)
            builder.HasOne(o => o.DeliveryMethod)
                .WithMany()
                .HasForeignKey(o => o.DeliveryMethodId)  // needs DeliveryMethodId FK on Order
                .OnDelete(DeleteBehavior.Restrict);       // don't lose order data if method is removed

            // One Order → Many OrderItems
            builder.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);  // items die with the order

            // One Order ↔ One Payment
            builder.HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // CouponId is nullable FK — no nav property needed unless you add one
            builder.Property(o => o.CouponId)
                .IsRequired(false);


            // Fast lookup by user (order history page)
            builder.HasIndex(o => o.UserId);

            // Fast lookup by email (guest checkout support in future)
            builder.HasIndex(o => o.BuyerEmail);
        }
    }
}
