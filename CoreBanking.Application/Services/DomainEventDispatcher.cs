using System;
using CoreBanking.Application.Common.Interfaces;
using CoreBanking.Core.Interface;
using CoreBanking.Infrastructure.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreBanking.Application.Services;

public class DomainEventDispatcher : IDomainEventDispatcher
{

    private readonly BankingDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        BankingDbContext context,
        IPublisher publisher,
        ILogger<DomainEventDispatcher> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }
    public async Task DispatchDomainEventAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            _logger.LogInformation("Dispatching domain event: {EventType}", domainEvent.GetType().Name);
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());
    }


}
