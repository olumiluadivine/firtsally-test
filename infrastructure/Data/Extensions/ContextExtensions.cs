using Microsoft.EntityFrameworkCore;

namespace infrastructure.Data.Extensions;

public static class ContextExtensions
{
    public static void UpdateTimestamps(this DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var updatedAtProperty = entityType.GetProperty("UpdatedAt");

            if (updatedAtProperty != null)
            {
                updatedAtProperty.SetValue(entry.Entity, DateTime.UtcNow);
            }
        }
    }
}