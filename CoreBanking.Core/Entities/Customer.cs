using System;
using CoreBanking.Core.Interface;
using CoreBanking.Core.Models;
using CoreBanking.Core.ValueObjects;

namespace CoreBanking.Core.Entities;

public class Customer : ISoftDelete
{
    public CustomerId CustomerId { get; private set; }
    public string Firstname { get; private set; }
    public string Lastname { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }

    public DateTime DateCreated { get; private set; }

    public bool IsActive { get; private set; }

     public bool IsDeleted { get; private set; }
     public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    private readonly List<Account> _account = new();

    public IReadOnlyCollection<Account> Accounts => _account.AsReadOnly();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Customer() { } // EF Core needs this
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public Customer(string firstName, string lastName, string email, string phoneNumber)
    {
        CustomerId = CustomerId.Create();
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

     public void SoftDelete(string deletedBy)
        {
            if (Accounts.Any(a => a.Balance.Amount > 0))
                throw new InvalidOperationException("Cannot delete customer with account balance");
                
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
        }


}
