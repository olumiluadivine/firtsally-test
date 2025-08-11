using Microsoft.EntityFrameworkCore;

namespace infrastructure.Data.Configurations;

public static class DatabaseExtensionsConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        // Enable basic UUID extension only
        modelBuilder.HasPostgresExtension("pgcrypto");
    }
}