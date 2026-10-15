using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>
/// Calcula los gastos de un mes sin persistir nada: puntuales del mes, registros propios de los
/// recurrentes y, si no los hay, la proyección de la versión vigente (RNF-05, data-model.md).
/// </summary>
public static class MonthProjector
{
    /// <summary>
    /// Gastos del mes ordenados por vencimiento (los que no tienen, al final) y luego por nombre.
    /// </summary>
    public static IReadOnlyList<EffectiveExpense> Project(Period period, IEnumerable<Expense> expenses) =>
        expenses
            .Select(e => EffectiveIn(e, period))
            .OfType<EffectiveExpense>()
            .OrderBy(e => e.Values.DueDate is null)
            .ThenBy(e => e.Values.DueDate)
            .ThenBy(e => e.Values.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>Valores del gasto en el mes, o <c>null</c> si no aparece en él.</summary>
    public static EffectiveExpense? EffectiveIn(Expense expense, Period period) => expense switch
    {
        OneOffExpense oneOff when oneOff.Period == period =>
            new EffectiveExpense(oneOff.Id, ExpenseKind.OneOff, period, oneOff.Values, oneOff.PaidOn),
        RecurringExpense recurring when recurring.IsActiveIn(period) => recurring.InstanceIn(period) is { } instance
            ? new EffectiveExpense(recurring.Id, ExpenseKind.Recurring, period, instance.Values, instance.PaidOn)
            : new EffectiveExpense(recurring.Id, ExpenseKind.Recurring, period, recurring.VersionIn(period).ProjectTo(period), null),
        _ => null,
    };
}
