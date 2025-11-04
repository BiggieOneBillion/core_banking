using CoreBanking.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.OccurredOn).IsRequired();
            builder.Property(x => x.ProcessedOn).IsRequired(false);
            builder.Property(x => x.Error).HasMaxLength(1000);
            builder.Property(x => x.RetryCount).HasDefaultValue(0);
        }
    }

