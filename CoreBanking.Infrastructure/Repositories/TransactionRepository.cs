// using System.Transactions;
using CoreBanking.Core.Entities;
using CoreBanking.Core.Interface;
using CoreBanking.Core.ValueObjects;
using CoreBankingTest.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Infrastructure.Repositories;

	public class TransactionRepository : ITransactionRepository
	{
		private readonly BankingDbContext _context;

		public TransactionRepository(BankingDbContext context)
		{
			_context = context;
		}

		public async Task<Transaction?> GetByIdAsync(TransactionId transactionId, CancellationToken cancellationToken = default)
        {
        return await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId, cancellationToken);
		}

		public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default)
		{
			return await _context.Transactions
				.Where(t => t.AccountId == accountId)
				.OrderByDescending(t => t.Timestamp)
				.ToListAsync(cancellationToken);
		}

		public async Task<IEnumerable<Transaction>> GetByAccountIdAndDateRangeAsync(AccountId accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
		{
			return await _context.Transactions
				.Where(t => t.AccountId == accountId &&
						   t.Timestamp >= startDate &&
						   t.Timestamp <= endDate)
				.OrderBy(t => t.Timestamp)
				.ToListAsync(cancellationToken);
		}

		public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
		{
			await _context.Transactions.AddAsync(transaction, cancellationToken);
		}

		public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
		{
			_context.Transactions.Update(transaction);
			await Task.CompletedTask;
		}

    public Task<IEnumerable<Core.Entities.Transaction>> GetAccountIdAsync(Guid accountId)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Core.Entities.Transaction transaction)
    {
        throw new NotImplementedException();
    }
}

