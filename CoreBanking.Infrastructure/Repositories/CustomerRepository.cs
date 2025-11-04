using CoreBanking.Core.Entities;
using CoreBanking.Core.Interface;
using CoreBankingTest.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Infrastructure.Repositories
    {
        public class CustomerRepository : ICustomerRepository
        {
            private readonly BankingDbContext _context;

            public CustomerRepository(BankingDbContext context)
            {
                _context = context;
            }

            public async Task<Customer?> GetByIdAsync(Guid customerId)
            {
            return await _context.Customers.Include(c => c.Accounts).FirstOrDefaultAsync(c => c.CustomerId.Value == customerId);
                // .FirstOrDefaultAsync(c => c.CustomerId == customerId);
                    // .Include(c => c.Accounts)
            }

            public async Task<IEnumerable<Customer>> GetAllAsync()
            {
                return await _context.Customers
                    .Include(c => c.Accounts)
                    .ToListAsync();
            }

            public async Task AddAsync(Customer customer)
            {
                await _context.Customers.AddAsync(customer);
            }

            public async Task UpdateAsync(Customer customer)
            {
                _context.Customers.Update(customer);
                await Task.CompletedTask;
            }

            public async Task<bool> ExistsAsync(Guid customerId)
            {
                return await _context.Customers
                    .AnyAsync(c => c.CustomerId.Value == customerId);
            }

            public async Task SaveChangesAsync()
            {
                await _context.SaveChangesAsync();
            }
        }
    }


