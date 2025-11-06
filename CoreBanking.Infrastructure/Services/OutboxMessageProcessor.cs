// using System;
// using System.Text.Json;
// using CoreBanking.Core.Common;
// using CoreBanking.Infrastructure.Persistence.Outbox;
// using CoreBankingTest.Infra.Data;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// namespace CoreBanking.Infrastructure.Services;

//     public class OutboxMessageProcessor : IOutboxMessageProcessor
//     {
//         private readonly BankingDbContext _context;
//         private readonly IEventBus _eventBus;
//         private readonly ILogger<OutboxMessageProcessor> _logger;

//         public OutboxMessageProcessor(BankingDbContext context, IEventBus eventBus, 
//             ILogger<OutboxMessageProcessor> logger)
//         {
//             _context = context;
//             _eventBus = eventBus;
//             _logger = logger;
//         }

//         public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
//         {
//             var messages = await _context.OutboxMessages
//                 .Where(x => x.ProcessedOn == null && x.RetryCount < 3)
//                 .OrderBy(x => x.OccurredOn)
//                 .Take(20)
//                 .ToListAsync(cancellationToken);

//             foreach (var message in messages)
//             {
//                 try
//                 {
//                     var domainEvent = DeserializeMessage(message);
//                     if (domainEvent != null)
//                     {
//                         await _eventBus.PublishAsync(domainEvent, cancellationToken);
//                     }

//                     message.ProcessedOn = DateTime.UtcNow;
//                     message.Error = null;
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
//                     message.RetryCount++;
//                     message.Error = ex.Message;
//                 }
//             }

//             await _context.SaveChangesAsync(cancellationToken);
//         }

//         private static IDomainEvent? DeserializeMessage(OutboxMessage message)
//         {
//             var eventType = Type.GetType($"CoreBanking.Core.Accounts.Events.{message.Type}, CoreBanking.Core");
//             if (eventType == null) 
//                 return null;

//             return JsonSerializer.Deserialize(message.Content, eventType) as IDomainEvent;
//         }
//     }

using System.Text.Json;
using CoreBanking.Core.Common;
using CoreBanking.Core.Interface;
using CoreBanking.Infrastructure.Persistence.Outbox;
using CoreBanking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace CoreBanking.Infrastructure.Services;

public class OutboxMessageProcessor : IOutboxMessageProcessor
{
    private readonly BankingDbContext _context;
    // private readonly IEventBus _eventBus;
    private readonly ILogger<OutboxMessageProcessor> _logger;

    public OutboxMessageProcessor(BankingDbContext context,
        ILogger<OutboxMessageProcessor> logger)
    {
        _context = context;
        // _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _context.OutboxMessages
            .Where(x => x.ProcessedOn == null && x.RetryCount < 3)
            .OrderBy(x => x.OccurredOn)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var domainEvent = DeserializeMessage(message);
                // if (domainEvent != null)
                // {
                //     await _eventBus.PublishAsync(domainEvent, cancellationToken);
                // }

                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                message.RetryCount++;
                message.Error = ex.Message;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IDomainEvent? DeserializeMessage(OutboxMessage message)
    {
        var eventType = Type.GetType($"CoreBanking.Core.Accounts.Events.{message.Type}, CoreBanking.Core");
        if (eventType == null)
            return null;

        return JsonSerializer.Deserialize(message.Content, eventType) as IDomainEvent;
    }
}
