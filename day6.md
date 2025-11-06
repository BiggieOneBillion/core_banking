Day 6 Lesson Plan: Advanced Patterns + gRPC Foundation
======================================================

> **Title:** Building Scalable Systems - Domain Events & gRPC Foundation

> **Theme:** Implementing Event-Driven Architecture with Domain Events and Introducing High-Performance gRPC Communication

Session 1: Domain Events & Outbox Pattern
-----------------------------------------

### Icebreaker & Event Thinking (15 mins)

**Ask:** "When you transfer money between banks, what happens behind the scenes after you click 'Send'? What systems need to know about this transaction beyond just the two accounts involved?"

**Expected Answers:** Fraud detection, notifications (SMS/email), transaction history, reporting, compliance monitoring, customer service systems.

**The Connection:** "Today we're implementing domain events and the outbox pattern to reliably communicate across different parts of our banking system - exactly what happens in real banking operations when transactions occur."

### Deep Dive into Domain Events

**What are Domain Events?**

*   **Domain Events:** Represent something that happened in the domain that other parts of the same domain might care about    
*   **Publish-Subscribe Pattern:** Loose coupling between event producers and consumers    
*   **Event-Driven Architecture:** Systems react to events rather than being called directly
    
**Domain Events in Banking Context:**

```csharp
    // CoreBanking.Core/Common/DomainEvent.cs
    namespace CoreBanking.Core.Common;

    public abstract record DomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType => GetType().Name;
    }

    // CoreBanking.Core/Events/AccountCreatedEvent.cs
    public record AccountCreatedEvent : DomainEvent
    {
        public AccountId AccountId { get; }
        public AccountNumber AccountNumber { get; }
        public CustomerId CustomerId { get; }
        public AccountType AccountType { get; }
        public Money InitialDeposit { get; }

        public AccountCreatedEvent(AccountId accountId, AccountNumber accountNumber, CustomerId customerId,
            AccountType accountType, Money initialDeposit)
        {
            AccountId = accountId;
            AccountNumber = accountNumber;
            CustomerId = customerId;
            AccountType = accountType;
            InitialDeposit = initialDeposit;
        }
    }

    // CoreBanking.Core/Events/MoneyTransferedEvent.cs
    public record MoneyTransferedEvent : DomainEvent
    {
        public TransactionId TransactionId { get; }
        public AccountNumber SourceAccountNumber { get; }
        public AccountNumber DestinationAccountNumber { get; }
        public Money Amount { get; }
        public string Reference { get; }
        public DateTime TransferDate { get; }

        public MoneyTransferedEvent(TransactionId transactionId, AccountNumber sourceAccountNumber, 
            AccountNumber destinationAccountNumber, Money amount, string reference)
        {
            TransactionId = transactionId;
            SourceAccountNumber = sourceAccountNumber;
            DestinationAccountNumber = destinationAccountNumber;
            Amount = amount;
            Reference = reference;
            TransferDate = DateTime.UtcNow;
        }
    }

    // CoreBanking.Core/Events/InsufficientFundsEvent.cs
    public record InsufficientFundsEvent : DomainEvent
    {
        public AccountNumber AccountNumber { get; }
        public Money RequestedAmount { get; }
        public Money CurrentBalance { get; }
        public string Operation { get; }

        public InsufficientFundsEvent(AccountNumber accountNumber, Money requestedAmount, 
            Money currentBalance, string operation)
        {
            AccountNumber = accountNumber;
            RequestedAmount = requestedAmount;
            CurrentBalance = currentBalance;
            Operation = operation;
        }
    }
```

**Raising Domain Events in Aggregate Roots:**

```csharp
    // CoreBanking.Core/Entities/Account.cs (enhanced)
    public class Account : ISoftDelete, AggregateRoot<AccountId>
    {
        // ... existing properties and methods

        private readonly List<DomainEvent> _domainEvents = new();
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public static Account Create(
            CustomerId customerId,
            AccountNumber accountNumber,
            AccountType accountType,
            Money initialBalance)
        {
            // Domain validation
            if (initialBalance.Amount < 0)
                throw new InvalidOperationException("Initial balance cannot be negative");

            if (initialBalance.Amount > 1000000)
                throw new InvalidOperationException("Initial deposit too large");

            // Create account using private constructor
            var account = new Account(
                accountNumber: accountNumber,
                accountType: accountType,
                customerId: customerId
            )
            {
                Balance = initialBalance // Set initial balance after construction
            };

            // Raise domain event if needed
            account.AddDomainEvent(new AccountCreatedEvent(
                accountId: account.AccountId,
                accountNumber: account.AccountNumber,
                customerId: account.CustomerId,
                accountType: account.AccountType,
                initialDeposit: account.Balance
            ));

            return account;
        }

        public void Transfer(Money amount, Account destination, string reference, string description)
        {
            // Validate inputs
            if (destination == null)
                throw new ArgumentNullException(nameof(destination), "Destination account cannot be null");

            if (amount.Amount <= 0)
                throw new InvalidOperationException("Transfer amount must be positive");

            if (this == destination)
                throw new InvalidOperationException("Cannot transfer to the same account");

            // Check source account conditions
            if (!IsActive)
                throw new InvalidOperationException("Source account is not active");

            if (!destination.IsActive)
                throw new InvalidOperationException("Destination account is not active");

            // Check sufficient funds
            if (Balance.Amount < amount.Amount)
            {
                // Raise insufficient funds event
                _domainEvents.Add(new InsufficientFundsEvent(
                    AccountNumber, amount, Balance, "Transfer"));

                throw new InvalidOperationException("Insufficient funds for transfer");
            }   

            // Special business rules for Savings accounts
            if (AccountType == AccountType.Savings && _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
                throw new InvalidOperationException("Savings account withdrawal limit reached");

            // Execute the transfer as an atomic operation
            var debitResult = Debit(amount, $"Transfer to {destination.AccountNumber}", reference);
            if (!debitResult.IsSuccess)
                return debitResult;

            var creditResult = destination.Credit(amount, $"Transfer from {AccountNumber}", reference);
            if (!creditResult.IsSuccess)
                return creditResult;

            // Raise money transferred event
            var transactionId = TransactionId.Create();
            _domainEvents.Add(new MoneyTransferedEvent(
                transactionId, AccountNumber, destination.AccountNumber, amount, reference));
        }
        ...
    }
```

### Deep Dive into Outbox Pattern

**The Problem: Dual Write Problem**

*   **Scenario:** Saving to database AND publishing events must be atomic    
*   **Risk:** Database save succeeds but event publishing fails (or vice versa)    
*   **Result:** Inconsistent state across the system    

**Outbox Pattern Solution:**
```csharp
    // CoreBanking.Infrastructure/Persistence/Outbox/OutboxMessage.cs
    namespace CoreBanking.Infrastructure.Persistence.Outbox;

    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; }
        public DateTime? ProcessedOn { get; set; }
        public string? Error { get; set; }
        public int RetryCount { get; set; }
    }

    // CoreBanking.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.OccurredOn).IsRequired();
            builder.Property(x => x.ProcessedOn).IsRequired(false);
            builder.Property(x => x.Error).HasMaxLength(1000);
            builder.Property(x => x.RetryCount).HasDefaultValue(0);
        }
    }

    // CoreBanking.Infrastructure/Data/BankingDbContext.cs (enhanced)
    public class BankingDbContext : DbContext
    {
        // ... existing DbSets
        
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
            // ... other configurations
        }

        public async Task SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default)
        {
            // Convert domain events to outbox messages
            var events = ChangeTracker.Entries<AggregateRoot<AccountId>>()
                .SelectMany(x => x.Entity.DomainEvents)
                .Select(domainEvent => new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = domainEvent.GetType().Name,
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    OccurredOn = domainEvent.OccurredOn
                })
                .ToList();

            // Clear domain events from aggregates
            ChangeTracker.Entries<AggregateRoot<AccountId>>()
                .ToList()
                .ForEach(entry => entry.Entity.ClearDomainEvents());

            // Save changes (including outbox messages) in single transaction
            await base.SaveChangesAsync(cancellationToken);

            // Add outbox messages after saving to ensure they're included in transaction
            if (events.Any())
            {
                await OutboxMessages.AddRangeAsync(events, cancellationToken);
                await base.SaveChangesAsync(cancellationToken);
            }
        }
    }
```

**Outbox Message Processor:**

```csharp
    // CoreBanking.Infrastructure/Services/OutboxMessageProcessor.cs
    namespace CoreBanking.Infrastructure.Services;

    public class OutboxMessageProcessor : IOutboxMessageProcessor
    {
        private readonly BankingDbContext _context;
        private readonly IEventBus _eventBus;
        private readonly ILogger<OutboxMessageProcessor> _logger;

        public OutboxMessageProcessor(BankingDbContext context, IEventBus eventBus, 
            ILogger<OutboxMessageProcessor> logger)
        {
            _context = context;
            _eventBus = eventBus;
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
                    if (domainEvent != null)
                    {
                        await _eventBus.PublishAsync(domainEvent, cancellationToken);
                    }

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

    // Background service for processing outbox
    public class OutboxBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

        public OutboxBackgroundService(IServiceProvider serviceProvider, ILogger<OutboxBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IOutboxMessageProcessor>();
                    await processor.ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing outbox messages");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
```

Session 2: gRPC vs REST Comparison
----------------------------------

### Deep Dive into gRPC Fundamentals

**What is gRPC?**

*   **gRPC:** Modern, high-performance RPC (Remote Procedure Call) framework    
*   **Protocol Buffers:** Interface definition language and message format    
*   **HTTP/2:** Underlying transport protocol for better performance
    

**gRPC vs REST Comparison:**

```csharp
    // Comparison Table for Banking Context

    | Aspect          | REST (HTTP/JSON)              | gRPC (HTTP/2/Protobuf)         |
    |-----------------|--------------------------------|---------------------------------|
    | Performance     | Good (text-based, HTTP/1.1)   | Excellent (binary, HTTP/2)     |
    | Payload Size    | Larger (JSON verbose)          | Smaller (binary serialization) |
    | Code Generation | Manual/Swagger                | Automatic from .proto files    |
    | Streaming       | Limited (SSE, WebSockets)     | Native (client, server, bidirectional) |
    | Browser Support | Excellent                     | Limited (requires gRPC-Web)    |
    | Use Cases       | Public APIs, web clients      | Internal services, mobile apps |

    // Banking-specific use cases for each:

    // REST: Public banking APIs, third-party integrations, web applications
    // gRPC: Internal microservices, real-time transaction processing, mobile banking apps
```

**When to Use gRPC in Banking:**

*   **High-Frequency Trading:** Real-time price updates and order execution    
*   **Transaction Processing:** High-volume internal transfers    
*   **Mobile Banking:** Reduced bandwidth consumption    
*   **Microservices Communication:** Efficient service-to-service calls    
*   **Real-time Notifications:** Live transaction alerts    

**When to Stick with REST:**

*   **Public APIs:** Third-party developer access    
*   **Web Applications:** Browser compatibility    
*   **Simple CRUD Operations:** Basic account management    
*   **Legacy Integration:** Systems expecting JSON/HTTP
    
Session 3: Protobuf Schema Design
---------------------------------

### Deep Dive into Protocol Buffers

**Protocol Buffers Basics:**
```bash
    // CoreBanking.API/gRPC/Protos/account.proto
    syntax = "proto3";

    package corebanking;

    import "google/protobuf/timestamp.proto";

    option csharp_namespace = "CoreBanking.API.gRPC";

    // Account service definition
    service AccountService {
    rpc GetAccount (GetAccountRequest) returns (AccountResponse);
    rpc CreateAccount (CreateAccountRequest) returns (CreateAccountResponse);
    rpc TransferMoney (TransferMoneyRequest) returns (TransferMoneyResponse);
    rpc GetTransactionHistory (TransactionHistoryRequest) returns (stream TransactionResponse);
    }

    // Message definitions
    message GetAccountRequest {
    string account_number = 1;
    }

    message AccountResponse {
    string account_id = 1;
    string account_number = 2;
    string account_type = 3;
    double balance = 4;
    string currency = 5;
    string customer_name = 6;
    google.protobuf.Timestamp date_opened = 7;
    bool is_active = 8;
    }

    message CreateAccountRequest {
    string customer_id = 1;
    string account_type = 2;
    double initial_deposit = 3;
    string currency = 4;
    }

    message CreateAccountResponse {
    string account_id = 1;
    string account_number = 2;
    string message = 3;
    }

    message TransferMoneyRequest {
    string source_account_number = 1;
    string destination_account_number = 2;
    double amount = 3;
    string currency = 4;
    string reference = 5;
    string description = 6;
    }

    message TransferMoneyResponse {
    bool success = 1;
    string message = 2;
    string transaction_id = 3;
    google.protobuf.Timestamp transfer_date = 4;
    }

    message TransactionHistoryRequest {
    string account_number = 1;
    google.protobuf.Timestamp start_date = 2;
    google.protobuf.Timestamp end_date = 3;
    int32 page_size = 4;
    }

    message TransactionResponse {
    string transaction_id = 1;
    string type = 2;
    double amount = 3;
    string currency = 4;
    string description = 5;
    string reference = 6;
    google.protobuf.Timestamp transaction_date = 7;
    string status = 8;
    }
```

**Advanced Protobuf Features for Banking:**

```bash
    // CoreBanking.Core/gRPC/Protos/common.proto
    syntax = "proto3";

    package corebanking;

    message Money {
    double amount = 1;
    string currency = 2;
    }

    message Address {
    string line1 = 1;
    string line2 = 2;
    string city = 3;
    string state = 4;
    string postal_code = 5;
    string country = 6;
    }

    message ErrorDetail {
    string code = 1;
    string message = 2;
    string target = 3;
    repeated ErrorDetail details = 4;
    }

    message ApiResponse {
    bool success = 1;
    string message = 2;
    repeated ErrorDetail errors = 3;
    google.protobuf.Timestamp timestamp = 4;
    }

    // Enhanced account service with streaming
    service EnhancedAccountService {
    // Server streaming for real-time transactions
    rpc StreamTransactions (StreamTransactionsRequest) returns (stream TransactionResponse);
    
    // Client streaming for batch operations
    rpc BatchTransfer (stream TransferMoneyRequest) returns (BatchTransferResponse);
    
    // Bidirectional streaming for real-time trading
    rpc LiveTrading (stream TradingOrder) returns (stream TradingExecution);
    }
```

**Project Configuration for gRPC:**

```xml
    <!-- CoreBanking.API/CoreBanking.API.csproj -->
    
    ...
    <ItemGroup>
        <Protobuf Include="gRPC\Protos\account.proto" GrpcServices="Server" />
        <Protobuf Include="gRPC\Protos\common.proto" GrpcServices="Server" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Grpc.AspNetCore" Version="2.71.0" />
        <PackageReference Include="Google.Protobuf" Version="3.30.2" />
        <PackageReference Include="Grpc.Tools" Version="2.71.0">
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
    </ItemGroup>
    ...
```                  

Session 4: Enhanced CQRS Pipeline
---------------------------------

### Deep Dive into Enhanced Pipeline Behaviors

**Domain Events Pipeline Behavior:**

> Reference Application Layer From Infrastructure Layer:
>  - `Infrastructure` -> References `Core` and **`Application`**

```csharp
    // CoreBanking.Application/Common/Behaviors/DomainEventsBehavior.cs
    namespace CoreBanking.Application.Common.Behaviors;

    public class DomainEventsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly ILogger<DomainEventsBehavior<TRequest, TResponse>> _logger;

        public DomainEventsBehavior(
            IDomainEventDispatcher dispatcher,
            ILogger<DomainEventsBehavior<TRequest, TResponse>> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing domain events for {RequestType}", typeof(TRequest).Name);

            var response = await next();

            // Collect and persist domain events
            await _dispatcher.DispatchDomainEventsAsync(cancellationToken);

            return response;
        }
    }
```

```csharp
    // CoreBanking.Application/Common/Interfaces/IDomainEventDispatcher.cs

    namespace CoreBanking.Application.Common.Interfaces;

    public interface IDomainEventDispatcher
    {
        Task DispatchDomainEventsAsync(CancellationToken cancellationToken);
    }
```

```csharp
    // CoreBanking.Infrastructure/Services/DomainEventDispatcher.cs

    namespace CoreBanking.Infrastructure.Services;

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

        public async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
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
```

**Event Handlers for Banking Operations:**

```csharp
    // CoreBanking.Application/Accounts/EventHandlers/AccountCreatedEventHandler.cs
    namespace CoreBanking.Application.Accounts.EventHandlers;

    public class AccountCreatedEventHandler : INotificationHandler<AccountCreatedEvent>
    {
        private readonly ILogger<AccountCreatedEventHandler> _logger;
        //private readonly IEmailService _emailService;

        //public AccountCreatedEventHandler(ILogger<AccountCreatedEventHandler> logger, IEmailService emailService)
        public AccountCreatedEventHandler(ILogger<AccountCreatedEventHandler> logger)
        {
            _logger = logger;
            //_emailService = emailService;
        }

        public async Task Handle(AccountCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing account created event for account {AccountNumber}",
                notification.AccountNumber);

            try
            {
                // Send welcome email
                //await _emailService.SendWelcomeEmailAsync(notification.CustomerId, notification.AccountNumber);

                // Could also: Update search index, trigger compliance checks, etc.
                _logger.LogInformation("Successfully processed account created event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process account created event for {AccountNumber}",
                    notification.AccountNumber);
                throw;
            }
        }
    }

    // CoreBanking.Application/Accounts/EventHandlers/MoneyTransferedEventHandler.cs
    public class MoneyTransferedEventHandler : INotificationHandler<MoneyTransferedEvent>
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

        public async Task Handle(MoneyTransferedEvent notification, CancellationToken cancellationToken)
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

    // CoreBanking.Application/Accounts/EventHandlers/InsufficientFundsEventHandler.cs
    public class InsufficientFundsEventHandler : INotificationHandler<InsufficientFundsEvent>
    {
        private readonly ILogger<InsufficientFundsEventHandler> _logger;
        //private readonly IFraudDetectionService _fraudDetectionService;

        //public InsufficientFundsEventHandler(ILogger<InsufficientFundsEventHandler> logger, IFraudDetectionService fraudDetectionService)
        public InsufficientFundsEventHandler(ILogger<InsufficientFundsEventHandler> logger)
        {
            _logger = logger;
            //_fraudDetectionService = fraudDetectionService;
        }

        public async Task Handle(InsufficientFundsEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogWarning("Processing insufficient funds event for account {AccountNumber}",
                notification.AccountNumber);

            // Trigger fraud detection analysis
            //await _fraudDetectionService.AnalyzeAccountActivityAsync(
            //  notification.AccountNumber,
                //notification.RequestedAmount,
                //notification.CurrentBalance);

            // Could also: Notify customer success team, update credit risk assessment, etc.
            _logger.LogInformation("Completed insufficient funds event processing");
        }
    }
```

**Enhanced Service Registration:**

```csharp
    // CoreBanking.API/Program.cs (enhanced)
    // Register domain event handlers
    builder.Services.AddTransient<INotificationHandler<AccountCreatedEvent>, AccountCreatedEventHandler>();
    builder.Services.AddTransient<INotificationHandler<MoneyTransferedEvent>, MoneyTransferedEventHandler>();
    builder.Services.AddTransient<INotificationHandler<InsufficientFundsEvent>, InsufficientFundsEventHandler>();

    // Register pipeline behaviors
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainEventsBehavior<,>));
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    // Register outbox services
    builder.Services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor>();
    builder.Services.AddHostedService<OutboxBackgroundService>();
```

Session 5: Hands-On Exercise & Assignment
-----------------------------------------

### Live Demo: Complete Implementation

**Step 1: Implement Domain Events Infrastructure**

```csharp
    // CoreBanking.Core/Interfaces/IDomainEvent.cs
    namespace CoreBanking.Core.Interfaces;

    public interface IDomainEvent
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
        string EventType { get; }
    }

    // CoreBanking.Core/Common/AggregateRoot.cs (enhanced)
    namespace CoreBanking.Core.Common;

    public abstract class AggregateRoot<TId> : IAggregateRoot where TId : notnull
    {
        [NotMapped]
        private readonly List<IDomainEvent> _domainEvents = new();

        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
```

**Step 2: Update Account Entity to Raise Events** : 

> Modify Transfer function and add Debit and Credit

```csharp
    // CoreBanking.Core/Entities/Account.cs
    ...
    public Result Transfer(Money amount, Account destination, string reference, string description)
    {
        // Validate inputs
        if (destination == null)
            throw new ArgumentNullException(nameof(destination), "Destination account cannot be null");

        if (amount.Amount <= 0)
            throw new InvalidOperationException("Transfer amount must be positive");

        if (this == destination)
            throw new InvalidOperationException("Cannot transfer to the same account");

        // Check source account conditions
        if (!IsActive)
            throw new InvalidOperationException("Source account is not active");

        if (!destination.IsActive)
            throw new InvalidOperationException("Destination account is not active");

        // Check sufficient funds
        if (Balance.Amount < amount.Amount)
        {
            // Raise insufficient funds event
            _domainEvents.Add(new InsufficientFundsEvent(
                AccountNumber, amount, Balance, "Transfer"));

            return Result.Failure("Insufficient funds for transfer");
        }

        // Special business rules for Savings accounts
        if (AccountType == AccountType.Savings &&
            _transactions.Count(t => t.Type == TransactionType.Withdrawal) >= 6)
        {
            return Result.Failure("Savings account withdrawal limit reached");
        }

        // Execute the transfer as an atomic operation
        var debitResult = Debit(amount, $"Transfer to {destination.AccountNumber}", reference);
        if (!debitResult.IsSuccess)
            return debitResult;

        var creditResult = destination.Credit(amount, $"Transfer from {AccountNumber}", reference);
        if (!creditResult.IsSuccess)
            return creditResult;

        // Raise money transferred event
        var transactionId = TransactionId.Create();
        _domainEvents.Add(new MoneyTransferedEvent(
            transactionId, AccountNumber, destination.AccountNumber, amount, reference));

        // Return success result
        return Result.Success();
    }

    public Result Debit(Money amount, string description, string reference)
    {
        if (IsDeleted)
            return Result.Failure("Cannot debit a deleted account");

        if (amount.Amount <= 0)
            return Result.Failure("Debit amount must be positive");

        if (Balance.Amount < amount.Amount)
            return Result.Failure("Insufficient funds");

        // Apply debit
        Balance -= amount;

        // Record transaction (matches your Transaction constructor)
        var transaction = new Transaction(
            AccountId,                            // AccountId
            TransactionType.Withdrawal,    // Transaction type
            amount,                        // Amount
            description,                   // Description
            this,                          // Account reference
            reference                      // Optional reference
        );

        _transactions.Add(transaction);

        // Raise domain event
        //AddDomainEvent(new AccountDebitedEvent(Id, amount, reference));

        return Result.Success();
    }

    public Result Credit(Money amount, string description, string reference)
    {
        if (IsDeleted)
            return Result.Failure("Cannot credit a deleted account");

        if (amount.Amount <= 0)
            return Result.Failure("Credit amount must be positive");

        // Apply credit
        Balance += amount;

        // Record transaction (matches your Transaction constructor)
        var transaction = new Transaction(
            AccountId,                          // AccountId
            TransactionType.Deposit,     // Transaction type
            amount,                      // Amount
            description,                 // Description
            this,                        // Account reference
            reference                    // Optional reference
        );

        _transactions.Add(transaction);

        // Raise domain event
        //AddDomainEvent(new AccountCreditedEvent(Id, amount, reference));

        return Result.Success();
    }
    ...
```

**Step 3: Add Result**
```csharp
    // CoreBanking.Core/Common/Result.cs
    
    namespace CoreBanking.Core.Common
    {
        public class Result
        {
            public bool IsSuccess { get; }
            public string Error { get; }

            public bool IsFailure => !IsSuccess;

            protected Result(bool isSuccess, string error)
            {
                if (isSuccess && !string.IsNullOrEmpty(error))
                    throw new InvalidOperationException("Success result cannot have an error message.");
                if (!isSuccess && string.IsNullOrEmpty(error))
                    throw new InvalidOperationException("Failure result must have an error message.");

                IsSuccess = isSuccess;
                Error = error;
            }

            public static Result Success() => new(true, string.Empty);
            public static Result Failure(string error) => new(false, error);

            public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
            public static Result<T> Failure<T>(string error) => new(default!, false, error);
        }

        public class Result<T> : Result
        {
            public T Value { get; }

            protected internal Result(T value, bool isSuccess, string error)
                : base(isSuccess, error)
            {
                Value = value;
            }
        }
    }
```

**Step 3: Modify Domain Event to include INotification and Add MediatR to CoreBanking.Core**

```csharp
    // CoreBanking.Core/Common/DomainEvent.cs    
    ...
    public abstract record DomainEvent : IDomainEvent, INotification
    ...
```

**Step 3: Add Interface for OutboxMessageProcessor**

```csharp
    // CoreBanking.Application/Common/Interfaces/IOutboxMessageProcessor.cs
    namespace CoreBanking.Application.Common.Interfaces
    {
        public interface IOutboxMessageProcessor
        {
            Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default);
        }
    }
```

**Step 3: Create gRPC Service Implementation**

```csharp
    // CoreBanking.API/gRPC/Services/AccountGrpcService.cs
    namespace CoreBanking.API.gRPC.Services;

    public class AccountGrpcService : AccountService.AccountServiceBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountGrpcService> _logger;

        public AccountGrpcService(IMediator mediator, IMapper mapper, ILogger<AccountGrpcService> logger)
        {
            _mediator = mediator;
            _mapper = mapper;
            _logger = logger;
        }

        public override async Task<AccountResponse> GetAccount(GetAccountRequest request,
            ServerCallContext context)
        {
            _logger.LogInformation("gRPC GetAccount called for {AccountNumber}", request.AccountNumber);

            var query = new GetAccountDetailsQuery
            {
                AccountNumber = AccountNumber.Create(request.AccountNumber)
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.NotFound, string.Join(", ", result.Errors)));

            return _mapper.Map<AccountResponse>(result.Data!);
        }

        public override async Task<CreateAccountResponse> CreateAccount(CreateAccountRequest request,
            ServerCallContext context)
        {
            if (!Guid.TryParse(request.CustomerId, out var customerGuid))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid customer ID format"));

            var command = new CreateAccountCommand
            {
                CustomerId = CustomerId.Create(customerGuid),
                AccountType = request.AccountType,
                InitialDeposit = (decimal)request.InitialDeposit,
                Currency = request.Currency
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", result.Errors)));

            return new CreateAccountResponse
            {
                AccountId = result.Data!.ToString(),
                AccountNumber = "TEMP", // Would come from the created account
                Message = "Account created successfully"
            };
        }

        public override async Task<TransferMoneyResponse> TransferMoney(TransferMoneyRequest request,
            ServerCallContext context)
        {
            var command = new TransferMoneyCommand
            {
                SourceAccountNumber = AccountNumber.Create(request.SourceAccountNumber),
                DestinationAccountNumber = AccountNumber.Create(request.DestinationAccountNumber),
                Amount = new Money((decimal)request.Amount, request.Currency),
                Reference = request.Reference,
                Description = request.Description
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                var status = result.Errors.Any(e => e.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
                    ? StatusCode.FailedPrecondition
                    : StatusCode.InvalidArgument;

                throw new RpcException(new Status(status, string.Join(", ", result.Errors)));
            }

            return new TransferMoneyResponse
            {
                Success = true,
                Message = "Transfer completed successfully",
                TransferDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }

        public override async Task GetTransactionHistory(TransactionHistoryRequest request,
            IServerStreamWriter<TransactionResponse> responseStream, ServerCallContext context)
        {
            var query = new GetTransactionHistoryQuery
            {
                AccountNumber = AccountNumber.Create(request.AccountNumber),
                StartDate = request.StartDate?.ToDateTime(),
                EndDate = request.EndDate?.ToDateTime(),
                PageSize = request.PageSize
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                throw new RpcException(new Status(StatusCode.NotFound, string.Join(", ", result.Errors)));

            foreach (var transaction in result.Data!.Transactions)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                await responseStream.WriteAsync(_mapper.Map<TransactionResponse>(transaction));
                await Task.Delay(100); // Simulate processing time
            }
        }
    }
```

**Step 4: Configure gRPC Services**

> Configure Program.cs to match
```csharp
    namespace CoreBanking.API
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddDbContext<BankingDbContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

                // Add Application and Infrastructure Services
                //builder.Services.AddApplicationServices();
                //builder.Services.AddInfrastructureServices(builder.Configuration);

                builder.WebHost.ConfigureKestrel(options =>
                {
                    // HTTP (for Swagger, REST, etc.)
                    options.ListenLocalhost(5037, o =>
                    {
                        o.Protocols = HttpProtocols.Http1;
                    });

                    // HTTPS (for gRPC, requires HTTP/2)
                    options.ListenLocalhost(7288, o =>
                    {
                        o.UseHttps(); // uses developer cert
                        o.Protocols = HttpProtocols.Http2;
                    });
                });

                // Register dependencies (DI)

                // Register Repositories
                builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
                builder.Services.AddScoped<IAccountRepository, AccountRepository>();
                builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
                builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

                // Register domain event handlers
                builder.Services.AddTransient<INotificationHandler<AccountCreatedEvent>, AccountCreatedEventHandler>();
                builder.Services.AddTransient<INotificationHandler<MoneyTransferedEvent>, MoneyTransferedEventHandler>();
                builder.Services.AddTransient<INotificationHandler<InsufficientFundsEvent>, InsufficientFundsEventHandler>();

                // Register pipeline behaviors
                builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainEventsBehavior<,>));
                builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

                // Add gRPC services to the container.
                builder.Services.AddGrpc(options =>
                {
                    options.EnableDetailedErrors = true;
                    //options.Interceptors.Add<ExceptionInterceptor>();
                });
                builder.Services.AddGrpcReflection();

                // Add MediatR with behaviours
                builder.Services.AddMediatR(cfg =>
                {
                    // Note: Registering one command is enough per Layer—MediatR scans the entire Application assembly (all Commands & Queries).
                    cfg.RegisterServicesFromAssembly(typeof(CreateAccountCommand).Assembly);

                    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                    cfg.AddOpenBehavior(typeof(DomainEventsBehavior<,>));

                    cfg.Lifetime = ServiceLifetime.Scoped;
                });

                // Add Validators and AutoMapper
                builder.Services.AddValidatorsFromAssembly(typeof(CreateAccountCommandValidator).Assembly);
                builder.Services.AddAutoMapper(cfg => { }, typeof(AccountProfile).Assembly);
                builder.Services.AddAutoMapper(cfg => { }, typeof(AccountGrpcProfile).Assembly);

                // Register outbox and Background services
                builder.Services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor>();
                builder.Services.AddHostedService<OutboxBackgroundService>();

                // Add controllers and swagger
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();

                // Enriched swaggerGen with XML comments and authentication
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "CoreBanking API",
                        Version = "v1",
                        Description = "A modern banking API built with Clean Architecture, DDD and CQRS",
                        Contact = new OpenApiContact
                        {
                            Name = "CoreBanking Team",
                            Email = "support@corebanking.com"
                        }
                    });

                    // Include XML comments
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);

                    // Add authentication support in Swagger
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme.",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });
                });


                var app = builder.Build();

                
                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger(options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0);

                    // Enriched Swagger UI
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoreBanking API v1");
                        c.RoutePrefix = "swagger"; // Access at /swagger
                        c.DocumentTitle = "CoreBanking API Documentation";
                        c.EnableDeepLinking();
                        c.DisplayOperationId();
                    });
                }

                app.UseHttpsRedirection();           

                app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

                app.UseAuthorization();

                app.MapControllers();

                //Use grpc Endpoints
                app.MapGrpcService<AccountGrpcService>();
                app.MapGet("/", () => "CoreBanking API is running. Use /swagger for REST or a gRPC client for gRPC calls.");
                if (app.Environment.IsDevelopment())
                {
                    app.MapGrpcReflectionService();
                }

                app.Run();
            }
        }
    }
```

```csharp
    // CoreBanking.API/gRPC/Mappings/AccountGrpcProfile.cs

    using AutoMapper;
    using CoreBanking.API.gRPC; // gRPC-generated types
    using CoreBanking.Application.Accounts.Queries.GetAccountDetails;
    using CoreBanking.Application.Accounts.Queries.GetTransactionHistory;
    using Google.Protobuf.WellKnownTypes;

    namespace CoreBanking.API.gRPC.Mappings
    {
        public class AccountGrpcProfile : Profile
        {
            public AccountGrpcProfile()
            {
                CreateMap<AccountDetailsDto, AccountResponse>()
                    .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => src.AccountNumber))
                    .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => src.AccountType))
                    .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance.Amount))
                    .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Balance.Currency))
                    .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.CustomerName))
                    .ForMember(dest => dest.DateOpened, opt => opt.MapFrom(src =>
                        Timestamp.FromDateTime(DateTime.SpecifyKind(src.DateOpened, DateTimeKind.Utc))))
                    .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

                CreateMap<TransactionDto, TransactionResponse>()
                    .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.TransactionId))
                    .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                    .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                    .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
                    .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                    .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => src.Reference))
                    .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src =>
                        Timestamp.FromDateTime(DateTime.SpecifyKind(src.Timestamp, DateTimeKind.Utc))))
                    .ForMember(dest => dest.RunningBalance, opt => opt.MapFrom(src => src.RunningBalance));
            }
        }
    }
```
    
**The Deliverable:**

*   Working domain events for key banking operations    
*   Complete outbox pattern implementation with background processor    
*   Event handlers for notifications and reporting    
*   Basic gRPC service with account operations    
*   Protobuf schema for banking operations
    
**Success Criteria**

*   Domain events are properly raised from aggregates    
*   Outbox pattern ensures reliable event delivery    
*   Event handlers process events asynchronously    
*   gRPC service handles basic account operations    
*   Protobuf schemas follow best practices    
*   System maintains consistency across components    

**In-Depth Study Guide for Students:**
*   Domain-Driven Design: Domain Events Pattern    
*   Reliable Messaging Patterns    
*   gRPC Protocol Buffers Specification    
*   Event-Driven Architecture Principles    
*   Distributed Systems Consistency Models
    

### Q&A and Preview of Day 7

"Tomorrow we'll dive deep into gRPC service implementation with advanced streaming patterns and introduce SignalR for real-time web communication - building the foundation for live transaction feeds and real-time banking features."

_This lesson plan enhances our banking architecture with event-driven capabilities and introduces high-performance gRPC communication, setting the stage for scalable, real-time banking systems._
