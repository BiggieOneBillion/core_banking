using System;
using CoreBanking.Application.Common.Models;
using MediatR;

namespace CoreBanking.Application.Customers.Commands.CreateCustomers;

public record CreateCustomerCommand: IRequest<Result<Guid>>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
}
