using System;
using CoreBanking.Core.Entities;

namespace CoreBanking.Core.Interface;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetAccountIdAsync(Guid accountId);
    Task AddAsync(Transaction transaction);
}
