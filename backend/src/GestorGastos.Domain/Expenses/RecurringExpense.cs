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

    /// <summary>Indica si el gasto aparece en el mes: entre su inicio y su baja (inclusive).</summary>
    public bool IsActiveIn(Period period) => StartPeriod <= period && (EndPeriod is null || period <= EndPeriod);

    /// <summary>Versión vigente en el mes: la de mayor <c>EffectiveFrom</c> que no lo supere.</summary>
    public RecurringVersion VersionIn(Period period) =>
        _versions.Where(v => v.EffectiveFrom <= period).MaxBy(v => v.EffectiveFrom)
        ?? throw new InvalidOperationException($"El gasto {Id} no tiene una versión vigente en {period}.");

    /// <summary>Registro propio del mes, si lo tiene.</summary>
    public RecurringInstance? InstanceIn(Period period) => _instances.SingleOrDefault(i => i.Period == period);

    internal void AddVersion(RecurringVersion version) => _versions.Add(version);

    internal void AddInstance(RecurringInstance instance) => _instances.Add(instance);
}
