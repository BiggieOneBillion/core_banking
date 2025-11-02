// CoreBanking.Core/Events/AccountCreatedEvent.cs
using CoreBanking.Core.Common;
using CoreBanking.Core.Entities;

namespace CoreBanking.Core.Events;

public class AccountCreatedEvent : IDomainEvent
{
    public Account Account { get; }
    public DateTime OccurredOn { get; }

    public AccountCreatedEvent(Account account)
    {
        Account = account;
        OccurredOn = DateTime.UtcNow;
    }
}