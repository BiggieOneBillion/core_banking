using CoreBanking.Application.Common.Interfaces;
using CoreBanking.Application.Common.Models;
using CoreBanking.Core.Interface;
using CoreBanking.Core.ValueObjects;
using MediatR;

namespace CoreBanking.Application.Accounts.Queries.GetAccountDetails;

public record GetAccountDetailsQuery : IQuery<AccountDetailsDto>
{
    // public string AccountNumber { get; init; } = string.Empty;
    public AccountNumber AccountNumber { get; init; } = AccountNumber.Create(string.Empty);
}

public record AccountDetailsDto
{
    // public string AccountNumber { get; init; } = string.Empty;
    // public string AccountType { get; init; } = string.Empty;
    // public decimal Balance { get; init; }
    // public string Currency { get; init; } = string.Empty;

    public AccountNumber AccountNumber { get; init; } = AccountNumber.Create(string.Empty);
    public string AccountType { get; init; } = string.Empty;
    public Money Balance { get; init; } = new Money(0);
    public DateTime DateOpened { get; init; }
    public bool IsActive { get; init; }
    public string CustomerName { get; init; } = string.Empty;
}

public class GetAccountDetailsQueryHandler : IRequestHandler<GetAccountDetailsQuery, Result<AccountDetailsDto>>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountDetailsQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    // public async Task<Result<AccountDetailsDto>> Handle(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    // {
    //     var account = await _accountRepository.GetByAccountNumberAsync(new AccountNumber(request.AccountNumber));

    //     if (account == null)
    //         return Result<AccountDetailsDto>.Failure("Account not found");

    //     var dto = new AccountDetailsDto
    //     {
    //         AccountNumber = account.AccountNumber.Value,
    //         AccountType = account.AccountType.ToString(),
    //         Balance = account.Balance.Amount,
    //         Currency = account.Balance.Currency,
    //         DateOpened = account.DateOpened,
    //         IsActive = account.IsActive,
    //         CustomerName = $"{account.Customer.Firstname} {account.Customer.Lastname}"
    //     };

    //     return Result<AccountDetailsDto>.Success(dto);
    // }
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
            CustomerName = $"{account.Customer.Firstname} {account.Customer.Lastname}"
        };

        return Result<AccountDetailsDto>.Success(dto);
    }
}