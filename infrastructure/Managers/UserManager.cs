using application.Commands.Users;
using application.Contracts.Managers;
using application.Contracts.Repos;
using application.Contracts.Services;
using application.Queries.Users;
using domain.Entities;
using FluentValidation;
using FluentValidation.Results;

namespace infrastructure.Managers;

public class UserManager(
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService,
    IAccountNumberGenerator accountNumberGenerator,
    ITokenService tokenService) : IUserManager
{
    public async Task<UserDetailsDto> GetUserDetails(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{request.UserId}' not found.");
        }

        return new UserDetailsDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.CreatedAt,
            user.IsActive,
            user.IsEmailVerified,
            user.LastLoginAt);
    }

    public async Task<UserAccountsDto> GetUserAccount(GetUserAccountsQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdWithAccountsAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{request.UserId}' not found.");
        }

        var accountDtos = user.Accounts.Select(a => new UserAccountDto(
            a.Id,
            a.AccountNumber,
            a.AccountType,
            a.Balance.Amount,
            a.Balance.Currency,
            a.IsActive,
            a.CreatedAt));

        return new UserAccountsDto(
            user.Id,
            user.FullName,
            accountDtos);
    }

    public async Task<LoginResult> Login(LoginCommand request, CancellationToken cancellationToken)
    {
        // Get user by email
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password
        var isPasswordValid = encryptionService.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is deactivated");
        }

        // Update last login
        user.UpdateLastLogin();
        await unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Create token
        var token = tokenService.CreateToken(user);
        var expiresAt = DateTime.UtcNow.AddMonths(1); // Should match token expiration

        var userProfile = new UserProfileDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.IsEmailVerified,
            user.CreatedAt
        );

        return new LoginResult(token, expiresAt, userProfile);
    }

    public async Task<RegisterUserResult> Register(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Business rule validation (beyond FluentValidation)
        await ValidateBusinessRulesAsync(request, cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Hash password and PIN
            var passwordHash = encryptionService.HashPassword(request.Password);
            var pinHash = encryptionService.HashPIN(request.PIN);

            // Create user
            var user = new User(
                request.FirstName,
                request.LastName,
                request.Email,
                request.PhoneNumber,
                passwordHash);

            await unitOfWork.Users.AddAsync(user, cancellationToken);

            // Generate unique account number
            var accountNumber = await accountNumberGenerator.GenerateUniqueAccountNumberAsync("35", cancellationToken);

            // Create initial account for the user
            var account = new Account(
                user.Id,
                accountNumber,
                request.InitialAccountType,
                pinHash);

            await unitOfWork.Accounts.AddAsync(account, cancellationToken);

            // Add account to user
            user.AddAccount(account);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            var token = tokenService.CreateToken(user);

            return new RegisterUserResult(
                token,
                user.CreatedAt);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Example business rule: Check if email domain is allowed
    /// </summary>
    private static bool IsEmailDomainAllowed(string email)
    {
        // Example: Only allow certain domains
        var allowedDomains = new[] { "gmail.com", "outlook.com", "yahoo.com", "company.com" };
        var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
        return domain != null && allowedDomains.Contains(domain);
    }

    /// <summary>
    /// Validates business rules that go beyond basic input validation
    /// </summary>
    private async Task ValidateBusinessRulesAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = new List<ValidationFailure>();

        // Check if user already exists by email
        bool emailExists = await unitOfWork.Users.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            validationErrors.Add(new ValidationFailure(
                nameof(request.Email),
                $"User with email '{request.Email}' already exists.")
            {
                AttemptedValue = request.Email
            });
        }

        // Check if user already exists by phone number
        bool phoneExists = await unitOfWork.Users.ExistsByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (phoneExists)
        {
            validationErrors.Add(new ValidationFailure(
                nameof(request.PhoneNumber),
                $"User with phone number '{request.PhoneNumber}' already exists.")
            {
                AttemptedValue = request.PhoneNumber
            });
        }

        // Additional business rules can be added here
        // Example: Check if email domain is allowed
        if (!IsEmailDomainAllowed(request.Email))
        {
            validationErrors.Add(new ValidationFailure(
                nameof(request.Email),
                "Email domain is not allowed for registration.")
            {
                AttemptedValue = request.Email
            });
        }

        // If there are validation errors, throw exception
        if (validationErrors.Any())
        {
            throw new ValidationException(validationErrors);
        }
    }
}