using CoreBanking.Application.Accounts.Queries.GetAccountSummaries;
using CoreBanking.Application.Common.Interfaces;

namespace CoreBanking.Application.Accounts.Queries.GetAccountSummary;

public record GetAccountSummaryQuery: IQuery<List<AccountSummaryDto>>;

