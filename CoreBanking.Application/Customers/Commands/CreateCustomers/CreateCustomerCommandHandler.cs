using CoreBanking.Application.Common.Models;
using CoreBanking.Core.Entities;
using CoreBanking.Core.Interface;
using CoreBanking.Core.Interfaces;
using MediatR;

namespace CoreBanking.Application.Customers.Commands.CreateCustomers;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Note: Validation is handled by CreateCustomerCommandValidator via MediatR pipeline

        // Create customer using domain constructor
        var customer = new Customer(
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            phoneNumber: request.Phone
        );

        // Persist to database
        await _customerRepository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(customer.CustomerId.Value);
    }
}

