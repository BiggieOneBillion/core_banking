// // CoreBanking.Core/Events/AccountCreatedEvent.cs
// using CoreBanking.Core.Common;
// using CoreBanking.Core.Entities;

// namespace CoreBanking.Core.Events;

// public class AccountCreatedEvent : IDomainEvent
// {
//     public Account Account { get; }
//     public DateTime OccurredOn { get; }

//     public AccountCreatedEvent(Account account)
//     {
//         Account = account;
//         OccurredOn = DateTime.UtcNow;
//     }
// }
using CoreBanking.APP.Common;
using CoreBanking.Core.Enums;
using CoreBanking.Core.ValueObjects;

public record AccountCreatedEvent : DomainEvent
{
    public AccountId AccountId { get; }
    public AccountNumber AccountNumber { get; }
    public CustomerId CustomerId { get; }
    public AccountType AccountType { get; }
    public Money InitialDeposit { get; }
    public AccountCreatedEvent(AccountId accountId, AccountNumber accountNumber, CustomerId customerId, AccountType accountType, Money initialDeposit)
    {
        AccountId = accountId;
        AccountNumber = accountNumber;
        CustomerId = customerId;
        AccountType = accountType;
        InitialDeposit = initialDeposit;
    }

}

