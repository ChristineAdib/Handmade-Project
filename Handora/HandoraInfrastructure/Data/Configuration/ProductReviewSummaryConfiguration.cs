using HandoraDomain.Models.ProductEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HandoraInfrastructure.Data.Configuration
{
    public class ProductReviewSummaryConfiguration : IEntityTypeConfiguration<ProductReviewSummary>
    {
        public void Configure(EntityTypeBuilder<ProductReviewSummary> builder)
        {
            builder.HasKey(prs => prs.Id);

            builder.Property(prs => prs.OverallSummary)
                .IsRequired();

            // Configured as strings that will hold serialized JSON arrays
            builder.Property(prs => prs.Pros)
                .IsRequired()
                .HasDefaultValue("[]");

            builder.Property(prs => prs.Cons)
                .IsRequired()
                .HasDefaultValue("[]");

            builder.Property(prs => prs.LastUpdated)
                .IsRequired();

            // 1-to-1 relationship between Product and ProductReviewSummary
            builder.HasOne(prs => prs.Product)
                .WithOne(p => p.ReviewSummary)
                .HasForeignKey<ProductReviewSummary>(prs => prs.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
