using System;

namespace CoreBanking.Core.ValueObjects;

public record CustomerId(Guid Value)
{
    public static CustomerId Create() => new(Guid.NewGuid());
    public static CustomerId Create(Guid value) => new(value);
}


