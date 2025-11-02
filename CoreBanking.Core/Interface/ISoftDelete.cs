using System;

namespace CoreBanking.Core.Interface;

  public interface ISoftDelete 
    {
        bool IsDeleted { get; }
        DateTime? DeletedAt { get; }
        string? DeletedBy { get; }
    }

