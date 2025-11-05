using CoreBanking.Application.Common.Interfaces;
using CoreBanking.Application.Common.Models;
using CoreBanking.Core.Interface;
using CoreBanking.Core.ValueObjects;
using MediatR;

namespace CoreBanking.Application.Accounts.Queries.GetTransactionHistory;

public record GetTransactionHistoryQuery : IQuery<TransactionHistoryDto>
{
    // public string AccountNumber { get; init; } = string.Empty;
     public AccountNumber AccountNumber { get; init; } = AccountNumber.Create(string.Empty);
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public record TransactionHistoryDto
{
    public string AccountNumber { get; init; } = string.Empty;
    public List<TransactionDto> Transactions { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int TotalPages { get; init; }
}



