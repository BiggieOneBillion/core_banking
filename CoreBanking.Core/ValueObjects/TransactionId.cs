using System;

namespace CoreBanking.Core.ValueObjects;

public record TransactionId(Guid Value)
{
   public static TransactionId Create() => new(Guid.NewGuid());
    public static TransactionId Create(Guid value) => new(value);
}
