using System.Globalization;

// Parses typed filter values and compares them with item values of Date and Number columns.
internal static class FancyFilterValues
{
    private const string DateFormat = "yyyy-MM-dd";

    internal static bool TryParseDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    internal static bool TryParseNumber(string? value, out decimal number) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

    // Whether a filter value can be read for the column type; Text accepts any value.
    internal static bool IsValidValue(FancyColumnFilterType type, string? value) => type switch
    {
        FancyColumnFilterType.Date => TryParseDate(value, out _),
        FancyColumnFilterType.Number => TryParseNumber(value, out _),
        _ => true
    };

    // Matches the raw item value of a Date or Number column; null and unsupported values count as empty.
    internal static bool Matches(FancyColumnFilterType type, object? itemValue, FancyColumnFilter filter) => type switch
    {
        FancyColumnFilterType.Date => Matches(ToDate(itemValue), filter, TryParseDate),
        FancyColumnFilterType.Number => Matches(ToNumber(itemValue), filter, TryParseNumber),
        _ => false
    };

    private delegate bool TryParse<T>(string? value, out T result);

    private static bool Matches<T>(T? item, FancyColumnFilter filter, TryParse<T> tryParse)
        where T : struct, IComparable<T>
    {
        switch (filter.Operator)
        {
            case FancyColumnFilterOperator.IsEmpty:
                return item is null;
            case FancyColumnFilterOperator.IsNotEmpty:
                return item is not null;
        }

        if (!tryParse(filter.Value, out T value))
        {
            return false;
        }

        if (item is not T actual)
        {
            return filter.Operator is FancyColumnFilterOperator.NotEquals;
        }

        int comparison = actual.CompareTo(value);
        switch (filter.Operator)
        {
            case FancyColumnFilterOperator.Equals:
                return comparison == 0;
            case FancyColumnFilterOperator.NotEquals:
                return comparison != 0;
            case FancyColumnFilterOperator.GreaterThan:
                return comparison > 0;
            case FancyColumnFilterOperator.GreaterThanOrEqual:
                return comparison >= 0;
            case FancyColumnFilterOperator.LessThan:
                return comparison < 0;
            case FancyColumnFilterOperator.LessThanOrEqual:
                return comparison <= 0;
            case FancyColumnFilterOperator.Between:
                if (!tryParse(filter.ValueTo, out T valueTo))
                {
                    return false;
                }

                (T low, T high) = value.CompareTo(valueTo) <= 0 ? (value, valueTo) : (valueTo, value);
                return actual.CompareTo(low) >= 0 && actual.CompareTo(high) <= 0;
            default:
                return false;
        }
    }

    private static DateOnly? ToDate(object? value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        DateTimeOffset dateTimeOffset => DateOnly.FromDateTime(dateTimeOffset.DateTime),
        _ => null
    };

    private static decimal? ToNumber(object? value) => value switch
    {
        sbyte number => number,
        byte number => number,
        short number => number,
        ushort number => number,
        int number => number,
        uint number => number,
        long number => number,
        ulong number => number,
        decimal number => number,
        float number => FromBinary(number),
        double number => FromBinary(number),
        _ => null
    };

    // Converts a binary floating-point value; NaN, infinities and values outside the decimal range count as empty.
    private static decimal? FromBinary(double number)
    {
        // (double)decimal.MaxValue rounds up past decimal.MaxValue, so the bounds themselves are excluded.
        if (!double.IsFinite(number) || number <= (double)decimal.MinValue || number >= (double)decimal.MaxValue)
        {
            return null;
        }

        return (decimal)number;
    }
}
