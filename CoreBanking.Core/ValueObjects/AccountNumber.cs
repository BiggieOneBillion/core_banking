namespace CoreBanking.Core.ValueObjects;

public record class AccountNumber
{
    public string Value { get; }

    private AccountNumber() : this(string.Empty) { }

    public static AccountNumber Create(string value) => new(value);

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 10)
            throw new ArgumentException("Account number must be 10 digits");

        if (!value.All(char.IsDigit))
            throw new ArgumentException("Account number must contain only digits");

        Value = value;
    }

    public static implicit operator string(AccountNumber number) => number.Value;
    public static explicit operator AccountNumber(string value) => new(value);

    public override string ToString() => Value;
    
}
