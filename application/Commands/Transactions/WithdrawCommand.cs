using application.Contracts.Managers;
using MediatR;

namespace application.Commands.Transactions;

public record WithdrawCommand(
    Guid AccountId,
    decimal Amount,
    string PIN,
    string Description = "Withdrawal",
    TransferType TransferType = TransferType.Internal,
    Guid? TargetAccountId = null, // For internal transfers
    string? BankCode = null, // For external transfers
    string? AccountNumber = null, // For external transfers
    string? AccountName = null // For external transfers
) : IRequest<WithdrawResult>;

public record WithdrawResult(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    decimal NewBalance,
    string Description,
    DateTime TransactionDate,
    bool IsSuccessful,
    string Message,
    string? TransferReference = null,
    TransferStatus Status = TransferStatus.Completed
);

public enum TransferType
{
    Internal, // Transfer to another account within the same bank
    External  // Transfer to external bank via Paystack
}

public enum TransferStatus
{
    Pending,
    Completed,
    Failed,
    Reversed
}

public class WithdrawCommandHandler(ITransactionManager transactionManager) : IRequestHandler<WithdrawCommand, WithdrawResult>
{
    public async Task<WithdrawResult> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        return await transactionManager.Withdraw(request, cancellationToken);
    }
}