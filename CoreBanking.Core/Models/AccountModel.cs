using System;

namespace CoreBanking.Core.Models;

public class AccountModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public string Currency { get; set; }
}
