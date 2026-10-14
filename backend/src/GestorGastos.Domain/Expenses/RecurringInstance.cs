using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>
/// Registro mensual de un recurrente: snapshot completo de un mes con información propia
/// (un cambio "Solo este mes" o un pago). Existe como máximo uno por mes (RNF-06).
/// </summary>
public sealed class RecurringInstance
{
    // Para EF Core.
    private RecurringInstance()
    {
        Name = null!;
    }

    public RecurringInstance(Guid id, Period period, ExpenseValues values, DateOnly? paidOn)
    {
        Id = id;
        Period = period;
        Name = values.Name;
        Description = values.Description;
        Amount = values.Amount;
        CategoryId = values.CategoryId;
        DueDate = values.DueDate;
        PaidOn = paidOn;
    }

    public Guid Id { get; private set; }

    public Period Period { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Amount Amount { get; private set; }

    public Guid? CategoryId { get; private set; }

    public DateOnly? DueDate { get; private set; }

    /// <summary>Fecha de pago; <c>null</c> = pendiente.</summary>
    public DateOnly? PaidOn { get; private set; }

    public ExpenseValues Values => new()
    {
        Name = Name,
        Description = Description,
        Amount = Amount,
        CategoryId = CategoryId,
        DueDate = DueDate,
    };
}
