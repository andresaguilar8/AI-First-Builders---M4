using System.Globalization;

namespace GestorGastos.Domain.Periods;

/// <summary>Un mes de un año: la unidad de consulta, alta, resumen y exportación.</summary>
public readonly record struct Period : IComparable<Period>
{
    public Period(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    public DateOnly FirstDay => new(Year, Month, 1);

    public DateOnly LastDay => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    /// <summary>Lee un período en formato <c>yyyy-MM</c>.</summary>
    public static Period Parse(string value) =>
        TryParse(value, out var period)
            ? period
            : throw new FormatException($"'{value}' no es un período válido con formato yyyy-MM.");

    public static bool TryParse(string? value, out Period period)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            period = FromDate(date);
            return true;
        }

        period = default;
        return false;
    }

    public static Period FromDate(DateOnly date) => new(date.Year, date.Month);

    public Period Next() => FromDate(FirstDay.AddMonths(1));

    public Period Previous() => FromDate(FirstDay.AddMonths(-1));

    /// <summary>El día indicado del mes o, si el mes es más corto, su último día (RF-07).</summary>
    public DateOnly DayOrLast(int day) => new(Year, Month, Math.Min(day, LastDay.Day));

    public bool Contains(DateOnly date) => date.Year == Year && date.Month == Month;

    public int CompareTo(Period other) => (Year, Month).CompareTo((other.Year, other.Month));

    public override string ToString() => FirstDay.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public static bool operator <(Period left, Period right) => left.CompareTo(right) < 0;

    public static bool operator >(Period left, Period right) => left.CompareTo(right) > 0;

    public static bool operator <=(Period left, Period right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Period left, Period right) => left.CompareTo(right) >= 0;
}
