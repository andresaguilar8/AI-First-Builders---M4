using GestorGastos.Domain.Common;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>Valores editables de un gasto, validados contra el mes del gasto (RF-01, RF-05).</summary>
public sealed record ExpenseValues
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private const string DueDateBeforePeriod = "El vencimiento no puede ser anterior al mes del gasto.";

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required Amount Amount { get; init; }

    public Guid? CategoryId { get; init; }

    public DateOnly? DueDate { get; init; }

    /// <summary>
    /// Recorta los textos y valida todos los campos a la vez; si algo no cumple, lanza
    /// <see cref="DomainValidationException"/> con los errores de cada campo.
    /// </summary>
    public static ExpenseValues Create(
        string? name,
        string? description,
        Amount? amount,
        Guid? categoryId,
        DateOnly? dueDate,
        Period period)
    {
        var errors = new Dictionary<string, string[]>();
        var trimmedName = name?.Trim();
        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (string.IsNullOrEmpty(trimmedName))
            errors["name"] = ["El nombre es obligatorio."];
        else if (trimmedName.Length > NameMaxLength)
            errors["name"] = [$"El nombre admite hasta {NameMaxLength} caracteres."];

        if (trimmedDescription?.Length > DescriptionMaxLength)
            errors["description"] = [$"La descripción admite hasta {DescriptionMaxLength} caracteres."];

        if (amount is null)
            errors["amount"] = ["El monto es obligatorio."];

        if (dueDate < period.FirstDay)
            errors["dueDate"] = [DueDateBeforePeriod];

        if (errors.Count > 0)
            throw new DomainValidationException(errors);

        return new ExpenseValues
        {
            Name = trimmedName!,
            Description = trimmedDescription,
            Amount = amount!.Value,
            CategoryId = categoryId,
            DueDate = dueDate,
        };
    }

    /// <summary>El vencimiento debe caer en el mes del gasto o en uno posterior (RF-05).</summary>
    internal static void EnsureDueDateIn(DateOnly? dueDate, Period period)
    {
        if (dueDate < period.FirstDay)
            throw new DomainValidationException("dueDate", DueDateBeforePeriod);
    }
}
