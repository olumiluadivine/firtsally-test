using domain.Entities;
using domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Transaction entity
/// </summary>
public static class TransactionConfiguration
{
    public static void Configure(ModelBuilder modelBuilder, ValueConverter<Money, string> moneyConverter)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            // Table configuration
            entity.ToTable("Transactions");
            entity.HasKey(e => e.Id);

            // Foreign keys
            entity.Property(e => e.AccountId)
                .IsRequired();

            entity.Property(e => e.RelatedAccountId)
                .IsRequired(false);

            // Enum properties
            entity.Property(e => e.Type)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<int>()
                .IsRequired();

            // Value object
            entity.Property(e => e.Amount)
                .HasConversion(moneyConverter)
                .IsRequired();

            // String properties
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Reference)
                .HasMaxLength(50);

            // Date/Time properties
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Essential indexes only
            entity.HasIndex(e => e.AccountId);

            entity.HasIndex(e => e.RelatedAccountId);

            entity.HasIndex(e => e.Reference)
                .IsUnique();

            // Basic date index for transaction history
            entity.HasIndex(e => e.CreatedAt);

            // Composite index for account transactions
            entity.HasIndex(e => new { e.AccountId, e.CreatedAt });
        });
    }
}