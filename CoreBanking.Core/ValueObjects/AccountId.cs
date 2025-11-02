using System;

namespace CoreBanking.Core.ValueObjects;

public record AccountId(Guid Value)
{
    public static AccountId Create() => new(Guid.NewGuid());
    public static AccountId Create(Guid value) => new(value);
}
