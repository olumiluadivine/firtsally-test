using application.Contracts.Managers;
using domain.Enums;
using FluentValidation;
using MediatR;

namespace application.Commands.Users;

public record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    AccountType InitialAccountType,
    string PIN
) : IRequest<RegisterUserResult>;

public record RegisterUserResult(
    string token,
    DateTime CreatedAt
);

public class RegisterUserCommandHandler(
    IUserManager userManager) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        return await userManager.Register(request, cancellationToken);
    }
}

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(50)
            .WithMessage("First name cannot exceed 50 characters")
            .Matches(@"^[a-zA-Z\s]+$")
            .WithMessage("First name can only contain letters and spaces");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(50)
            .WithMessage("Last name cannot exceed 50 characters")
            .Matches(@"^[a-zA-Z\s]+$")
            .WithMessage("Last name can only contain letters and spaces");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(255)
            .WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Phone number must be a valid international format");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character");

        RuleFor(x => x.InitialAccountType)
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