using System;
using CoreBanking.Core.Models;

namespace CoreBanking.Core.Entities;

public class Customer
{
    public Guid CustomerId { get; private set; }
    public string Firstname { get; private set; }
    public string Lastname { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }

    public DateTime DateCreated { get; private set; }

    public bool IsActive { get; private set; }

    private readonly List<Account> _account = new();

    public IReadOnlyCollection<Account> Accounts => _account.AsReadOnly();

    public Customer(string firstName, string lastName, string email, string phoneNumber)
    {
        CustomerId = Guid.NewGuid();
        Firstname = firstName ?? throw new ArgumentNullException(nameof(firstName));
        Lastname = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        DateCreated = DateTime.UtcNow;
        IsActive = true;
    }

    public void UpdateContactInfo(string email, string phoneNumber)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive customer");

        Email = email;
        PhoneNumber = phoneNumber;
    }

    public void Deactivate()
    {
        if (_account.Any(a => a.Balance.Amount > 0))
            throw new InvalidOperationException("Cannot deactivate customer with account");

        IsActive = false;
    }

    internal void AddAccount(Account account)
    {
        _account.Add(account);
    }

}
