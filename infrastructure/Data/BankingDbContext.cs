using domain.Entities;
using infrastructure.Data.Configurations;
using infrastructure.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Data;

public class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure value objects
        var moneyConverter = ValueObjectConfiguration.CreateMoneyConverter();

        // Apply entity configurations
        UserConfiguration.Configure(modelBuilder, moneyConverter);
        AccountConfiguration.Configure(modelBuilder, moneyConverter);
        TransactionConfiguration.Configure(modelBuilder, moneyConverter);

        // Apply database extensions
        //DatabaseExtensionsConfiguration.Configure(modelBuilder);
    }

    // Override SaveChanges for automatic UpdatedAt timestamp
    public override int SaveChanges()
    {
        this.UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }
}