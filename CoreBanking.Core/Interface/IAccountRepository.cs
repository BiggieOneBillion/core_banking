using System;
using CoreBanking.Core.Entities;
using CoreBanking.Core.Models;
using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Core.Interface;

public interface IAccountRepository
{
    Task<Account> GetByIdAsync(Guid accountId);
    
    Task<Account> GetByAccountNumberAsync(AccountNumber accountNumber);

    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);

    Task AddAsync(Account account);

    Task UpdateAsync(Account account);

    Task<bool> AccountNumberExistsAsync(AccountNumber accountNumber);

    // AccountModel GetById(Guid id);

    Task<List<Account>> GetAllAsync();

    // void Add(AccountModel account);
}
