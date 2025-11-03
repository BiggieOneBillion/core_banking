using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Application.Customers.Queries.GetCustomers;

public record  CustomerDto
{
    public CustomerId CustomerId { get; init; } = CustomerId.Create();
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public DateTime DateRegistered { get; init; }
    public bool IsActive { get; init; }
}
