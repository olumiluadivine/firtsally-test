using application.Contracts.Managers;
using FluentValidation;
using MediatR;

namespace application.Commands.Transactions;

public record InitiateDepositCommand(
Guid AccountId,
decimal Amount,
string Description = "Deposit",
string? CustomerEmail = null
) : IRequest<InitiateDepositResult>;

public record InitiateDepositResult(
    string PaymentReference,
    string PaymentUrl,
    string AccessCode,
    decimal Amount,
    string Currency,
    DateTime ExpiresAt,
    Guid AccountId,
    string Message
);

public class InitiateDepositCommandHandler(
ITransactionManager transactionManger) : IRequestHandler<InitiateDepositCommand, InitiateDepositResult>
{
    public async Task<InitiateDepositResult> Handle(InitiateDepositCommand request, CancellationToken cancellationToken)
    {
        return await transactionManger.HandleDeposit(request, cancellationToken);
    }
}

public class InitiateDepositCommandValidator : AbstractValidator<InitiateDepositCommand>
{
    public InitiateDepositCommandValidator()
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

        RuleFor(x => x.CustomerEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.CustomerEmail))
            .WithMessage("Customer email must be a valid email address");
    }
}