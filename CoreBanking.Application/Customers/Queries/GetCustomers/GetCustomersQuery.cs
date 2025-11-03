using CoreBanking.Application.Common.Interfaces;

namespace CoreBanking.Application.Customers.Queries.GetCustomers;

public record class GetCustomersQuery : IQuery<List<CustomerDto>>;

