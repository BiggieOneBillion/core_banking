using FluentValidation;

namespace CoreBanking.Application.Accounts.Queries.GetAccountDetails;

public class GetAccountDetailsQueryValidator : AbstractValidator<GetAccountDetailsQuery>
{
    public GetAccountDetailsQueryValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotNull().WithMessage("Account number is required")
            .Must(x => !string.IsNullOrWhiteSpace(x.Value))
            .WithMessage("Account number cannot be empty")
            .Must(x => x.Value.Length == 10)
            .WithMessage("Account number must be 10 digits")
            .Must(x => x.Value.All(char.IsDigit))
            .WithMessage("Account number must contain only digits");
    }
}
