using System.Reflection.Metadata.Ecma335;
using CoreBanking.Core.Entities;
using CoreBanking.Core.Interface;
using CoreBanking.Core.Models;
using CoreBanking.Core.ValueObjects;
using CoreBanking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly BankingDbContext _context;

        public AccountRepository(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetByIdAsync(Guid accountId)
        {
            //  return await _context.Accounts
            //        .Include(a => a.Customer)
            //        .Include(a => a.Transactions)
            //        .FirstOrDefaultAsync(a => a.AccountId.Value! == accountId);
            return await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.AccountId.Value! == accountId);
        }

        public async Task<Account?> GetByAccountNumberAsync(AccountNumber accountNumber)
        {
            return await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

        public async Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _context.Accounts
                .Where(a => a.CustomerId.Value == customerId)
                .Include(a => a.Transactions)
                .ToListAsync();
        }

        public async Task AddAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
        }

        public async Task UpdateAsync(Account account)
        {
            _context.Accounts.Update(account);
            await Task.CompletedTask;
        }

        public async Task<bool> AccountNumberExistsAsync(AccountNumber accountNumber)
        {
            return await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        
       
        public async Task<List<Account>> GetAllAsync()
        {
            return await _context.Accounts
                .Include(a => a.Transactions)
                .ToListAsync();
        }

    }
}
