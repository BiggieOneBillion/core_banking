using System;
using CoreBanking.Application.Common.Models;
using CoreBanking.Core.Interface;
using CoreBanking.Core.ValueObjects;
using MediatR;

namespace CoreBanking.Application.Accounts.Queries.GetTransactionHistory;

public class GetTransactionHistoryQueryHandler : IRequestHandler<GetTransactionHistoryQuery, Result<TransactionHistoryDto>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;

    public GetTransactionHistoryQueryHandler(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
    }

    public async Task<Result<TransactionHistoryDto>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByAccountNumberAsync(new AccountNumber(request.AccountNumber));
        if (account == null)
            return Result<TransactionHistoryDto>.Failure("Account not found");

        // Start with IQueryable or IEnumerable from repository
        var transactionsQuery = (await _transactionRepository.GetByAccountIdAsync(account.AccountId, cancellationToken))
            .AsQueryable(); // Or keep as IEnumerable

        // Apply date filtering
        if (request.StartDate.HasValue)
            transactionsQuery = transactionsQuery.Where(t => t.Timestamp >= request.StartDate.Value);
        if (request.EndDate.HasValue)
            transactionsQuery = transactionsQuery.Where(t => t.Timestamp <= request.EndDate.Value);

        // Get total count before pagination
        var totalCount = transactionsQuery.Count();

        // Apply pagination and execute query
        var pagedTransactions = transactionsQuery
            .OrderByDescending(t => t.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var transactionDtos = pagedTransactions.Select(t => new TransactionDto
        {
            // TransactionId = t.TransactionId.Value.ToString(),
            TransactionId = TransactionId.Create(t.TransactionId.Value).Value,
            Type = t.Type.ToString(),
            Amount = t.Amount.Amount,
            Currency = t.Amount.Currency,
            Description = t.Description,
            Reference = t.Reference,
            Timestamp = t.Timestamp
        }).ToList();

        var dto = new TransactionHistoryDto
        {
            AccountNumber = request.AccountNumber,
            Transactions = transactionDtos,
            TotalCount = totalCount,
            Page = request.Page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };

        return Result<TransactionHistoryDto>.Success(dto);
    }
}

