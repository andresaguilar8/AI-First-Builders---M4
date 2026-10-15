using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>Valores vigentes de un recurrente desde <see cref="EffectiveFrom"/> en adelante.</summary>
public sealed class RecurringVersion
{
    // Para EF Core.
    private RecurringVersion()
    {
        Name = null!;
    }

    public RecurringVersion(
        Guid id,
        Period effectiveFrom,
        string name,
        string? description,
        Amount amount,
        Guid? categoryId,
        int? dueDay)
    {
        if (dueDay is < 1 or > 31)
            throw new ArgumentOutOfRangeException(nameof(dueDay), dueDay, "El día de vencimiento va de 1 a 31.");

        Id = id;
        EffectiveFrom = effectiveFrom;
        Name = name;
        Description = description;
        Amount = amount;
        CategoryId = categoryId;
        DueDay = dueDay;
    }

    public Guid Id { get; private set; }

    public Period EffectiveFrom { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Amount Amount { get; private set; }

    public Guid? CategoryId { get; private set; }

    /// <summary>Día del mes en que vence (1–31); <c>null</c> = sin vencimiento (RF-07).</summary>
    public int? DueDay { get; private set; }

    /// <summary>Versión con los valores dados, vigente desde el mes; el vencimiento se guarda como día del mes.</summary>
    public static RecurringVersion From(Period effectiveFrom, ExpenseValues values) => new(
        Guid.CreateVersion7(),
        effectiveFrom,
        values.Name,
        values.Description,
        values.Amount,
        values.CategoryId,
        values.DueDate?.Day);

    /// <summary>
    /// Valores proyectados en un mes: el vencimiento cae en el día <see cref="DueDay"/> o en el último
    /// día del mes si este es más corto (RF-07).
    /// </summary>
    public ExpenseValues ProjectTo(Period period) => new()
    {
        Name = Name,
        Description = Description,
        Amount = Amount,
        CategoryId = CategoryId,
        DueDate = DueDay is { } day ? period.DayOrLast(day) : null,
    };

    /// <summary>Copia de esta versión vigente desde otro mes.</summary>
    internal RecurringVersion CopyFrom(Period effectiveFrom) =>
        new(Guid.CreateVersion7(), effectiveFrom, Name, Description, Amount, CategoryId, DueDay);

    /// <summary>Aplica solo los campos que cambiaron; el vencimiento se guarda como día del mes.</summary>
    internal void Apply(ExpenseChanges changes)
    {
        var after = changes.After;
        if (changes.Name)
            Name = after.Name;
        if (changes.Description)
            Description = after.Description;
        if (changes.Amount)
            Amount = after.Amount;
        if (changes.CategoryId)
            CategoryId = after.CategoryId;
        if (changes.DueDate)
            DueDay = after.DueDate?.Day;
    }

    internal bool HasSameValuesAs(RecurringVersion other) =>
        Name == other.Name
        && Description == other.Description
        && Amount == other.Amount
        && CategoryId == other.CategoryId
        && DueDay == other.DueDay;
}
