using domain.Enums;

namespace domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    
    private readonly List<Account> _accounts = new();
    public IReadOnlyList<Account> Accounts => _accounts.AsReadOnly();

    private User() { } // For EF Core

    public User(string firstName, string lastName, string email, string phoneNumber, string passwordHash)
    {
        Id = Guid.NewGuid();
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
        IsEmailVerified = false;
    }

    public string FullName => $"{FirstName} {LastName}";

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeactivateUser()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        
        // Deactivate all user accounts
        foreach (var account in _accounts)
        {
            account.DeactivateAccount();
        }
    }

    public void ActivateUser()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string firstName, string lastName, string phoneNumber)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));
            
        _accounts.Add(account);
        UpdatedAt = DateTime.UtcNow;
    }

    public Account? GetPrimaryAccount()
    {
        return _accounts.FirstOrDefault(a => a.IsActive);
    }

    public Account? GetAccountByType(AccountType accountType)
    {
        return _accounts.FirstOrDefault(a => a.AccountType == accountType && a.IsActive);
    }
}