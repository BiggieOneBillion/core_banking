// CoreBanking.Core/Events/MoneyTransferredEvent.cs
using CoreBanking.Core.Entities;
using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Core.Events;

public class MoneyTransferredEvent : IDomainEvent
{
    public Account SourceAccount { get; }
    public Account DestinationAccount { get; }
    public Money Amount { get; }
    public string Reference { get; }
    public DateTime OccurredOn { get; }

    public MoneyTransferredEvent(Account sourceAccount, Account destinationAccount, Money amount, string reference)
    {
        SourceAccount = sourceAccount;
        DestinationAccount = destinationAccount;
        Amount = amount;
        Reference = reference;
        OccurredOn = DateTime.UtcNow;
    }
}