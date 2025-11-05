using System;
using CoreBanking.Application.Common.Models;
using CoreBanking.Core.Interface;
using CoreBanking.Core.Interfaces;
using CoreBanking.Core.ValueObjects;
using MediatR;

namespace CoreBanking.Application.Accounts.Commands.TransferMoney;

    public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand, Result>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TransferMoneyCommandHandler(IAccountRepository accountRepository, ITransactionRepository transactionRepository, IUnitOfWork unitOfWork)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var sourceAccount = await _accountRepository.GetByAccountNumberAsync(new AccountNumber(request.SourceAccountNumber));
                var destAccount = await _accountRepository.GetByAccountNumberAsync(new AccountNumber(request.DestinationAccountNumber));

                if (sourceAccount == null) return Result.Failure("Source account not found");
                if (destAccount == null) return Result.Failure("Destination account not found");


            // sourceAccount.Transfer(
            //     amount: new Money(request.Amount, request.Currency),
            //     destination: destAccount,
            //     reference: request.Reference,
            //     description: request.Description
            //     );
            sourceAccount.Transfer(
                amount: request.Amount,
                destination: destAccount,
                reference: request.Reference,
                description: request.Description
            );

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure($"{ex.Message}");
            }
            catch (Exception ex) {
                return Result.Failure($"An unexpected error occurred : {ex.Message}");
            }


    }


}

