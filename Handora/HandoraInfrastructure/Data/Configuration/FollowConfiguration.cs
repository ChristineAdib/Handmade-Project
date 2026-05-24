using HandoraDomain.Models.FollowEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HandoraInfrastructure.Data.Configuration
{
    public class FollowConfiguration : IEntityTypeConfiguration<Follow>
    {
        public void Configure(EntityTypeBuilder<Follow> builder)
        {
            builder.HasKey(f => f.Id);

            builder.HasIndex(f => new { f.UserId, f.ShopId })
                .IsUnique();

            builder.HasOne(f => f.User)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Shop)
                .WithMany(s => s.Followers)
                .HasForeignKey(f => f.ShopId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}