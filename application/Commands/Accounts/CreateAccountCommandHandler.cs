using application.Contracts.Managers;
using domain.Enums;
using FluentValidation;
using MediatR;

namespace application.Commands.Accounts;

public record CreateAccountCommand(
    string AccountHolderName,
    string Email,
    AccountType AccountType,
    string PIN,
    Guid UserId
) : IRequest<CreateAccountResult>;

public record CreateAccountResult(
    Guid AccountId,
    string AccountNumber,
    AccountType AccountType,
    DateTime CreatedAt
);

public class CreateAccountCommandHandler(
    IAccountManager accountManager) : IRequestHandler<CreateAccountCommand, CreateAccountResult>
{
    public async Task<CreateAccountResult> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        return await accountManager.CreateAccount(request, cancellationToken);
    }
}

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.AccountHolderName)
            .NotEmpty()
            .WithMessage("Account holder name is required")
            .MaximumLength(100)
            .WithMessage("Account holder name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address");

        RuleFor(x => x.AccountType)
            .IsInEnum()
            .WithMessage("Invalid account type");

        RuleFor(x => x.PIN)
            .NotEmpty()
            .WithMessage("PIN is required")
            .Length(4)
            .WithMessage("PIN must be 4 characters")
            .Matches(@"^\d+$")
            .WithMessage("PIN must contain only digits");
    }
}