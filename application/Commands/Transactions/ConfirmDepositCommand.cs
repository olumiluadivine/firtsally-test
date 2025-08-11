using application.Contracts.Managers;
using FluentValidation;
using MediatR;

namespace application.Commands.Transactions;
public record ConfirmDepositCommand(
    string PaymentReference,
    string PaystackReference
) : IRequest<ConfirmDepositResult>;

public record ConfirmDepositResult(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    decimal NewBalance,
    string Description,
    string PaymentReference,
    DateTime TransactionDate,
    bool IsSuccessful,
    string Message
);

public class ConfirmDepositCommandHandler(
    ITransactionManager transactionManager) : IRequestHandler<ConfirmDepositCommand, ConfirmDepositResult>
{
    public async Task<ConfirmDepositResult> Handle(ConfirmDepositCommand request, CancellationToken cancellationToken)
    {
        return await transactionManager.ConfirmDeposit(request.PaymentReference, request.PaystackReference, cancellationToken);
    }
}

public class ConfirmDepositCommandValidator : AbstractValidator<ConfirmDepositCommand>
{
    public ConfirmDepositCommandValidator()
    {
        RuleFor(x => x.PaymentReference)
            .NotEmpty()
            .WithMessage("Payment reference is required")
            .MaximumLength(100)
            .WithMessage("Payment reference cannot exceed 100 characters");

        RuleFor(x => x.PaystackReference)
            .NotEmpty()
            .WithMessage("Paystack reference is required")
            .MaximumLength(100)
            .WithMessage("Paystack reference cannot exceed 100 characters");
    }
}