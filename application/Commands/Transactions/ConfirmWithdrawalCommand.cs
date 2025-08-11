using application.Contracts.Managers;
using FluentValidation;
using MediatR;

namespace application.Commands.Transactions;
public record ConfirmWithdrawalCommand(
    string TransferReference,
    string PaystackTransferCode,
    bool IsSuccessful
) : IRequest<ConfirmWithdrawalResult>;

public record ConfirmWithdrawalResult(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    decimal NewBalance,
    string Description,
    string TransferReference,
    DateTime ProcessedAt,
    bool IsSuccessful,
    string Message,
    TransferStatus Status
);

public class ConfirmWithdrawalCommandHandler(ITransactionManager transactionManager) : IRequestHandler<ConfirmWithdrawalCommand, ConfirmWithdrawalResult>
{
    public async Task<ConfirmWithdrawalResult> Handle(ConfirmWithdrawalCommand request, CancellationToken cancellationToken)
    {
        return await transactionManager.ConfirmWithdrawal(
            request.TransferReference,
            request.PaystackTransferCode,
            request.IsSuccessful,
            cancellationToken);
    }
}

public class ConfirmWithdrawalCommandValidator : AbstractValidator<ConfirmWithdrawalCommand>
{
    public ConfirmWithdrawalCommandValidator()
    {
        RuleFor(x => x.TransferReference)
            .NotEmpty()
            .WithMessage("Transfer reference is required")
            .MaximumLength(100)
            .WithMessage("Transfer reference cannot exceed 100 characters");

        RuleFor(x => x.PaystackTransferCode)
            .NotEmpty()
            .WithMessage("Paystack transfer code is required")
            .MaximumLength(50)
            .WithMessage("Paystack transfer code cannot exceed 50 characters");

        RuleFor(x => x.IsSuccessful)
            .NotNull()
            .WithMessage("IsSuccessful flag is required");
    }
}