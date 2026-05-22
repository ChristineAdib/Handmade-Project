using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CartEntities;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.WishListEntoties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Data.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);

            builder.Property(u => u.ProfileImage)
                .HasMaxLength(500);

            builder.Property(u => u.Bio)
                .HasMaxLength(1000);

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            // Soft delete filter
            //builder.HasQueryFilter(u => !u.IsDeleted);

            // Relationships
            builder.HasMany(u => u.Addresses)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Reviews)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.WishList)
                .WithOne(w => w.User)
                .HasForeignKey<WishList>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Shop)
                .WithOne(s => s.Owner)
                .HasForeignKey<Shop>(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
