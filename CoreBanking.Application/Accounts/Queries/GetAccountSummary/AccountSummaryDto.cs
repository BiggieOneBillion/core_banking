using CoreBanking.Core.Entities;
using CoreBanking.Core.Enums;
using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Application.Accounts.Queries.GetAccountSummaries;

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
