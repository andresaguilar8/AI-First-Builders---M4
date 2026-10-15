using System.Globalization;
using System.Text.RegularExpressions;
using GestorGastos.Domain.Common;

namespace GestorGastos.Domain.Money;

/// <summary>Monto en pesos argentinos: mayor a 0, hasta <see cref="Max"/>, con hasta 2 decimales (RF-03).</summary>
public readonly partial record struct Amount
{
    public const decimal Max = 999_999_999_999.99m;

    private const string Field = "amount";

    private Amount(decimal value) => Value = value;

    public decimal Value { get; }

    public static Amount Create(decimal value)
    {
        if (value <= 0)
            throw new DomainValidationException(Field, "El monto debe ser mayor a 0.");
        if (value > Max)
            throw new DomainValidationException(Field, "El monto no puede superar $999.999.999.999,99.");
        if (decimal.Round(value, 2) != value)
            throw new DomainValidationException(Field, "El monto admite hasta 2 decimales.");

        return new Amount(value);
    }

    /// <summary>Lee el formato canónico de la API: punto decimal y sin separador de miles (<c>"1234.56"</c>).</summary>
    public static Amount Parse(string value)
    {
        if (!CanonicalFormat().IsMatch(value))
            throw new DomainValidationException(Field, "El monto debe tener el formato 1234.56.");

        return Create(decimal.Parse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
    }

    public override string ToString() => Value.ToString("0.00", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^\d{1,12}(\.\d{1,2})?$")]
    private static partial Regex CanonicalFormat();
}
