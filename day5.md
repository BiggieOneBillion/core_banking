# Day 5 Lesson Plan: RESTful APIs + FluentValidation/AutoMapper
> **Title:** Building Production-Ready APIs - Validation, Mapping & Documentation

> **Theme:** Implementing Robust REST APIs with Validation, Auto-Mapping, and Professional Documentation

## Session 1: REST API Design & FluentValidation

### Icebreaker & API Thinking (15 mins)
**Ask:** "When you use a banking app or website, what makes you trust that your transactions are secure and reliable? What API design elements contribute to that trust?"

**Expected Answers:** Clear error messages, consistent responses, proper validation, good documentation, secure endpoints.

**The Connection:** "Today we're building production-ready REST APIs with proper validation, automated mapping, and professional documentation - the foundation of trustworthy banking applications."

### Deep Dive into REST API Design Principles
**RESTful Principles for Banking APIs:**
- **Resource-Oriented Design:** Treat everything as resources (accounts, transactions, customers)
- **HTTP Semantics:** Proper use of verbs (GET, POST, PUT, DELETE) and status codes
- **Statelessness:** Each request contains all necessary information
- **HATEOAS:** Hypermedia as the Engine of Application State (for discoverability)

**Banking API Best Practices:**
```csharp
    // Good RESTful design
    GET    /api/accounts/{accountNumber}          // Get account details
    POST   /api/accounts                         // Create new account
    POST   /api/accounts/{accountNumber}/transfer // Transfer money
    GET    /api/accounts/{accountNumber}/transactions?page=1&pageSize=50

    // vs Poor design
    GET    /api/getAccount.php?account=123
    POST   /api/doTransfer.php
```

**HTTP Status Codes for Banking:**
- `200 OK` - Successful GET requests
- `201 Created` - Resource created successfully
- `400 Bad Request` - Validation errors
- `404 Not Found` - Resource doesn't exist
- `409 Conflict` - Business rule violation
- `422 Unprocessable Entity` - Semantic errors 
---

### Deep Dive into FluentValidation Integration
**Why FluentValidation over Data Annotations?**
- **Separation of Concerns:** Validation logic separate from DTOs
- **Complex Rules:** Support for conditional validation and business rules
- **Testability:** Easy to unit test validation rules
- **Reusability:** Validators can be composed and reused

**FluentValidation Architecture:**
```csharp
   // Validation flow
    Controller → MediatR → ValidationBehavior → FluentValidation Validator → Command/Query Handler
```

**Banking-Specific Validation Rules:**
```csharp
    //CoreBanking.Application/Accounts/Commands/CreateAccount/CreateAccountCommandValidator.cs
    public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
    {
        public CreateAccountCommandValidator()
        {   
            RuleFor(x => x.CustomerId.Value)
                .NotEmpty().WithMessage("Customer ID is required")
                .NotEqual(Guid.Empty).WithMessage("Customer ID cannot be empty");

            RuleFor(x => x.AccountType)
                .NotEmpty().WithMessage("Account type is required")
                .Must(BeValidAccountType).WithMessage("Invalid account type. Must be Savings or Current");

            RuleFor(x => x.InitialDeposit)
                .GreaterThanOrEqualTo(0).WithMessage("Initial deposit cannot be negative")
                .LessThan(1000000).WithMessage("Initial deposit cannot exceed ₦1,000,000");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required")
                .Length(3).WithMessage("Currency must be 3 characters")
                .Must(BeSupportedCurrency).WithMessage("Unsupported currency. Supported: NGN, USD, GBP");
        }

        private bool BeValidAccountType(string accountType)
                => Enum.TryParse<AccountType>(accountType, out _);

        private bool BeSupportedCurrency(string currency)
            => new[] { "NGN", "USD", "GBP" }.Contains(currency);
    }
```
---

## Session 2: AutoMapper Configuration & Controller Implementation

### Deep Dive into AutoMapper Configuration
**Why AutoMapper for Banking Applications?**
- **Separation of Concerns:** Keep mapping logic out of domain entities and controllers
- **Maintainability:** Centralized mapping configuration
- **Complex Transformations:** Handle complex object graphs and conditional mapping
- **Testability:** Mapping logic can be unit tested

**AutoMapper Profiles for Banking:**
```bash
    # In CoreBanking.Application
    dotnet add package AutoMapper
```
```csharp
    // CoreBanking.API/Program.cs
    ...
    // Add AutoMapper
    builder.Services.AddAutoMapper(cfg => { }, typeof(AccountProfile).Assembly);
    ...
```

```csharp
    // CoreBanking.Application/Common/Mappings/AccountProfile.cs
    namespace CoreBanking.Application.Common.Mappings;

    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            // Domain Entity to DTO mappings
            CreateMap<Account, AccountDetailsDto>()
                .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => src.AccountNumber.Value))
                .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => src.AccountType.ToString()))
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance.Amount))
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => $"{src.Customer.FirstName} {src.Customer.LastName}"));

            // Command to Domain Entity mappings (for complex scenarios)
            CreateMap<CreateAccountCommand, Account>()
                .ConstructUsing(src => Account.Create(
                    src.CustomerId,
                    AccountNumber.Create("TEMPORARY"), // Will be replaced in handler
                    Enum.Parse<AccountType>(src.AccountType),
                    new Money(src.InitialDeposit, src.Currency)
                ))
                .ForAllMembers(opt => opt.Ignore()); // Ignore all direct mappings since we use constructor

            // Transaction mappings
            CreateMap<Transaction, TransactionDto>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.TransactionId.Value.ToString()))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.Amount))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Amount.Currency));
        }
    }
```

**Advanced Mapping Scenarios:**
```csharp
    // CoreBanking.Application/Common/Mappings/Resolvers/FullNameResolver.cs

    // Custom value resolvers for complex mappings
    public class FullNameResolver : IValueResolver<Customer, object, string>
    {
        public string Resolve(Customer source, object destination, string destMember, ResolutionContext context)
            => $"{source.FirstName} {source.LastName}";
    }

    // Add the following to your AccountProfile
    ...
    // Conditional mapping for different account types
    CreateMap<Account, AccountSummaryDto>()
        .ForMember(dest => dest.DisplayName,
            opt => opt.MapFrom((src, dest) =>
                src.AccountType == AccountType.Savings
                    ? $"{src.AccountNumber.Value} - Savings"
                    : $"{src.AccountNumber.Value} - Current"));
    ...
```

> Add AccountSummaryDto
```csharp
    // CoreBanking.Application/Accounts/Queries/GetAccountSummary/AccountSummaryDto.cs
    public record AccountSummaryDto
    {
        public AccountNumber AccountNumber { get; init; } = AccountNumber.Create(string.Empty);
        public string AccountType { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public decimal Balance { get; init; }
        public string Currency { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime DateOpened { get; init; }
    }
```

---
### Deep Dive into Controller Implementation
**API Controller Design Patterns:**
```csharp
    // CoreBanking.API/Controllers/AccountsController.cs
    namespace CoreBanking.API.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(IMediator mediator, IMapper mapper, ILogger<AccountsController> logger)
        {
            _mediator = mediator;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("{accountNumber}")]
        [ProducesResponseType(typeof(ApiResponse<AccountDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<AccountDetailsDto>>> GetAccountDetails(string accountNumber)
        {
            _logger.LogInformation("Retrieving account details for {AccountNumber}", accountNumber);

            var query = new GetAccountDetailsQuery { AccountNumber = AccountNumber.Create(accountNumber) };
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return NotFound(ApiResponse.CreateFailure(result.Errors));

            return Ok(ApiResponse<AccountDetailsDto>.CreateSuccess(result.Data!));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateAccount([FromBody] CreateAccountRequest request)
        {
            _logger.LogInformation("Creating new account for customer {CustomerId}", CustomerId.Create(request.CustomerId) );

            var command = _mapper.Map<CreateAccountCommand>(request);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse.CreateFailure(result.Errors));

            return CreatedAtAction(
                nameof(GetAccountDetails),
                new { accountNumber = "TEMPORARY" }, // Would need account number here
                ApiResponse<Guid>.CreateSuccess(result.Data!));
        }

        [HttpPost("{accountNumber}/transfer")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse>> TransferMoney(
            string accountNumber,
            [FromBody] TransferMoneyRequest request)
        {
            _logger.LogInformation("Processing transfer from {AccountNumber}", accountNumber);

            var command = new TransferMoneyCommand
            {
                SourceAccountNumber = AccountNumber.Create(accountNumber),
                DestinationAccountNumber = AccountNumber.Create(request.DestinationAccountNumber),
                Amount = new Money(request.Amount, request.Currency),
                Reference = request.Reference,
                Description = request.Description
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return result.Errors.Any(e => e.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
                    ? Conflict(ApiResponse.CreateFailure(result.Errors))
                    : BadRequest(ApiResponse.CreateFailure(result.Errors));
            }

            return Ok(ApiResponse.CreateSuccess("Transfer completed successfully"));
        }

        [HttpGet("{accountNumber}/transactions")]
        [ProducesResponseType(typeof(ApiResponse<TransactionHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TransactionHistoryDto>>> GetTransactionHistory(
            string accountNumber,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = new GetTransactionHistoryQuery
            {
                AccountNumber = AccountNumber.Create(accountNumber),
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return NotFound(ApiResponse.CreateFailure(result.Errors));

            return Ok(ApiResponse<TransactionHistoryDto>.CreateSuccess(result.Data!));
        }
    }
```

**Consistent API Response Format:**
```csharp
    // CoreBanking.API/Models/ApiResponse.cs
    namespace CoreBanking.API.Models;

    public record ApiResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string[] Errors { get; init; } = Array.Empty<string>();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public static ApiResponse CreateSuccess(string message = "Operation completed successfully")
            => new() { Success = true, Message = message };

        public static ApiResponse CreateFailure(params string[] errors)
            => new() { Success = false, Errors = errors };
    }

    public record ApiResponse<T> : ApiResponse
    {
        public T? Data { get; init; }

        public static ApiResponse<T> CreateSuccess(T data, string message = "Operation completed successfully")
            => new() { Success = true, Message = message, Data = data };

        public static new ApiResponse<T> CreateFailure(params string[] errors)
            => new() { Success = false, Errors = errors };
    }
```
## Session 3: Hands-On Implementation & API Documentation

### Live Demo: Complete API Implementation

**Step 1: Add Required Packages**
```bash
    # In CoreBanking.API project
    dotnet add package Swashbuckle.AspNetCore
    dotnet add package Swashbuckle.AspNetCore.Annotations

    # In CoreBanking.Application project
    dotnet add package FluentValidation.DependencyInjectionExtensions
```

**Step 2: Configure Services in Program.cs**
```csharp
    // CoreBanking.API/Program.cs

    // Add FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(CreateAccountCommandValidator).Assembly);

    // Add MediatR with behaviors
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(CreateAccountCommand).Assembly);
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));

        cfg.Lifetime = ServiceLifetime.Scoped;
    });

    // Add Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "CoreBanking API", 
            Version = "v1",
            Description = "A modern banking API built with Clean Architecture and CQRS",
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
```

**Step 3: Implement Request DTOs and Validators**
```csharp
    // CoreBanking.API/Models/Requests/CreateAccountRequest.cs
    namespace CoreBanking.API.Models.Requests;

    public record CreateAccountRequest
    {
        public Guid CustomerId { get; init; }
        public string AccountType { get; init; } = string.Empty;
        public decimal InitialDeposit { get; init; }
        public string Currency { get; init; } = "NGN";
    }

    // CoreBanking.API/Models/Requests/TransferMoneyRequest.cs
    public record TransferMoneyRequest
    {
        public string DestinationAccountNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "NGN";
        public string Reference { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    // CoreBanking.API/Models/Requests/CreateCustomerRequest.cs
    public record CreateCustomerRequest
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public DateTime DateOfBirth { get; init; }
    }
    
    // CoreBanking.Application/Accounts/Commands/TransferMoney/TransferMoneyCommandValidator.cs
    public class TransferMoneyCommandValidator : AbstractValidator<TransferMoneyCommand>
    {
        public TransferMoneyCommandValidator()
        {
            RuleFor(x => x.SourceAccountNumber.Value)
                .NotEmpty().WithMessage("Source account number is required")
                .Length(10).WithMessage("Source account number must be 10 digits")
                .Matches(@"^\d+$").WithMessage("Source account number must contain only digits");

            RuleFor(x => x.DestinationAccountNumber.Value)
                .NotEmpty().WithMessage("Destination account number is required")
                .Length(10).WithMessage("Destination account number must be 10 digits")
                .Matches(@"^\d+$").WithMessage("Destination account number must contain only digits")
                .NotEqual(cmd => cmd.SourceAccountNumber).WithMessage("Cannot transfer to the same account");

            RuleFor(x => x.Amount.Amount)
                .GreaterThan(0).WithMessage("Transfer amount must be greater than 0")
                .LessThanOrEqualTo(500000).WithMessage("Single transfer cannot exceed ₦500,000");

            RuleFor(x => x.Amount.Currency)
                .NotEmpty().WithMessage("Currency is required")
                .Length(3).WithMessage("Currency must be 3 characters");

            RuleFor(x => x.Reference)
                .MaximumLength(50).WithMessage("Reference cannot exceed 50 characters");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("Description cannot exceed 200 characters");
        }
    }
```

**Step 4: Implement Customer Controller**
```csharp
    // CoreBanking.API/Controllers/CustomersController.cs
    namespace CoreBanking.API.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(IMediator mediator, IMapper mapper, ILogger<CustomersController> logger)
        {
            _mediator = mediator;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CustomerDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<CustomerDto>>>> GetCustomers()
        {
            var query = new GetCustomersQuery();
            var result = await _mediator.Send(query);

            return Ok(ApiResponse<List<CustomerDto>>.CreateSuccess(result.Data!));
        }

        [HttpGet("{customerId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CustomerDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<CustomerDetailsDto>>> GetCustomer(Guid customerId)
        {
            var query = new GetCustomerDetailsQuery { CustomerId = customerId };
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return NotFound(ApiResponse.CreateFailure(result.Errors));

            return Ok(ApiResponse<CustomerDetailsDto>.CreateSuccess(result.Data!));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateCustomer([FromBody] CreateCustomerRequest request)
        {
            var command = _mapper.Map<CreateCustomerCommand>(request);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse.CreateFailure(result.Errors));

            return CreatedAtAction(
                nameof(GetCustomer),
                new { customerId = result.Data },
                ApiResponse<Guid>.CreateSuccess(result.Data!));
        }
    }
```

**Step 5: Configure Swagger Middleware**
```csharp
    // CoreBanking.API/Program.cs (after building app)

    app.UseSwagger(options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0);
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoreBanking API v1");
        c.RoutePrefix = "swagger"; // Access at /swagger
        c.DocumentTitle = "CoreBanking API Documentation";
        c.EnableDeepLinking();
        c.DisplayOperationId();
    });
```
> Enable XML documentation in CoreBanking.API.csproj

```bash
    # CoreBanking.API/CoreBanking.API.csproj

    # Add this after the first '</PropertyGroup>' 
    ...    
    <PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
    </PropertyGroup>    
```

**Step 6: Add Enhanced API Documentation**
```csharp
    // CoreBanking.API/Controllers/AccountsController.cs (enhanced with XML comments)
    /// <summary>
    /// Banking accounts management API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AccountsController : ControllerBase
    {
        // ... constructor

        /// <summary>
        /// Get account details by account number
        /// </summary>
        /// <param name="accountNumber">The 10-digit account number</param>
        /// <returns>Account details including balance and customer information</returns>
        /// <response code="200">Returns the account details</response>
        /// <response code="404">Account not found</response>
        /// <response code="400">Invalid account number format</response>
        [HttpGet("{accountNumber}")]
        [ProducesResponseType(typeof(ApiResponse<AccountDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<AccountDetailsDto>>> GetAccountDetails(string accountNumber)
        {
            // ... implementation
        }

        /// <summary>
        /// Create a new bank account
        /// </summary>
        /// <param name="request">Account creation details</param>
        /// <returns>The newly created account ID</returns>
        /// <response code="201">Account created successfully</response>
        /// <response code="400">Invalid request data or business rule violation</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateAccount([FromBody] CreateAccountRequest request)
        {
            // ... implementation
        }

        /// <summary>
        /// Transfer money between accounts
        /// </summary>
        /// <param name="accountNumber">Source account number</param>
        /// <param name="request">Transfer details</param>
        /// <returns>Transfer operation result</returns>
        /// <response code="200">Transfer completed successfully</response>
        /// <response code="400">Invalid transfer request</response>
        /// <response code="409">Business rule violation (e.g., insufficient funds)</response>
        [HttpPost("{accountNumber}/transfer")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse>> TransferMoney(
            string accountNumber, 
            [FromBody] TransferMoneyRequest request)
        {
            // ... implementation
        }
    }
```

**Step 7: Global Exception Handling**
```csharp
    // CoreBanking.API/Middleware/GlobalExceptionHandlerMiddleware.cs
    namespace CoreBanking.API.Middleware;

    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status409Conflict,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            var response = new ApiResponse
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new[] { exception.Message }
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    // Register in Program.cs
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
```

**Step 8: Register Behaviors** 
```csharp
    // CoreBanking.API/Program.cs
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

**Step 9: Fixing Broken Snippets Due to Code Evolution**
- 01: Using Value Objects Correctly in TransferMoneyCommand
```csharp
    // CoreBanking.Application/Accounts/Commands/TransferMoney/TransferMoneyCommand.cs
    ...
    //Replace these 4 lines
    //OLD LINES
    public string SourceAccountNumber { get; init; } = string.Empty;
    public string DestinationAccountNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "NGN";

    //NEW LINES
    public AccountNumber SourceAccountNumber { get; init; } = AccountNumber.Create(string.Empty);
    public AccountNumber DestinationAccountNumber { get; init; } = AccountNumber.Create(string.Empty);
    public Money Amount { get; init; } = new Money(0);    
```

```csharp
    // CoreBanking.Application/Accounts/Commands/TransferMoney/TransferMoneyCommand.cs
    ...
    //Replace these lines
    //OLD LINES
    sourceAccount.Transfer(
        amount: new Money(request.Amount, request.Currency),
        destination: destAccount,
        reference: request.Reference,
        description: request.Description
    );

    //NEW LINES
    sourceAccount.Transfer(
        amount: request.Amount,
        destination: destAccount,
        reference: request.Reference,
        description: request.Description
    );    
```

- 02 - Using Value Objects Correctly in GetAccountDetails
```csharp
    // CoreBanking.Application/Accounts/Queries/GetAccountDetails/GetAccountDetailsQuery.cs
    ...
    //Replace these lines
    //OLD LINE (DON'T CHANGE THE ONE IN THE DTO)
    public string AccountNumber { get; init; } = string.Empty;

    //NEW LINE
    public AccountNumber AccountNumber { get; init; } = AccountNumber.Create(string.Empty);
```

```csharp
    // CoreBanking.Application/Accounts/Queries/GetAccountDetails/GetAccountDetailsQuery.cs
    ...
    //Replace these lines
    //OLD LINES
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Currency { get; init; } = string.Empty;

    //NEW LINES
    public AccountNumber AccountNumber { get; init; } = AccountNumber.Create(string.Empty);
    public string AccountType { get; init; } = string.Empty;
    public Money Balance { get; init; } = new Money(0);
```

```csharp
    // CoreBanking.Application/Accounts/Queries/GetAccountDetails/GetAccountDetailsQuery.cs
    ...
    //Replace these lines
    //OLD LINES
    public async Task<Result<AccountDetailsDto>> Handle(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByAccountNumberAsync(new AccountNumber(request.AccountNumber));

        if (account == null)
            return Result<AccountDetailsDto>.Failure("Account not found");

        var dto = new AccountDetailsDto
        {
            AccountNumber = account.AccountNumber.Value,
            AccountType = account.AccountType.ToString(),
            Balance = account.Balance.Amount,
            Currency = account.Balance.Currency,
            DateOpened = account.DateOpened,
            IsActive = account.IsActive,
            CustomerName = $"{account.Customer.FirstName} {account.Customer.LastName}"
        };
        return Result<AccountDetailsDto>.Success(dto);
    }

    //NEW LINES
    public async Task<Result<AccountDetailsDto>> Handle(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByAccountNumberAsync(AccountNumber.Create(request.AccountNumber));

        if (account == null)
            return Result<AccountDetailsDto>.Failure("Account not found");

        var dto = new AccountDetailsDto
        {
            AccountNumber = account.AccountNumber,
            AccountType = account.AccountType.ToString(),
            Balance = new Money(account.Balance.Amount, account.Balance.Currency),
            DateOpened = account.DateOpened,
            IsActive = account.IsActive,
            CustomerName = $"{account.Customer.FirstName} {account.Customer.LastName}"
        };

        return Result<AccountDetailsDto>.Success(dto);
    }
```

- 03 - Using Value Objects Correctly in GetTransactionHistory
```csharp
    // CoreBanking.Application/Accounts/Queries/GetTransactionHistory/GetTransactionHistoryQuery.cs
    ...
    //Replace these lines
    //OLD LINE
    public string AccountNumber { get; init; } = string.Empty;

    //NEW LINE
    public AccountNumber AccountNumber { get; init; } = AccountNumber.Create(string.Empty);
```

```csharp
    // CoreBanking.Application/Accounts/Queries/GetTransactionHistory/GetTransactionHistoryQuery.cs
    ...
    //Replace these lines
    //OLD LINES
    var transactionDtos = pagedTransactions.Select(t => new TransactionDto
    {
        TransactionId = t.TransactionId.Value.ToString(),

    //NEW LINES
    var transactionDtos = pagedTransactions.Select(t => new TransactionDto
    {
        TransactionId = TransactionId.Create(t.TransactionId.Value),
```

- 04 - Add TransactionDto
```csharp
    // CoreBanking.Application/Accounts/Queries/GetTransactionHistory/TransactionDto.cs
    namespace CoreBanking.Application.Accounts.Queries.GetTransactionHistory
    {
        public record TransactionDto
        {
            public string TransactionId { get; init; } = string.empty;
            public string Type { get; init; } = string.Empty;
            public decimal Amount { get; init; }
            public string Currency { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string Reference { get; init; } = string.Empty;
            public DateTime Timestamp { get; init; }
            public decimal RunningBalance { get; init; }
        }
    }
```

- 05 - Materialize AccountNumber ValueObject
```csharp
    // CoreBanking.Core/ValueObjects/AccountNumber.cs
    ...
    //Replace these lines
    //OLD LINE
    public static AccountNumber Create(string value) => new(value);

    //NEW LINES
    // EF Core needs this
    private AccountNumber() : this(string.Empty) { }

    public static AccountNumber Create(string value) => new(value);
```

- 06 - Eager Load Customer in AccountRepository functions
```csharp
    // CoreBanking.Infrastructure/Repositories/AccountRepository.cs
    ...
    //Replace these lines
    //OLD LINES
    return await _context.Accounts
    .Include(a => a.Transactions)

    //NEW LINES
    return await _context.Accounts
    .Include(a => a.Customer) 
    .Include(a => a.Transactions)
 ```  

- 07 - Reconfigure How we interact with AccountNumber ValueObject in DbContext
```csharp
    // CoreBanking.Infrastructure/Data/BankingDbContext.cs
    ...
    //Replace these lines
    //OLD LINES
    // Configure AccountNumber as owned type (Value Object)
    entity.OwnsOne(a => a.AccountNumber, an =>
    {
        an.Property(a => a.Value)
        .HasColumnName("AccountNumber")
        .IsRequired()
        .HasMaxLength(10);
    });

    //NEW LINES
    // Configure AccountNumber as owned type (Value Object)
    entity.Property(a => a.AccountNumber)
        .HasConversion(
            accountNumber => accountNumber.Value,
            value => AccountNumber.Create(value))
        .HasColumnName("AccountNumber")
        .HasMaxLength(10)
        .IsRequired();
```

```csharp
    // CoreBanking.Infrastructure/Data/BankingDbContext.cs
    ...
    //Replace these lines
    //OLD LINES
    modelBuilder.Entity<Account>().HasData(new {
        AccountId = AccountId.Create(Guid.Parse("c3d4e5f6-3456-7890-cde1-345678901cde")),
        AccountType = AccountType.Checking, // EF handles enum conversion                    

    //NEW LINES
    modelBuilder.Entity<Account>().HasData(new {
        AccountId = AccountId.Create(Guid.Parse("c3d4e5f6-3456-7890-cde1-345678901cde")),
        AccountNumber = AccountNumber.Create("1000000001"),
        AccountType = AccountType.Checking, // EF handles enum conversion

    //DELETE THIS
    modelBuilder.Entity<Account>().OwnsOne(a => a.AccountNumber).HasData(
        new
        {
            AccountId = AccountId.Create(Guid.Parse("c3d4e5f6-3456-7890-cde1-345678901cde")),
            Value = "1000000001"
        }
    );
```

- 08 - Reset Migrations To ensure our changes reflect
```bash   
    // A - DELETE YOUR DB MANUALLY OR USE DOTNET TO REVERT ALL CHANGES TO YOUR DB

    // B - Remove last migration (Remove all if you have more than 1)
    dotnet ef migrations remove -p CoreBanking.Infrastructure -s CoreBanking.API

    // C - Create migration using Package manager console
    dotnet ef migrations add InitialCreate -p CoreBanking.Infrastructure -s CoreBanking.API

    // D - Update database using Package manager console
    dotnet ef database update InitialCreate -p CoreBanking.Infrastructure -s CoreBanking.API
```


## Session 4: Hands-On Exercise & Assignment
XXXXXXXXX

### Review Assignment & Success Criteria
XXXXXXXXX

**The Deliverable:**
- Complete REST API with proper HTTP verbs and status codes
- Working FluentValidation rules for all commands
- AutoMapper profiles for entity-DTO transformations
- Comprehensive Swagger/OpenAPI documentation
- Consistent API response format
- Global exception handling

**Success Criteria**
- All endpoints follow RESTful conventions
- Validation rules prevent invalid operations
- AutoMapper correctly transforms between layers
- Swagger documentation is complete and accurate
- Error handling provides meaningful responses
- API is testable and well-documented

**In-Depth Study Guide for Students:**
- REST API Design Best Practices
- FluentValidation Advanced Scenarios
- AutoMapper Custom Resolvers and Projections
- OpenAPI/Swagger Specification
- API Versioning Strategies

### Q&A and Preview of Day 5
"Tomorrow we'll enhance our architecture with advanced patterns including domain events for better system integration and the outbox pattern for reliability."

---

*This lesson plan completes our banking application's API layer with production-ready REST endpoints, comprehensive validation, and professional documentation while maintaining clean architecture separation.*

