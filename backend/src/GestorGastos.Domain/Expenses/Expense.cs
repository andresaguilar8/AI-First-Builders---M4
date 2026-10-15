namespace GestorGastos.Domain.Expenses;

/// <summary>Obligación de pago: un gasto puntual o un gasto recurrente.</summary>
public abstract class Expense
{
    // Para EF Core.
    protected Expense()
    {
    }

    protected Expense(Guid id, DateTimeOffset createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
