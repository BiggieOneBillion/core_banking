using System.Xml.Schema;
using CoreBanking.APP.Common;
using CoreBanking.Core.Common;
using CoreBanking.Core.Enums;
using CoreBanking.Core.Events;
using CoreBanking.Core.Interface;
using CoreBanking.Core.ValueObjects;
namespace CoreBanking.Core.Entities;

public class Account : AggregateRoot<AccountId>, ISoftDelete
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

    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Navigation properties - private to enforce aggregate boundary
    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();



#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Account() { } // EF Core needs this
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    // Option 1: Keep existing constructor for internal use
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Account(AccountNumber accountNumber, AccountType accountType, CustomerId customerId)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        AccountId = AccountId.Create();
        AccountNumber = accountNumber;
        AccountType = accountType;
        CustomerId = customerId;
        Balance = new Money(0);
        DateOpened = DateTime.UtcNow;
        IsActive = true;
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
            account.AddDomainEvent(new AccountCreatedEvent(
                accountId: account.AccountId,
                accountNumber: account.AccountNumber,
                customerId: account.CustomerId,
                accountType: account.AccountType,
                initialDeposit: account.Balance
            ));

            return account;
        }

    // Core banking operations - these are the aggregate's public API
    public Transaction Deposit(Money amount, string description = "Deposit")
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot deposit to inactive account");

        if (amount.Amount <= 0)
            throw new ArgumentException("Deposit amount must be positive");

        Balance += amount;

        var transaction = new Transaction(
            accountId: AccountId,
            type: TransactionType.Deposit,
            amount: amount,
            description: description,
            account:this
        );

        _transactions.Add(transaction);
        return transaction;
    }



    public Transaction Withdraw(Money amount, string description = "Withdrawal")
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
        type: TransactionType.Withdrawal,
        amount: amount,
        description: description,
        account: this
        );

        _transactions.Add(transaction);
        return transaction;
    }

    // public static Account Create(
    //     CustomerId customerId,
    //     AccountNumber accountNumber,
    //     AccountType accountType,
    //     Money initialBalance)
    // {
    //     // Domain validation
    //     if (initialBalance.Amount < 0)
    //         throw new InvalidOperationException("Initial balance cannot be negative");

    //     if (initialBalance.Amount > 1000000)
    //         throw new InvalidOperationException("Initial deposit too large");

    //     // Create account using private constructor
    //     var account = new Account(
    //         accountNumber: accountNumber,
    //         accountType: accountType,
    //         customerId: customerId
    //     )
    //     {
    //         Balance = initialBalance // Set initial balance after construction
    //     };

    //     // Raise domain event if needed
    //     account.AddDomainEvent(new AccountCreatedEvent(accountId: account.AccountId, accountNumber: account.AccountNumber, customerId: account.CustomerId, accountType: account.AccountType, initialDeposit: account.Balance));

    //     return account;
    // }



    public Result Transfer(Money amount, Account destination, string reference, string description)
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
        {
            // Raise insufficient funds event
            _domainEvents.Add(new InsufficientFundsEvent(
                AccountNumber, amount, Balance, "Transfer"));

            return Result.Failure("Insufficient funds for transfer");
        }

        // Special business rules for Savings accounts
        if (AccountType == AccountType.Savings &&
            _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
        {
            return Result.Failure("Savings account withdrawal limit reached");
        }

        // Execute the transfer as an atomic operation
        var debitResult = Debit(amount, $"Transfer to {destination.AccountNumber}", reference);
        if (!debitResult.IsSuccess)
            return debitResult;

        var creditResult = destination.Credit(amount, $"Transfer from {AccountNumber}", reference);
        if (!creditResult.IsSuccess)
            return creditResult;

        // Raise money transferred event
        var transactionId = TransactionId.Create();
        _domainEvents.Add(new MoneyTransferedEVent(
            transactionId, AccountNumber, destination.AccountNumber, amount, reference));

        // Return success result
        return Result.Success();
    }
    public Result Debit(Money amount, string description, string reference)
    {
        if (IsDeleted)
            return Result.Failure("Cannot debit a deleted account");

        if (amount.Amount <= 0)
            return Result.Failure("Debit amount must be positive");

        if (Balance.Amount < amount.Amount)
            return Result.Failure("Insufficient funds");

        // Apply debit
        Balance -= amount;

        // Record transaction (matches your Transaction constructor)
        var transaction = new Transaction(
            AccountId,
            TransactionType.Withdrawal,
            amount,
            description,
            account:this,
            reference
        );

        _transactions.Add(transaction);

        // Raise domain event
        //AddDomainEvent(new AccountDebitedEvent(Id, amount, reference));

        return Result.Success();
    }

    public Result Credit(Money amount, string description, string reference)
    {
        if (IsDeleted)
            return Result.Failure("Cannot credit a deleted account");

        if (amount.Amount <= 0)
            return Result.Failure("Credit amount must be positive");

        // Apply credit
        Balance += amount;

        // Record transaction (matches your Transaction constructor)
        var transaction = new Transaction(
           AccountId,
            TransactionType.Withdrawal,
            amount,
            description,
            account:this,
            reference
        );

        _transactions.Add(transaction);

        // Raise domain event
        //AddDomainEvent(new AccountCreditedEvent(Id, amount, reference));

        return Result.Success();
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