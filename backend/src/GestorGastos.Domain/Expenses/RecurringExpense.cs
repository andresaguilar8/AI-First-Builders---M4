using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>
/// Cabecera de un gasto recurrente: visible desde <see cref="StartPeriod"/> hasta
/// <see cref="EndPeriod"/> (o sin fin). Sus valores viven en <see cref="Versions"/> y la
/// información propia de cada mes en <see cref="Instances"/> (R-01).
/// </summary>
public sealed class RecurringExpense : Expense
{
    private readonly List<RecurringVersion> _versions = [];
    private readonly List<RecurringInstance> _instances = [];

    // Para EF Core.
    private RecurringExpense()
    {
    }

    public RecurringExpense(Guid id, DateTimeOffset createdAt, Period startPeriod)
        : base(id, createdAt)
    {
        StartPeriod = startPeriod;
    }

    public Period StartPeriod { get; private set; }

    /// <summary>Último mes visible; <c>null</c> = activo.</summary>
    public Period? EndPeriod { get; private set; }

    public IReadOnlyList<RecurringVersion> Versions => _versions;

    public IReadOnlyList<RecurringInstance> Instances => _instances;
}
