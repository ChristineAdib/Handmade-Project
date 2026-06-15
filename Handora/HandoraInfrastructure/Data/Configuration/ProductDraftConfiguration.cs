using HandoraDomain.Models.ProductEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HandoraInfrastructure.Data.Configuration
{
    public class ProductDraftConfiguration : IEntityTypeConfiguration<ProductDraft>
    {
        public void Configure(EntityTypeBuilder<ProductDraft> builder)
        {
            builder.ToTable("ProductDrafts");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(d => d.TitleEn).HasMaxLength(200);
            builder.Property(d => d.TitleAr).HasMaxLength(200);
            builder.Property(d => d.DescriptionEn).HasMaxLength(4000);
            builder.Property(d => d.DescriptionAr).HasMaxLength(4000);

            builder.Property(d => d.Price).HasPrecision(18, 2);
            builder.Property(d => d.DiscountPrice).HasPrecision(18, 2);

            builder.Property(d => d.ProposedTagsJson).HasMaxLength(4000);
            builder.Property(d => d.NewImageUrlsJson).HasMaxLength(4000);
            builder.Property(d => d.RemoveImageIdsJson).HasMaxLength(4000);

            builder.Property(d => d.IsDeleted).HasDefaultValue(false);
            builder.HasQueryFilter(d => !d.IsDeleted);

            // One-to-one with Product (one pending draft per product)
            builder.HasOne(d => d.Product)
                .WithOne(p => p.PendingDraft)
                .HasForeignKey<ProductDraft>(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for fast lookup by ProductId + Status
            builder.HasIndex(d => new { d.ProductId, d.Status });
        }
    }
}
