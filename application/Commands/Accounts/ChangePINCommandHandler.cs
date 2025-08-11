using application.Contracts.Managers;
using FluentValidation;
using MediatR;

namespace application.Commands.Accounts;

public record ChangePINCommand(
    Guid AccountId,
    string CurrentPIN,
    string NewPIN
) : IRequest<ChangePINResult>;

public record ChangePINResult(
    bool Success,
    string Message,
    DateTime UpdatedAt
);

public class ChangePINCommandHandler(
    IAccountManager accountManager) : IRequestHandler<ChangePINCommand, ChangePINResult>
{
    public async Task<ChangePINResult> Handle(ChangePINCommand request, CancellationToken cancellationToken)
    {
        return await accountManager.ChangePin(request, cancellationToken);
    }
}

public class ChangePINCommandValidator : AbstractValidator<ChangePINCommand>
{
    public ChangePINCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required.");

        RuleFor(x => x.CurrentPIN)
            .NotEmpty()
            .WithMessage("Current PIN is required.")
            .Matches(@"^\d{4}$")
            .WithMessage("Current PIN must be 4 digits.");

        RuleFor(x => x.NewPIN)
            .NotEmpty()
            .WithMessage("New PIN is required.")
            .Matches(@"^\d{4}$")
            .WithMessage("New PIN must be 4 digits.")
            .NotEqual(x => x.CurrentPIN)
            .WithMessage("New PIN must be different from current PIN.");
    }
}