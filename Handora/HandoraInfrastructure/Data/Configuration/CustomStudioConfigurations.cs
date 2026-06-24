using HandoraDomain.Models.CustomStudioEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HandoraInfrastructure.Data.Configuration
{
    public class CustomRequestConfiguration : IEntityTypeConfiguration<CustomRequest>
    {
        public void Configure(EntityTypeBuilder<CustomRequest> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.TargetBudget)
                .HasPrecision(18, 2);

            builder.HasOne(r => r.Buyer)
                .WithMany()
                .HasForeignKey(r => r.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.SelectedSeller)
                .WithMany()
                .HasForeignKey(r => r.SelectedSellerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.SelectedDesign)
                .WithMany()
                .HasForeignKey(r => r.SelectedDesignId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.CustomConfiguration)
                .WithOne(c => c.CustomRequest)
                .HasForeignKey<CustomConfiguration>(c => c.CustomRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.GeneratedDesigns)
                .WithOne(d => d.CustomRequest)
                .HasForeignKey(d => d.CustomRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.SellerRecommendations)
                .WithOne(sr => sr.CustomRequest)
                .HasForeignKey(sr => sr.CustomRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.CustomOffers)
                .WithOne(o => o.CustomRequest)
                .HasForeignKey(o => o.CustomRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.ProjectWorkspace)
                .WithOne(w => w.CustomRequest)
                .HasForeignKey<ProjectWorkspace>(w => w.CustomRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(r => r.BuyerId);
            builder.HasIndex(r => r.SelectedSellerId);
            builder.HasIndex(r => r.SelectedDesignId);
            builder.HasIndex(r => r.CreatedAt);
        }
    }

    public class CustomConfigurationConfiguration : IEntityTypeConfiguration<CustomConfiguration>
    {
        public void Configure(EntityTypeBuilder<CustomConfiguration> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.ConfigurationDataJson).IsRequired();
        }
    }

    public class GeneratedDesignConfiguration : IEntityTypeConfiguration<GeneratedDesign>
    {
        public void Configure(EntityTypeBuilder<GeneratedDesign> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.ImageUrl).IsRequired();
            builder.Property(d => d.Prompt).IsRequired();
        }
    }

    public class SellerRecommendationConfiguration : IEntityTypeConfiguration<SellerRecommendation>
    {
        public void Configure(EntityTypeBuilder<SellerRecommendation> builder)
        {
            builder.HasKey(sr => sr.Id);
            builder.Property(sr => sr.EstimatedPrice).HasPrecision(18, 2);

            builder.HasOne(sr => sr.Shop)
                .WithMany()
                .HasForeignKey(sr => sr.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(sr => sr.CustomRequestId);
            builder.HasIndex(sr => sr.ShopId);
        }
    }

    public class CustomOfferConfiguration : IEntityTypeConfiguration<CustomOffer>
    {
        public void Configure(EntityTypeBuilder<CustomOffer> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Price).HasPrecision(18, 2);

            builder.HasOne(o => o.Shop)
                .WithMany()
                .HasForeignKey(o => o.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(o => o.CustomRequestId);
            builder.HasIndex(o => o.ShopId);
            builder.HasIndex(o => o.CreatedAt);
        }
    }

    public class ProjectWorkspaceConfiguration : IEntityTypeConfiguration<ProjectWorkspace>
    {
        public void Configure(EntityTypeBuilder<ProjectWorkspace> builder)
        {
            builder.HasKey(w => w.Id);

            builder.HasOne(w => w.SelectedOffer)
                .WithMany()
                .HasForeignKey(w => w.SelectedOfferId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(w => w.ChatConversation)
                .WithMany()
                .HasForeignKey(w => w.ChatConversationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(w => w.CustomRequestId).IsUnique();
            builder.HasIndex(w => w.SelectedOfferId);
            builder.HasIndex(w => w.ChatConversationId);
        }
    }

    public class CustomStudioSettingConfiguration : IEntityTypeConfiguration<CustomStudioSetting>
    {
        public void Configure(EntityTypeBuilder<CustomStudioSetting> builder)
        {
            builder.HasKey(s => s.Id);
        }
    }

    public class CustomStudioAuditLogConfiguration : IEntityTypeConfiguration<CustomStudioAuditLog>
    {
        public void Configure(EntityTypeBuilder<CustomStudioAuditLog> builder)
        {
            builder.HasKey(l => l.Id);
        }
    }
}
