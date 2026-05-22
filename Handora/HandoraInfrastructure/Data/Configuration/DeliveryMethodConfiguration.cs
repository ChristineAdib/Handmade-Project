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
                .HasMaxLength(50);

            builder.Property(d => d.DescriptionEn)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.DescriptionAr)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.DeliveryTimeEn)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.DeliveryTimeAr)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.Cost)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(d => d.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(d => d.IsActive);
        }
    }
}
