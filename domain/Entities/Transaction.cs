using domain.Enums;
using domain.ValueObjects;

namespace domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? RelatedAccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string? Reference { get; private set; }

    private Transaction()
    { } // For EF Core

    public Transaction(
        Guid accountId,
        Guid? relatedAccountId,
        TransactionType type,
        Money amount,
        string description,
        string? reference = null)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        RelatedAccountId = relatedAccountId;
        Type = type;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        CreatedAt = DateTime.UtcNow;
        Status = TransactionStatus.Completed;
        Reference = reference ?? GenerateReference();
    }

    public void MarkAsFailed()
    {
        Status = TransactionStatus.Failed;
    }

    public void MarkAsCancelled()
    {
        Status = TransactionStatus.Cancelled;
    }

    public void MarkAsCompleted()
    {
        Status = TransactionStatus.Completed;
    }

    private string GenerateReference()
    {
        return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
}