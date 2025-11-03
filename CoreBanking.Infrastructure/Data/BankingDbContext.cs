using CoreBanking.Core.Entities;
using CoreBanking.Core.Enums;
using CoreBanking.Core.ValueObjects;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity properties and relationships here if needed

            modelBuilder.Entity<Customer>(entity =>
             {
                 entity.HasKey(e => e.CustomerId);
                 entity.Property(c => c.CustomerId).HasConversion(customerId => customerId.Value, value => new CustomerId(value));
                 entity.Property(c => c.Firstname).IsRequired().HasMaxLength(100);
                 entity.Property(c => c.Lastname).IsRequired().HasMaxLength(100);

                 entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                 entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);

                 entity.HasMany(c => c.Accounts)
                 .WithOne(a => a.Customer)
                 .HasForeignKey(a => a.CustomerId);

             });

            // Account entity configuration
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.AccountId);
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

            modelBuilder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Account>().HasQueryFilter(a => !a.IsDeleted);

            modelBuilder.Entity<Customer>().HasData(new {
			CustomerId = Guid.Parse("a1b2c3d4-1234-5678-9abc-123456789abc"),
			FirstName = "Alice",
			LastName = "Johnson",
			Email = "alice.johnson@email.com",
			PhoneNumber = "555-0101",
			DateCreated = DateTime.UtcNow.AddDays(-30),
			IsActive = true,
			IsDeleted = false
		     }
	        );

	        modelBuilder.Entity<Account>().HasData(new {
			// AccountId = Guid.Parse("c3d4e5f6-3456-7890-cde1-345678901cde"),
			AccountId = AccountId.Create(Guid.Parse("c3d4e5f6-3456-7890-cde1-345678901cde")),
			AccountNumber = AccountNumber.Create("1000000001"), // maps to AccountNumber.Value
			AccountType = AccountType.Checkings, // EF handles enum conversion
			CustomerId = Guid.Parse("a1b2c3d4-1234-5678-9abc-123456789abc"),
			BalanceAmount = 1500.00m, // maps to Money.Amount
			Currency = "NGN",
			DateOpened = DateTime.UtcNow.AddDays(-20),
			IsActive = true,
			IsDeleted = false
	        }
           ); 


        }
    }
}
