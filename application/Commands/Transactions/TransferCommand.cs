using application.Contracts.Managers;
using FluentValidation;
using MediatR;

namespace application.Commands.Transactions;

public record TransferCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string PIN,
    string Description = "Transfer"
) : IRequest<TransferResult>;

public record TransferResult(
    Guid FromTransactionId,
    Guid ToTransactionId,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    decimal FromAccountNewBalance,
    decimal ToAccountNewBalance,
    string Description,
    DateTime TransactionDate
);

public class TransferCommandHandler(
    ITransactionManager transactionManager) : IRequestHandler<TransferCommand, TransferResult>
{
    public async Task<TransferResult> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        return await transactionManager.Transfer(request, cancellationToken);
    }
}

public class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    public TransferCommandValidator()
    {
        RuleFor(x => x.FromAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required");

        RuleFor(x => x.ToAccountId)
            .NotEmpty()
            .WithMessage("Destination account ID is required")
            .NotEqual(x => x.FromAccountId)
            .WithMessage("Cannot transfer to the same account");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(100000)
            .WithMessage("Amount cannot exceed 100,000");

        RuleFor(x => x.PIN)
            .NotEmpty()
            .WithMessage("PIN is required")
            .Length(4)
            .WithMessage("PIN must be 4 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(200)
            .WithMessage("Description cannot exceed 200 characters");
    }
}