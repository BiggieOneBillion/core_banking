using System;
using CoreBanking.Core.Enums;
using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Core.Entities;

public class Transaction
{
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public string Description { get; private set; }
    public DateTime Timestamp { get; private set; }

    public string Reference { get; private set; }


    private Transaction() {}

    public Transaction(Guid accountId, TransactionType type, Money amount, string description)
    {
        TransactionId = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Timestamp = DateTime.UtcNow;
        Reference = GenerateReference();

    }

    private string GenerateReference()
    {
        return $"{Timestamp:yyyMMddHHmmss} - {TransactionId.ToString().Substring(0, 8)}";
    }
}
