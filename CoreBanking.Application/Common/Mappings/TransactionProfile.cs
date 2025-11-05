using AutoMapper;
using CoreBanking.Application.Accounts.Queries.GetTransactionHistory;
using CoreBanking.Core.Entities;

namespace CoreBanking.Application.Common.Mappings;

public class TransactionProfile : Profile
{
    public TransactionProfile()
    {
        // Transaction entity to TransactionDto mapping
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.TransactionId.Value))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Amount.Currency))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => src.Reference))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.RunningBalance, opt => opt.Ignore()); // RunningBalance is calculated separately
    }
}
