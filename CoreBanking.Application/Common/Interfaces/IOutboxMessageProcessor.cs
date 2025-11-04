using System;

namespace CoreBanking.APP.Common.Interfaces;

public interface IOutboxMessageProcessor
{
    Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken);

}
