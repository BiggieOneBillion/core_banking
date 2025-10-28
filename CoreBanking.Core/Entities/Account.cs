using System;
using System.Transactions;
using CoreBanking.Core.Enums;
using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Core.Entities;

public class Account
{
     public Guid AccountId { get; private set; }
    public AccountNumber AccountNumber { get; private set; }
    public AccountType AccountType { get; private set; }
    public Money Balance { get; private set; }
    public Guid CustomerId { get; private set; }

    public DateTime DateOpened { get; private set; }

    public bool IsActive { get; private set; }

    private readonly List<Transaction> _transactions = new();

    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private Account() { }

    public Account(AccountNumber accountNumber, AccountType accountType, Guid customerId)
    {
        AccountId = Guid.NewGuid();
        AccountNumber = accountNumber;
        AccountType = accountType;
        CustomerId = customerId;
        Balance = new Money(0);
        DateOpened = DateTime.UtcNow;
        IsActive = true;
    }

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
            description: description
        );

        _transactions.Add(transaction);
        return transaction;
    }

    public Transaction Withdraw(Money amount, string description = "withdrawal")
    {

        if (!IsActive)
            throw new InvalidOperationException("Cannot deposit to inactive account");

        if (amount.Amount <= 0)
            throw new ArgumentException("Deposit amount must be positive");

        if (Balance.Amount < amount.Amount)
            throw new InvalidOperationException("Insufficient funds");

        if (AccountType == AccountType.Savings && _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
            throw new InvalidOperationException("Savings account withdrawal limit reached");

        Balance -= amount;

        var transaction = new Transaction(
           accountId: AccountId,
           type: TransactionType.Withdrawal,
           amount: amount,
           description: description
       );

        _transactions.Add(transaction);

        return transaction;

    }
    
    public void CloseAccount()
    {
        if (Balance.Amount != 0)
            throw new InvalidOperationException("Cannot close account with non-zero balance");

        IsActive = false;
    }

}
