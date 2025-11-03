using System;

namespace CoreBanking.Application.Accounts.Queries.GetTransactionHistory;

public record TransactionDto
{
    public Guid TransactionId { get; init; } 
    // public string TransactionId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public decimal RunningBalance { get; init; }
}
