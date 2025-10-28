namespace CoreBanking.Core.ValueObjects;

public record class Money
{
   public decimal Amount { get; }
    public string Currency { get; } = "ngn";

    public Money(decimal amount, string currency = "NGN")
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative");

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add different currencies");

        return new Money(a.Amount + b.Amount, a.Currency);
    }

     public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot substract different currencies");

        return new Money(a.Amount - b.Amount, a.Currency);
    }
}
