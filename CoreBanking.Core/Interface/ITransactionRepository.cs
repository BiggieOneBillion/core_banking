using System;
using CoreBanking.Core.Entities;
using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Core.Interface;

public interface ITransactionRepository
{

     Task<Transaction?> GetByIdAsync(TransactionId transactionId, CancellationToken cancellationToken = default);
     Task<IEnumerable<Transaction>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default);

     Task<IEnumerable<Transaction>> GetByAccountIdAndDateRangeAsync(AccountId accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

     Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

     Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}


