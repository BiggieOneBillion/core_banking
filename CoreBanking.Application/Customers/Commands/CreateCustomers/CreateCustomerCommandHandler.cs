// using CoreBanking.Application.Common.Models;
// using CoreBanking.Core.Entities;
// using CoreBanking.Core.Interface;
// using CoreBanking.Core.Interfaces;
// using MediatR;

// namespace CoreBanking.Application.Customers.Commands.CreateCustomers;

// public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
// {
//     private readonly ICustomerRepository _customerRepository;
//     private readonly IUnitOfWork _unitOfWork;

//     public CreateCustomerCommandHandler(
//         ICustomerRepository customerRepository,
//         IUnitOfWork unitOfWork)
//     {
//         _customerRepository = customerRepository;
//         _unitOfWork = unitOfWork;
//     }

//     public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
//     {
//         // Validate input
//         if (string.IsNullOrWhiteSpace(request.FirstName))
//             return Result<Guid>.Failure("First name is required");

//         if (string.IsNullOrWhiteSpace(request.LastName))
//             return Result<Guid>.Failure("Last name is required");

//         if (string.IsNullOrWhiteSpace(request.Email))
//             return Result<Guid>.Failure("Email is required");

//         if (string.IsNullOrWhiteSpace(request.Phone))
//             return Result<Guid>.Failure("Phone number is required");

//         // Create customer
//         var customer = new Customer(
//             firstName: request.FirstName,
//             lastName: request.LastName,
//             email: request.Email,
//             phoneNumber: request.Phone
//         );

//         // Add to repository
//         await _customerRepository.AddAsync(customer);
//         await _unitOfWork.SaveChangesAsync(cancellationToken);

//         return Result<Guid>.Success(customer.CustomerId.Value);
//     }
// }

// using CoreBanking.Application.Common.Models;
// using CoreBanking.Application.Customers.Queries.GetCustomerDetails;
// using CoreBanking.Core.Entities;
// using CoreBanking.Core.Interface;
// using CoreBanking.Core.Interfaces;
// using MediatR;

// namespace CoreBanking.Application.Customers.Commands.CreateCustomers;

// public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
// {
//     private readonly ICustomerRepository _customerRepository;
//     private readonly IUnitOfWork _unitOfWork;

//     public CreateCustomerCommandHandler(
//         ICustomerRepository customerRepository,
//         IUnitOfWork unitOfWork)
//     {
//         _customerRepository = customerRepository;
//         _unitOfWork = unitOfWork;
//     }

//     public async Task<Result<CustomerDetailsDto>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
//     {
//         // Validate input
//         if (string.IsNullOrWhiteSpace(request.FirstName))
//             return Result<CustomerDetailsDto>.Failure("First name is required");

//         if (string.IsNullOrWhiteSpace(request.LastName))
//             return Result<CustomerDetailsDto>.Failure("Last name is required");

//         if (string.IsNullOrWhiteSpace(request.Email))
//             return Result<CustomerDetailsDto>.Failure("Email is required");

//         if (string.IsNullOrWhiteSpace(request.Phone))
//             return Result<CustomerDetailsDto>.Failure("Phone number is required");

//         // Create customer
//         var customer = new Customer(
//             firstName: request.FirstName,
//             lastName: request.LastName,
//             email: request.Email,
//             phoneNumber: request.Phone
//         );

//         // Add to repository
//         await _customerRepository.AddAsync(customer);
//         await _unitOfWork.SaveChangesAsync(cancellationToken);

//         return Result<CustomerDetailsDto>.Success(customer);
//     }
// }

