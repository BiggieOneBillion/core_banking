using CoreBanking.APP.Common;
using CoreBanking.Core.ValueObjects;

public record MoneyTransferedEVent : DomainEvent
{
    public TransactionId TransactionId { get; }
    public AccountNumber SourceAccountNumber { get; }
    public AccountNumber DestinationAccountNumber { get; }
    public Money Amount { get; }
    public string Reference { get; }
    public DateTime TransferDate { get; }
    public MoneyTransferedEVent(TransactionId transactionId, AccountNumber sourceAccountNumber, AccountNumber destinationAccountNumber, Money amount, string reference)
    {
        TransactionId = transactionId;
        SourceAccountNumber = sourceAccountNumber;
        DestinationAccountNumber = destinationAccountNumber;
        Amount = amount;
        Reference = reference;
        TransferDate = DateTime.UtcNow;
    }
}
