using System;
using CoreBanking.Core.Entities;

namespace CoreBanking.Core.Interface;

public interface ICustomerRepository
{
    Task<Customer> GetByIdAsync(Guid customerId);
    Task<IEnumerable<Customer>> GetAllAsync();

    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);

    Task<bool> ExistsAsync(Guid customerId);
}
