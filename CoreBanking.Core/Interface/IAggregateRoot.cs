using System;
using CoreBanking.Core.Common;
using CoreBanking.Core.Interface;

namespace CoreBanking.Core.Interface;

public interface IAggregateRoot
{
  IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
