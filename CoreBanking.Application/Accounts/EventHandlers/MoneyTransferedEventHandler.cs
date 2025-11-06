using MediatR;
using Microsoft.Extensions.Logging;

public class MoneyTransferedEventHandler : INotificationHandler<MoneyTransferedEVent>
    {
        private readonly ILogger<MoneyTransferedEventHandler> _logger;
        //private readonly INotificationService _notificationService;
        //private readonly IReportingService _reportingService;

        //public MoneyTransferedEventHandler(ILogger<MoneyTransferedEventHandler> logger, INotificationService notificationService, IReportingService reportingService)
        public MoneyTransferedEventHandler(ILogger<MoneyTransferedEventHandler> logger)
        {
            _logger = logger;
            //_notificationService = notificationService;
            //_reportingService = reportingService;
        }

        public async Task Handle(MoneyTransferedEVent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing money transferred event for transaction {TransactionId}",
                notification.TransactionId);

            // Send notifications to both parties
            /*await _notificationService.SendTransferNotificationAsync(
                notification.SourceAccountNumber,
                notification.DestinationAccountNumber,
                notification.Amount);

            // Update reporting and analytics
            await _reportingService.RecordTransactionAsync(notification);*/

            // Could also: Update fraud detection, trigger compliance monitoring, etc.
            _logger.LogInformation("Successfully processed money transferred event");
        }
    }
