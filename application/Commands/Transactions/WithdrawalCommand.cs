using application.Contracts.Managers;
using FluentValidation;
using MediatR;

namespace application.Commands.Transactions;
public record InitiateWithdrawalCommand(
    Guid AccountId,
    decimal Amount,
    string PIN,
    string BankCode,
    string AccountNumber,
    string AccountName,
    string Description = "External Transfer"
) : IRequest<InitiateWithdrawalResult>;

public record InitiateWithdrawalResult(
    string TransferReference,
    Guid AccountId,
    decimal Amount,
    string BankCode,
    string AccountNumber,
    string AccountName,
    DateTime InitiatedAt,
    bool IsSuccessful,
    string Message,
    string? PaystackRecipientCode = null,
    string? PaystackTransferCode = null
);

public class InitiateWithdrawalCommandHandler(ITransactionManager transactionManager) : IRequestHandler<InitiateWithdrawalCommand, InitiateWithdrawalResult>
{
    public async Task<InitiateWithdrawalResult> Handle(InitiateWithdrawalCommand request, CancellationToken cancellationToken)
    {
        return await transactionManager.InitiateExternalWithdrawal(request, cancellationToken);
    }
}

public class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(100000)
            .WithMessage("Amount cannot exceed 100,000");

        RuleFor(x => x.PIN)
            .NotEmpty()
            .WithMessage("PIN is required")
            .Length(4)
            .WithMessage("PIN must be between 4 and 6 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(200)
            .WithMessage("Description cannot exceed 200 characters");
    }
}