using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GestorGastos.Domain.Common;

namespace GestorGastos.Domain.Categories;

/// <summary>
/// Nombre de categoría: obligatorio, hasta 50 caracteres, sin espacios al inicio ni al final (RF-18).
/// <see cref="Normalized"/> lo compara sin mayúsculas, acentos ni espacios repetidos (RF-19, R-09).
/// </summary>
public sealed partial record CategoryName
{
    /// <summary>"Sin categoría" es la presentación de los gastos sin categoría, no un nombre válido (RF-24).</summary>
    public const string Uncategorized = "Sin categoría";

    private static readonly string ReservedNormalized = Normalize(Uncategorized);

    private CategoryName(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    public string Value { get; }

    public string Normalized { get; }

    public bool IsReserved => Normalized == ReservedNormalized;

    public static CategoryName Create(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainValidationException("name", "El nombre es obligatorio.");
        if (trimmed.Length > Category.NameMaxLength)
            throw new DomainValidationException("name", $"El nombre admite hasta {Category.NameMaxLength} caracteres.");

        return new CategoryName(trimmed, Normalize(trimmed));
    }

    private static string Normalize(string name)
    {
        var withoutMarks = new StringBuilder(name.Length);
        foreach (var c in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                withoutMarks.Append(c);
        }

        var collapsed = RepeatedSpaces().Replace(withoutMarks.ToString().Normalize(NormalizationForm.FormC), " ");
        return collapsed.Trim().ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedSpaces();
}
