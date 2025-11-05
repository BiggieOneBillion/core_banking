using System.Text.Json;
using CoreBanking.Core.Common;
using CoreBanking.Core.Entities;
using CoreBanking.Core.Enums;
using CoreBanking.Core.ValueObjects;
using CoreBanking.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
namespace CoreBankingTest.Infra.Data
{
    public class BankingDbContext : DbContext
    {

        public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());


            // Configure entity properties and relationships here if needed

            modelBuilder.Entity<Customer>(entity =>
             {
                 entity.HasKey(e => e.CustomerId);
                 entity.Property(c => c.CustomerId)
                 .HasConversion(
                    customerId => customerId.Value,
                    value => new CustomerId(value)
                 ).HasColumnName("CustomerId");
                 entity.Property(c => c.Firstname).IsRequired().HasMaxLength(100);
                 entity.Property(c => c.Lastname).IsRequired().HasMaxLength(100);

                 entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                 entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);

                 entity.HasMany(c => c.Accounts)
                 .WithOne(a => a.Customer)
                 .HasForeignKey(a => a.CustomerId);

                //  entity.Property(c => c.CustomerId)
                // .HasConversion(
                //     v => v.Value,            // to db (CustomerId -> Guid)
                //     v => new CustomerId(v)   // from db (Guid -> CustomerId)
                // )
                // .HasColumnName("CustomerId");

             });

            // Account entity configuration
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.Property(a => a.AccountId)
                    .HasConversion(
                        accountId => accountId.Value,
                        value => new AccountId(value));
                // entity.Property(e => e.AccountNumber).HasColumnName("AccountNumber").IsRequired().HasMaxLength(10);
                entity.Property(a => a.AccountNumber)
                        .HasConversion(
                            accountNumber => accountNumber.Value,
                            value => AccountNumber.Create(value))
                        .HasColumnName("AccountNumber")
                        .HasMaxLength(10)
                        .IsRequired();
                entity.OwnsOne(e => e.Balance, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("Balance Amount").HasPrecision(18, 2);
                    money.Property(m => m.Currency).HasColumnName("Balance Currency").HasMaxLength
              (3).HasDefaultValue("NGN");
                });

                entity.Property(a => a.AccountType).HasConversion<string>().IsRequired();

                // Account has many transactions
                entity.HasMany(a => a.Transactions)
                .WithOne(t => t.Account)
                .HasForeignKey(t => t.AccountId);

                // Ensure we don't accidentally load all transactions

                entity.Navigation(a => a.Transactions).AutoInclude(false);

                entity.Property(a => a.RowVersion)
           .IsRowVersion()
           .IsConcurrencyToken();

            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId);
                entity.Property(t => t.TransactionId)
                    .HasConversion(
                        transactionId => transactionId.Value,
                        value => new TransactionId(value));
                entity.Property(t => t.AccountId)
                    .HasConversion(
                        accountId => accountId.Value,
                        value => new AccountId(value));
                entity.OwnsOne(t => t.Amount, money =>
              {
                  money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2);
                  money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
              });

                entity.Property(t => t.Type).HasConversion<string>().IsRequired();

                entity.Property(t => t.Description).HasMaxLength(500);
                entity.Property(t => t.Reference).HasMaxLength(50);
                entity.Property(t => t.Timestamp).IsRequired();

            });

            // Query filters for soft delete
            modelBuilder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Account>().HasQueryFilter(a => !a.IsDeleted);

            // Seed data - provide raw values for value converters
            var customerIdValue = Guid.Parse("a1b2c3d4-1234-5678-9abc-123456789abc");
            var accountIdValue = Guid.Parse("c3d4e5f6-3456-7890-cde1-345678901cde");

            // // Seed Customer
            // modelBuilder.Entity<Customer>().HasData(new
            // {
            //     CustomerId = customerIdValue,
            //     Firstname = "Alice",
            //     Lastname = "Johnson",
            //     Email = "alice.johnson@email.com",
            //     PhoneNumber = "555-0101",
            //     DateCreated = new DateTime(2024, 12, 5, 0, 0, 0, DateTimeKind.Utc),
            //     IsActive = true,
            //     IsDeleted = false
            // });

            // // Seed Account
            // modelBuilder.Entity<Account>().OwnsOne(a => a.Balance).HasData(new
            // {
            //     AccountAccountId = accountIdValue,  // FK to parent Account
            //     Amount = 1500.00m,
            //     Currency = "NGN"
            // });

            // modelBuilder.Entity<Account>().HasData(new
            // {
            //     AccountId = accountIdValue,
            //     AccountNumber = "1000000001",
            //     AccountType = "Checkings",
            //     CustomerId = customerIdValue,
            //     DateOpened = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            //     IsActive = true,
            //     IsDeleted = false
            // });


        }
    
       public async Task SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default)
        {
            // Convert domain events to outbox messages
            var events = ChangeTracker.Entries<AggregateRoot<AccountId>>()
                .SelectMany(x => x.Entity.DomainEvents)
                .Select(domainEvent => new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = domainEvent.GetType().Name,
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    OccurredOn = domainEvent.OccurredOn
                })
                .ToList();

            // Clear domain events from aggregates
            ChangeTracker.Entries<AggregateRoot<AccountId>>()
                .ToList()
                .ForEach(entry => entry.Entity.ClearDomainEvents());

            // Save changes (including outbox messages) in single transaction
            await base.SaveChangesAsync(cancellationToken);

            // Add outbox messages after saving to ensure they're included in transaction
            if (events.Any())
            {
                await OutboxMessages.AddRangeAsync(events, cancellationToken);
                await base.SaveChangesAsync(cancellationToken);
            }
        }

    }
}
