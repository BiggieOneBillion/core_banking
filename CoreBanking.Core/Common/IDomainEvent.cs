
// CoreBanking.Core/Common/IDomainEvent.cs
namespace CoreBanking.Core.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}