using GestorGastos.Domain.Expenses;

namespace GestorGastos.Domain.Summary;

/// <summary>Total de una categoría en el mes; <see cref="CategoryId"/> nulo = "Sin categoría" (RF-24).</summary>
public sealed record CategoryTotal(Guid? CategoryId, decimal Total);

/// <summary>Totales del mes: por categoría (pagados y pendientes), del mes y pendiente (RF-23 a RF-26).</summary>
public sealed record MonthSummary(decimal Total, decimal PendingTotal, IReadOnlyList<CategoryTotal> ByCategory)
{
    public static MonthSummary Calculate(IReadOnlyCollection<EffectiveExpense> expenses) => new(
        expenses.Sum(e => e.Values.Amount.Value),
        expenses.Where(e => e.PaidOn is null).Sum(e => e.Values.Amount.Value),
        [.. expenses
            .GroupBy(e => e.Values.CategoryId)
            .Select(g => new CategoryTotal(g.Key, g.Sum(e => e.Values.Amount.Value)))]);
}
