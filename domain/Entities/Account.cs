using domain.Enums;
using domain.ValueObjects;

namespace domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AccountNumber { get; private set; }
    public Money Balance { get; private set; }
    public AccountType AccountType { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public string PINHash { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    private Account()
    { }

    public Account(Guid userId, string accountNumber, AccountType accountType, string pinHash)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        Balance = Money.Zero();
        AccountType = accountType;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
        PINHash = pinHash ?? throw new ArgumentNullException(nameof(pinHash));
    }

    public Transaction Deposit(decimal amount, string description = "Deposit")
    {
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive", nameof(amount));

        if (!IsActive)
            throw new InvalidOperationException("Cannot deposit to an inactive account");

        var transaction = new Transaction(
            Id,
            null,
            TransactionType.Deposit,
            new Money(amount),
            description);

        Balance = Balance.Add(amount);
        _transactions.Add(transaction);
        UpdatedAt = DateTime.UtcNow;

        return transaction;
    }

    public Transaction Withdraw(decimal amount, string description = "Withdrawal")
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive", nameof(amount));

        if (!IsActive)
            throw new InvalidOperationException("Cannot withdraw from an inactive account");

        if (Balance.Amount < amount)
            throw new InvalidOperationException("Insufficient funds");

        var transaction = new Transaction(
            Id,
            null,
            TransactionType.Withdrawal,
            new Money(amount),
            description);

        Balance = Balance.Subtract(amount);
        _transactions.Add(transaction);
        UpdatedAt = DateTime.UtcNow;

        return transaction;
    }

    public Transaction Transfer(Guid toAccountId, decimal amount, string description = "Transfer")
    {
        if (amount <= 0)
            throw new ArgumentException("Transfer amount must be positive", nameof(amount));

        if (!IsActive)
            throw new InvalidOperationException("Cannot transfer from an inactive account");

        if (Balance.Amount < amount)
            throw new InvalidOperationException("Insufficient funds");

        if (toAccountId == Id)
            throw new InvalidOperationException("Cannot transfer to the same account");

        var transaction = new Transaction(
            Id,
            toAccountId,
            TransactionType.Transfer,
            new Money(amount),
            description);

        Balance = Balance.Subtract(amount);
        _transactions.Add(transaction);
        UpdatedAt = DateTime.UtcNow;

        return transaction;
    }

    public Transaction ReceiveTransfer(decimal amount, Guid fromAccountId, string description = "Transfer received")
    {
        if (amount <= 0)
            throw new ArgumentException("Transfer amount must be positive", nameof(amount));

        if (!IsActive)
            throw new InvalidOperationException("Cannot receive transfer to an inactive account");

        var transaction = new Transaction(
            Id,
            fromAccountId,
            TransactionType.TransferReceived,
            new Money(amount),
            description);

        Balance = Balance.Add(amount);
        _transactions.Add(transaction);
        UpdatedAt = DateTime.UtcNow;

        return transaction;
    }

    public void DeactivateAccount()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ActivateAccount()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePIN(string newPINHash)
    {
        PINHash = newPINHash ?? throw new ArgumentNullException(nameof(newPINHash));
        UpdatedAt = DateTime.UtcNow;
    }

    public IEnumerable<Transaction> GetTransactionHistory(DateTime? from = null, DateTime? to = null)
    {
        var query = _transactions.AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        return query.OrderByDescending(t => t.CreatedAt);
    }

    public IEnumerable<Transaction> GetMonthlyStatement(int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        return GetTransactionHistory(from, to);
    }
}