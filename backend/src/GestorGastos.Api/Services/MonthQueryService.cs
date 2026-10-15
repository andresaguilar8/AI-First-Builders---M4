using GestorGastos.Api.Contracts;
using GestorGastos.Api.Data;
using GestorGastos.Domain.DueStatus;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Services;

/// <summary>Gastos de un mes con su estado de vencimiento (RF-33, RF-34), en pocas consultas indexadas (R-14).</summary>
public sealed class MonthQueryService(AppDbContext db, IBusinessCalendar calendar)
{
    private const string Zero = "0.00";

    public async Task<MonthViewDto> GetAsync(Period period, CancellationToken ct)
    {
        var oneOffs = await db.OneOffExpenses.AsNoTracking()
            .Where(e => e.Period == period)
            .ToListAsync(ct);

        // Solo las versiones que pueden estar vigentes y el registro propio del mes.
        var recurring = await db.RecurringExpenses.AsNoTracking()
            .Where(e => e.StartPeriod <= period && (e.EndPeriod == null || e.EndPeriod >= period))
            .Include(e => e.Versions.Where(v => v.EffectiveFrom <= period))
            .Include(e => e.Instances.Where(i => i.Period == period))
            .AsSplitQuery()
            .ToListAsync(ct);

        var effective = MonthProjector.Project(period, [.. oneOffs, .. recurring]);
        var categories = await LoadCategoriesAsync(effective, ct);
        var today = calendar.Today;

        return new MonthViewDto(
            period.ToString(),
            today,
            [.. effective.Select(e => ToDto(e, categories, today))],
            Zero,
            Zero,
            []);
    }

    /// <summary>Un gasto del mes con su categoría y su estado, listo para la respuesta.</summary>
    public async Task<MonthExpenseDto> ToDtoAsync(EffectiveExpense expense, CancellationToken ct) =>
        ToDto(expense, await LoadCategoriesAsync([expense], ct), calendar.Today);

    private async Task<Dictionary<Guid, string>> LoadCategoriesAsync(
        IReadOnlyCollection<EffectiveExpense> expenses,
        CancellationToken ct)
    {
        var ids = expenses.Select(e => e.Values.CategoryId).OfType<Guid>().Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await db.Categories.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
    }

    private static MonthExpenseDto ToDto(EffectiveExpense expense, Dictionary<Guid, string> categories, DateOnly today)
    {
        var values = expense.Values;
        var category = values.CategoryId is { } id && categories.TryGetValue(id, out var name)
            ? new CategoryRef(id, name)
            : null;

        return new MonthExpenseDto(
            expense.ExpenseId,
            expense.Kind,
            values.Name,
            values.Description,
            values.Amount.ToString(),
            category,
            values.DueDate,
            expense.PaidOn,
            DueStatusCalculator.Calculate(values.DueDate, expense.PaidOn, today));
    }
}
