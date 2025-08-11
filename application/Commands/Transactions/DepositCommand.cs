using application.Contracts.Managers;
using FluentValidation;
using MediatR;

namespace application.Commands.Transactions;

public record DepositCommand(
    Guid AccountId,
    decimal Amount,
    string Description = "Deposit"
) : IRequest<DepositResult>;

public record DepositResult(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    decimal NewBalance,
    string Description,
    DateTime TransactionDate
);

public class DepositCommandHandler(
    ITransactionManager transactionManager) : IRequestHandler<DepositCommand, DepositResult>
{
    public async Task<DepositResult> Handle(DepositCommand request, CancellationToken cancellationToken)
    {
        return await transactionManager.DirectDeposit(request, cancellationToken);
    }
}

public class DepositCommandValidator : AbstractValidator<DepositCommand>
{
    public DepositCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Amount cannot exceed 1,000,000");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(200)
            .WithMessage("Description cannot exceed 200 characters");
    }
}

// Cache data model for pending deposits
public class DepositCacheData
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string PaystackReference { get; set; } = string.Empty;
}