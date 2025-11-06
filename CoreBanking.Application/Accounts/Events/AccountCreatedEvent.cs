// CoreBanking.Core/Events/AccountCreatedEvent.cs
using CoreBanking.Core.Common;
using CoreBanking.Core.Entities;
using CoreBanking.Core.Interface;

namespace CoreBanking.Core.Events;

public class AccountCreatedEvent : IDomainEvent
{
    public Account Account { get; }
    public DateTime OccurredOn { get; }

    public Guid EventId => throw new NotImplementedException();

    public string EventType => throw new NotImplementedException();

    public AccountCreatedEvent(Account account)
    {
        Account = account;
        OccurredOn = DateTime.UtcNow;
    }
}