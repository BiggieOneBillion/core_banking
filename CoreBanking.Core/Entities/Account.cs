using System;
using System.Transactions;
using CoreBanking.Core.Common;
using CoreBanking.Core.Enums;
using CoreBanking.Core.Events;
using CoreBanking.Core.Interface;
using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Core.Entities;

public class Account : ISoftDelete
    {
        public AccountId AccountId { get; private set; }
        public AccountNumber AccountNumber { get; private set; }
        public AccountType AccountType { get; private set; }
        public Money Balance { get; private set; }
        public CustomerId CustomerId { get; private set; }
        public Customer Customer { get; private set; }
        public DateTime DateOpened { get; private set; }
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
        public bool IsActive { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public string? DeletedBy { get; private set; }

        // Domain events collection
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        // Navigation properties - private to enforce aggregate boundary
        private readonly List<Transaction> _transactions = new();
        public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

        private Account() { } // EF Core needs this

        // Option 1: Keep existing constructor for internal use
        private Account(AccountNumber accountNumber, AccountType accountType, CustomerId customerId)
        {
            AccountId = AccountId.Create();
            AccountNumber = accountNumber;
            AccountType = accountType;
            CustomerId = customerId;
            Balance = new Money(0);
            DateOpened = DateTime.UtcNow;
            IsActive = true;
        }

        // Core banking operations - these are the aggregate's public API
        public Transaction Deposit(Money amount, Account account, string description = "Deposit")
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot deposit to inactive account");

            if (amount.Amount <= 0)
                throw new ArgumentException("Deposit amount must be positive");

            Balance += amount;

            var transaction = new Transaction(
                accountId: AccountId,
                account: account,
                type: TransactionType.Deposit,
                amount: amount,
                description: description
            );

            _transactions.Add(transaction);
            return transaction;
        }

        public Transaction Withdraw(Money amount, Account account, string description = "Withdrawal")
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot withdraw from inactive account");

            if (amount.Amount <= 0)
                throw new ArgumentException("Withdrawal amount must be positive");

            if (Balance.Amount < amount.Amount)
                throw new InvalidOperationException("Insufficient funds");

            // Special business rule for Savings accounts
            if (AccountType == AccountType.Savings && _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
                throw new InvalidOperationException("Savings account withdrawal limit reached");

            Balance -= amount;

            var transaction = new Transaction(
                accountId: AccountId,
                account: account,
                type: TransactionType.Withdrawal,
                amount: amount,
                description: description
            );

            _transactions.Add(transaction);
            return transaction;
        }

        public static Account Create(
            CustomerId customerId,
            AccountNumber accountNumber,
            AccountType accountType,
            Money initialBalance)
        {
            // Domain validation
            if (initialBalance.Amount < 0)
                throw new InvalidOperationException("Initial balance cannot be negative");

            if (initialBalance.Amount > 1000000)
                throw new InvalidOperationException("Initial deposit too large");

            // Create account using private constructor
            var account = new Account(
                accountNumber: accountNumber,
                accountType: accountType,
                customerId: customerId
            )
            {
                Balance = initialBalance // Set initial balance after construction
            };

            // Raise domain event if needed
            account.AddDomainEvent(new AccountCreatedEvent(accountId:account.AccountId,accountNumber:account.AccountNumber, customerId:account.CustomerId,accountType:account.AccountType, initialDeposit:account.Balance));

            return account;
        }

        // Add to CoreBanking.Core/Entities/Account.cs
        public void Transfer(Money amount, Account destination, string reference, string description)
        {
            // Validate inputs
            if (destination == null)
                throw new ArgumentNullException(nameof(destination), "Destination account cannot be null");

            if (amount.Amount <= 0)
                throw new InvalidOperationException("Transfer amount must be positive");

            if (this == destination)
                throw new InvalidOperationException("Cannot transfer to the same account");

            // Check source account conditions
            if (!IsActive)
                throw new InvalidOperationException("Source account is not active");

            if (!destination.IsActive)
                throw new InvalidOperationException("Destination account is not active");

            // Check sufficient funds
            if (Balance.Amount < amount.Amount)
                throw new InvalidOperationException("Insufficient funds for transfer");

            // Special business rules for Savings accounts
            if (AccountType == AccountType.Savings && _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
                throw new InvalidOperationException("Savings account withdrawal limit reached");

            // Execute the transfer as an atomic operation
            // Withdraw from source
            Balance -= amount;
            var withdrawalTransaction = new Transaction(
                accountId: AccountId,
                account: this,
                type: TransactionType.Withdrawal,
                amount: amount,
                description: $"Transfer to {destination.AccountNumber.Value}: {description}",
                reference: reference
            );
            _transactions.Add(withdrawalTransaction);

            // Deposit to destination
            destination.Balance += amount;
            var depositTransaction = new Transaction(
                accountId: destination.AccountId,
                account: destination,
                type: TransactionType.Deposit,
                amount: amount,
                description: $"Transfer from {AccountNumber.Value}: {description}",
                reference: reference
            );
            destination._transactions.Add(depositTransaction);

            // Raise domain events for the transfer
            AddDomainEvent(new MoneyTransferredEvent(this, destination, amount, reference));
        }

        // Domain event methods
        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        public void CloseAccount()
        {
            if (Balance.Amount != 0)
                throw new InvalidOperationException("Cannot close account with non-zero balance");

            IsActive = false;
        }

        public void UpdateBalance(Money newBalance)
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot update balance for inactive account.");

            Balance = newBalance;
        }
    }

// public class Account : ISoftDelete
//    {
//        public AccountId AccountId { get; private set; }
//        public AccountNumber AccountNumber { get; private set; }
//        public AccountType AccountType { get; private set; }
//        public Money Balance { get; private set; }
//        public CustomerId CustomerId { get; private set; }
//        public Customer Customer { get; private set; }
//        public DateTime DateOpened { get; private set; }
//        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
//        public bool IsActive { get; private set; }
//        public bool IsDeleted { get; private set; }
//        public DateTime? DeletedAt { get; private set; }
//        public string? DeletedBy { get; private set; }

//        // Domain events collection
//        private readonly List<IDomainEvent> _domainEvents = new();
//        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

//        // Navigation properties - private to enforce aggregate boundary
//        private readonly List<Transaction> _transactions = new();
//        public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

//        private Account() { } // EF Core needs this

//        // Option 1: Keep existing constructor for internal use
//        private Account(AccountNumber accountNumber, AccountType accountType, CustomerId customerId)
//        {
//            AccountId = AccountId.Create();
//            AccountNumber = accountNumber;
//            AccountType = accountType;
//            CustomerId = customerId;
//            Balance = new Money(0);
//            DateOpened = DateTime.UtcNow;
//            IsActive = true;
//        }

//        // Core banking operations - these are the aggregate's public API
//        public Transaction Deposit(Money amount, Account account, string description = "Deposit")
//        {
//            if (!IsActive)
//                throw new InvalidOperationException("Cannot deposit to inactive account");

//            if (amount.Amount <= 0)
//                throw new ArgumentException("Deposit amount must be positive");

//            Balance += amount;

//            var transaction = new Transaction(
//                accountId: AccountId,
//                account: account,
//                type: TransactionType.Deposit,
//                amount: amount,
//                description: description
//            );

//            _transactions.Add(transaction);
//            return transaction;
//        }

//        public Transaction Withdraw(Money amount, Account account, string description = "Withdrawal")
//        {
//            if (!IsActive)
//                throw new InvalidOperationException("Cannot withdraw from inactive account");

//            if (amount.Amount <= 0)
//                throw new ArgumentException("Withdrawal amount must be positive");

//            if (Balance.Amount < amount.Amount)
//                throw new InvalidOperationException("Insufficient funds");

//            // Special business rule for Savings accounts
//            if (AccountType == AccountType.Savings && _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
//                throw new InvalidOperationException("Savings account withdrawal limit reached");

//            Balance -= amount;

//            var transaction = new Transaction(
//                accountId: AccountId,
//             //    account: account,
//                type: TransactionType.Withdrawal,
//                amount: amount,
//                description: description
//            );

//            _transactions.Add(transaction);
//            return transaction;
//        }

//        public static Account Create(
//            CustomerId customerId,
//            AccountNumber accountNumber,
//            AccountType accountType,
//            Money initialBalance)
//        {
//            // Domain validation
//            if (initialBalance.Amount < 0)
//                throw new InvalidOperationException("Initial balance cannot be negative");

//            if (initialBalance.Amount > 1000000)
//                throw new InvalidOperationException("Initial deposit too large");

//            // Create account using private constructor
//            var account = new Account(
//                accountNumber: accountNumber,
//                accountType: accountType,
//                customerId: customerId
//            )
//            {
//                Balance = initialBalance // Set initial balance after construction
//            };

//            // Raise domain event if needed
//            account.AddDomainEvent(new AccountCreatedEvent(account));

//            return account;
//        }

//        // Add to CoreBanking.Core/Entities/Account.cs
//        public void Transfer(Money amount, Account destination, string reference, string description)
//        {
//            // Validate inputs
//            if (destination == null)
//                throw new ArgumentNullException(nameof(destination), "Destination account cannot be null");

//            if (amount.Amount <= 0)
//                throw new InvalidOperationException("Transfer amount must be positive");

//            if (this == destination)
//                throw new InvalidOperationException("Cannot transfer to the same account");

//            // Check source account conditions
//            if (!IsActive)
//                throw new InvalidOperationException("Source account is not active");

//            if (!destination.IsActive)
//                throw new InvalidOperationException("Destination account is not active");

//            // Check sufficient funds
//            if (Balance.Amount < amount.Amount)
//                throw new InvalidOperationException("Insufficient funds for transfer");

//            // Special business rules for Savings accounts
//            if (AccountType == AccountType.Savings && _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
//                throw new InvalidOperationException("Savings account withdrawal limit reached");

//            // Execute the transfer as an atomic operation
//            // Withdraw from source
//            Balance -= amount;
//            var withdrawalTransaction = new Transaction(
//                accountId: AccountId,
//                account: this,
//                type: TransactionType.TransferOut,
//                amount: amount,
//                description: $"Transfer to {destination.AccountNumber.Value}: {description}",
//                reference: reference
//            );
//            _transactions.Add(withdrawalTransaction);

//            // Deposit to destination
//            destination.Balance += amount;
//            var depositTransaction = new Transaction(
//                accountId: destination.AccountId,
//                account: destination,
//                type: TransactionType.TransferIn,
//                amount: amount,
//                description: $"Transfer from {AccountNumber.Value}: {description}",
//                reference: reference
//            );
//            destination._transactions.Add(depositTransaction);

//            // Raise domain events for the transfer
//            AddDomainEvent(new MoneyTransferredEvent(this, destination, amount, reference));
//        }

//        // Domain event methods
//        public void AddDomainEvent(IDomainEvent domainEvent)
//        {
//            _domainEvents.Add(domainEvent);
//        }

//        public void ClearDomainEvents()
//        {
//            _domainEvents.Clear();
//        }

//        public void CloseAccount()
//        {
//            if (Balance.Amount != 0)
//                throw new InvalidOperationException("Cannot close account with non-zero balance");

//            IsActive = false;
//        }

//        public void UpdateBalance(Money newBalance)
//        {
//            if (!IsActive)
//                throw new InvalidOperationException("Cannot update balance for inactive account.");

//            Balance = newBalance;
//        }
//    }

// public class Account : ISoftDelete

// {
//      public AccountId _AccountId { get; private set; }

//      public Customer Customer { get; private set; }
//     public AccountNumber AccountNumber { get; private set; }
//     public AccountType AccountType { get; private set; }
//     public Money Balance { get; private set; }
//     public CustomerId CustomerId { get; private set; }

//     public DateTime DateOpened { get; private set; }

//     public bool IsActive { get; private set; }

//     public bool IsDeleted { get; private set; }
//     public DateTime? DeletedAt { get; private set; }
//     public string? DeletedBy { get; private set; }

//    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();


//     private readonly List<Transaction> _transactions = new();

//     public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

//     private Account() { }

//     public Account(AccountNumber accountNumber, AccountType accountType, Guid customerId)
//     {
//         _AccountId = AccountId.Create();
//         AccountNumber = accountNumber;
//         AccountType = accountType;
//         CustomerId = CustomerId.Create();
//         Balance = new Money(0);
//         DateOpened = DateTime.UtcNow;
//         IsActive = true;
//     }

//     public Transaction Deposit(Money amount, string description = "Deposit")
//     {
//         if (!IsActive)
//             throw new InvalidOperationException("Cannot deposit to inactive account");

//         if (amount.Amount <= 0)
//             throw new ArgumentException("Deposit amount must be positive");

//         Balance += amount;

//         var transaction = new Transaction(
//             accountId: _AccountId,
//             type: TransactionType.Deposit,
//             amount: amount,
//             description: description
//         );

//         _transactions.Add(transaction);
//         return transaction;
//     }

//     public Transaction Withdraw(Money amount, string description = "withdrawal")
//     {

//         if (!IsActive)
//             throw new InvalidOperationException("Cannot deposit to inactive account");

//         if (amount.Amount <= 0)
//             throw new ArgumentException("Deposit amount must be positive");

//         if (Balance.Amount < amount.Amount)
//             throw new InvalidOperationException("Insufficient funds");

//         if (AccountType == AccountType.Savings && _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
//             throw new InvalidOperationException("Savings account withdrawal limit reached");

//         Balance -= amount;

//         var transaction = new Transaction(
//            accountId: _AccountId,
//            type: TransactionType.Withdrawal,
//            amount: amount,
//            description: description
//        );

//         _transactions.Add(transaction);

//         return transaction;

//     }

//     public void CloseAccount()
//     {
//         if (Balance.Amount != 0)
//             throw new InvalidOperationException("Cannot close account with non-zero balance");

//         IsActive = false;
//     }
    
//     public void SoftDelete(string deletedBy)
//     {
//         if (Balance.Amount != 0)
//             throw new InvalidOperationException("Cannot close account with non-zero balance");

//         IsDeleted = true;
//         DeletedAt = DateTime.UtcNow;
//         DeletedBy = deletedBy;
//     }

   

// }
