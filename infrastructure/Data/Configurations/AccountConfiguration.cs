using domain.Entities;
using domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Account entity
/// </summary>
public static class AccountConfiguration
{
    public static void Configure(ModelBuilder modelBuilder, ValueConverter<Money, string> moneyConverter)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            // Table configuration
            entity.ToTable("Accounts");
            entity.HasKey(e => e.Id);

            // Foreign key
            entity.Property(e => e.UserId)
                .IsRequired();

            // String properties
            entity.Property(e => e.AccountNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.PINHash)
                .IsRequired()
                .HasMaxLength(500);

            // Value object
            entity.Property(e => e.Balance)
                .HasConversion(moneyConverter)
                .IsRequired();

            // Enum property
            entity.Property(e => e.AccountType)
                .HasConversion<int>()
                .IsRequired();

            // Date/Time properties
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Boolean properties
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Essential indexes only
            entity.HasIndex(e => e.AccountNumber)
                .IsUnique();

            entity.HasIndex(e => e.UserId);

            // Basic date index
            entity.HasIndex(e => e.CreatedAt);

            // Configure one-to-many relationship with transactions
            entity.HasMany(_ => _.Transactions)
                .WithOne()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}