using domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace infrastructure.Data.Configurations;

public static class ValueObjectConfiguration
{
    public static ValueConverter<Money, string> CreateMoneyConverter()
    {
        return new ValueConverter<Money, string>(
            v => v.Amount + "|" + v.Currency,
            v => CreateMoneyFromString(v));
    }

    private static Money CreateMoneyFromString(string value)
    {
        var parts = value.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException($"Invalid money format: {value}");

        if (!decimal.TryParse(parts[0], out var amount))
            throw new InvalidOperationException($"Invalid amount format: {parts[0]}");

        return new Money(amount, parts[1]);
    }
}