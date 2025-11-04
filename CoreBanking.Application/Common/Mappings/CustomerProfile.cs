using AutoMapper;
using CoreBanking.Application.Customers.Commands.CreateCustomers;
using CoreBanking.Application.Customers.Queries.GetCustomerDetails;
using CoreBanking.Application.Customers.Queries.GetCustomers;
using CoreBanking.Core.Entities;

namespace CoreBanking.Application.Common.Mappings;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        // Note: Request to Command mapping should be done in the API layer to avoid circular dependency

        // Domain Entity to DTO mappings
        CreateMap<Customer, CustomerDto>()
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId.Value))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Firstname))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Lastname))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PhoneNumber));

        CreateMap<Customer, CustomerDetailsDto>()
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId.Value))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Firstname))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Lastname))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));
            // .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            // .ForMember(dest => dest.TotalAccounts, opt => opt.MapFrom(src => src.Accounts.Count));
    }
}
