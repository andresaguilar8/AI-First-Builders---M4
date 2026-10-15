using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>Gasto puntual: pertenece a un único mes y no se repite (RF-08).</summary>
public sealed class OneOffExpense : Expense
{
    // Para EF Core.
    private OneOffExpense()
    {
        Name = null!;
    }

    public OneOffExpense(Guid id, DateTimeOffset createdAt, Period period, ExpenseValues values)
        : base(id, createdAt)
    {
        Period = period;
        Name = values.Name;
        Description = values.Description;
        Amount = values.Amount;
        CategoryId = values.CategoryId;
        DueDate = values.DueDate;
    }

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

    /// <summary>Reemplaza los valores; el pago se conserva (RF-05, RF-09).</summary>
    public void Update(ExpenseValues values)
    {
        ExpenseValues.EnsureDueDateIn(values.DueDate, Period);

        Name = values.Name;
        Description = values.Description;
        Amount = values.Amount;
        CategoryId = values.CategoryId;
        DueDate = values.DueDate;
    }

    internal void SetPaidOn(DateOnly? paidOn) => PaidOn = paidOn;
}
