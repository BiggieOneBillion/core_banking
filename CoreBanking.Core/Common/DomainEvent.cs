

using CoreBanking.Core.Interface;
using MediatR;

namespace CoreBanking.APP.Common;

public abstract record DomainEvent : IDomainEvent, INotification
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public string EventType => GetType().Name;

}
