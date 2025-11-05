using System;

namespace CoreBanking.Core.Exceptions;

public class InsufficientFundsException : Exception
{
    public string AccountNumber { get; }
    public decimal RequestedAmount { get; }
    public decimal AvailableBalance { get; }

    public InsufficientFundsException(string accountNumber, decimal requestedAmount, decimal availableBalance)
        : base($"Insufficient funds in account {accountNumber}. Requested: {requestedAmount:C}, Available: {availableBalance:C}")
    {
        AccountNumber = accountNumber;
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }

    public InsufficientFundsException(string accountNumber, decimal requestedAmount, decimal availableBalance, string message)
        : base(message)
    {
        AccountNumber = accountNumber;
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }

    public InsufficientFundsException(string accountNumber, decimal requestedAmount, decimal availableBalance, string message, Exception innerException)
        : base(message, innerException)
    {
        AccountNumber = accountNumber;
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }
}
