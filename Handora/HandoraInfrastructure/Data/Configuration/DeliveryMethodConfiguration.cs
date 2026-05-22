using HandoraDomain.Models.OrderEntity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Data.Configuration
{
    public class DeliveryMethodConfiguration : IEntityTypeConfiguration<DeliveryMethod>
    {
        public void Configure(EntityTypeBuilder<DeliveryMethod> builder)
        {
            builder.ToTable("DeliveryMethods");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.ShortName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.Description)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(d => d.DeliveryTime)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.Cost)
                .IsRequired()
                .HasPrecision(18, 2);

            // Seed initial data — no need for a migration every time you add a method
            builder.HasData(
                new DeliveryMethod { Id = 1, ShortName = "Standard", Description = "Standard Delivery", DeliveryTime = "5-7 Days", Cost = 15.00m, IsActive = true },
                new DeliveryMethod { Id = 2, ShortName = "Express", Description = "Express Delivery", DeliveryTime = "2-3 Days", Cost = 35.00m, IsActive = true },
                new DeliveryMethod { Id = 3, ShortName = "Next Day", Description = "Next Day Delivery", DeliveryTime = "1 Day", Cost = 60.00m, IsActive = true }
            );
        }
    }
}
