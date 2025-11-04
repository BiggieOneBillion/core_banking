using AutoMapper;
using CoreBanking.Application.Common.Models;
using CoreBanking.Core.Interface;
using MediatR;

namespace CoreBanking.Application.Customers.Queries.GetCustomerDetails;

public class GetCustomerDetailsQueryHandler : IRequestHandler<GetCustomerDetailsQuery, Result<CustomerDetailsDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public GetCustomerDetailsQueryHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDetailsDto>> Handle(GetCustomerDetailsQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);

        if (customer == null)
            return Result<CustomerDetailsDto>.Failure("Customer not found");

        var customerDto = _mapper.Map<CustomerDetailsDto>(customer);

        return Result<CustomerDetailsDto>.Success(customerDto);
    }
}
