using HandoraDomain.Models.ChatEntities;
using HandoraDomain.Models.CustomStudioEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Data.Configuration
{

    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.HasKey(c => c.Id);

            builder.HasOne(c => c.Buyer)
                .WithMany()
                .HasForeignKey(c => c.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.Seller)
                .WithMany()
                .HasForeignKey(c => c.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.ActiveDesignRequest)
                .WithMany()
                .HasForeignKey(c => c.ActiveDesignRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.DesignRequests)
                .WithOne(r => r.Conversation)
                .HasForeignKey(r => r.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
