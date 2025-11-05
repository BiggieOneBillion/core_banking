// using CoreBanking.Core.Interface;
// using FluentValidation;

// namespace CoreBanking.Application.Accounts.Commands.TransferMoney;

// public class TransferMoneyCommandValidator : AbstractValidator<TransferMoneyCommand>
// {
//     private readonly IAccountRepository _accountRepository;

//     public TransferMoneyCommandValidator(IAccountRepository accountRepository)
//     {
//         _accountRepository = accountRepository;

//         // Validate source account number
//         RuleFor(x => x.SourceAccountNumber)
//             .NotNull().WithMessage("Source account number is required")
//             .Must(x => !string.IsNullOrWhiteSpace(x.Value))
//             .WithMessage("Source account number cannot be empty")
//             .MustAsync(AccountExists)
//             .WithMessage("Source account does not exist");

//         // Validate destination account number
//         RuleFor(x => x.DestinationAccountNumber)
//             .NotNull().WithMessage("Destination account number is required")
//             .Must(x => !string.IsNullOrWhiteSpace(x.Value))
//             .WithMessage("Destination account number cannot be empty")
//             .MustAsync(AccountExists)
//             .WithMessage("Destination account does not exist");

//         // Validate source and destination are different
//         RuleFor(x => x)
//             .Must(x => x.SourceAccountNumber.Value != x.DestinationAccountNumber.Value)
//             .WithMessage("Source and destination accounts must be different");

//         // Validate amount
//         RuleFor(x => x.Amount)
//             .NotNull().WithMessage("Transfer amount is required")
//             .Must(x => x.Amount > 0)
//             .WithMessage("Transfer amount must be greater than zero")
//             .Must(x => x.Amount <= 1000000)
//             .WithMessage("Transfer amount cannot exceed ₦1,000,000");

//         // Validate currency
//         RuleFor(x => x.Amount.Currency)
//             .NotEmpty().WithMessage("Currency is required")
//             .Length(3).WithMessage("Currency must be 3 characters (e.g., NGN, USD)")
//             .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be 3 uppercase letters");

//         // Validate reference (optional but if provided, should be valid)
//         RuleFor(x => x.Reference)
//             .MaximumLength(50).WithMessage("Reference cannot exceed 50 characters");

//         // Validate description (optional but if provided, should be valid)
//         RuleFor(x => x.Description)
//             .MaximumLength(200).WithMessage("Description cannot exceed 200 characters");
//     }

//     private async Task<bool> AccountExists(CoreBanking.Core.ValueObjects.AccountNumber accountNumber, CancellationToken cancellationToken)
//     {
//         if (string.IsNullOrWhiteSpace(accountNumber.Value))
//             return false;

//         return await _accountRepository.AccountNumberExistsAsync(accountNumber);
//     }
// }


// CoreBanking.Application/Accounts/Commands/TransferMoney/TransferMoneyCommandValidator.cs
using CoreBanking.Application.Accounts.Commands.TransferMoney;
using FluentValidation;

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
