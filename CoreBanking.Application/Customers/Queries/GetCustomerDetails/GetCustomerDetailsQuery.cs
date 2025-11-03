using System;
using CoreBanking.Application.Common.Interfaces;

namespace CoreBanking.Application.Customers.Queries.GetCustomerDetails;

public record GetCustomerDetailsQuery: IQuery<CustomerDetailsDto>
{
    public Guid CustomerId { get; init; }
}
