using domain.Entities;
using domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for User entity
/// </summary>
public static class UserConfiguration
{
    public static void Configure(ModelBuilder modelBuilder, ValueConverter<Money, string> moneyConverter)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Table configuration
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);

            // String properties
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            // Date/Time properties
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.Property(e => e.LastLoginAt);

            // Boolean properties
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.IsEmailVerified)
                .IsRequired()
                .HasDefaultValue(false);

            // Essential indexes only
            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasIndex(e => e.PhoneNumber)
                .IsUnique();

            // Basic date index for sorting
            entity.HasIndex(e => e.CreatedAt);

            // Configure one-to-many relationship with accounts
            entity.HasMany(_ => _.Accounts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}