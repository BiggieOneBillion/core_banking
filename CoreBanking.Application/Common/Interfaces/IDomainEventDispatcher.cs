using System;

namespace CoreBanking.Application.Common.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchDomainEventAsync(CancellationToken cancellationToken);
}
