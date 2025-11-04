using AutoMapper;
using CoreBanking.Application.Common.Models;
using CoreBanking.Core.Interface;
using MediatR;

namespace CoreBanking.Application.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, Result<List<CustomerDto>>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<CustomerDto>>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await _customerRepository.GetAllAsync();
        var customerDtos = _mapper.Map<List<CustomerDto>>(customers);

        return Result<List<CustomerDto>>.Success(customerDtos);
    }
}
