using application.Contracts.Managers;
using FluentValidation;
using MediatR;

namespace application.Commands.Users;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;

public record LoginResult(
    string Token,
    DateTime ExpiresAt,
    UserProfileDto User
);

public record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    bool IsEmailVerified,
    DateTime CreatedAt
);

public class LoginCommandHandler(
    IUserManager userManager) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await userManager.Login(request, cancellationToken);
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long");
    }
}