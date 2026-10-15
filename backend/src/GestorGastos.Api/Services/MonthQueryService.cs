using GestorGastos.Api.Contracts;
using System.Globalization;
using GestorGastos.Api.Data;
using GestorGastos.Domain.Categories;
using GestorGastos.Domain.DueStatus;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Periods;
using GestorGastos.Domain.Summary;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Services;

/// <summary>
/// Gastos de un mes con su estado de vencimiento, totales y resumen por categoría (RF-23 a RF-26,
/// RF-33, RF-34), en pocas consultas indexadas; los totales se calculan en memoria (R-14).
/// </summary>
public sealed class MonthQueryService(AppDbContext db, IBusinessCalendar calendar)
{
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
        var summary = MonthSummary.Calculate(effective);

        return new MonthViewDto(
            period.ToString(),
            today,
            [.. effective.Select(e => ToDto(e, categories, today))],
            FormatAmount(summary.Total),
            FormatAmount(summary.PendingTotal),
            ToCategoryTotals(summary, categories));
    }

    /// <summary>Categorías por nombre y "Sin categoría" al final.</summary>
    private static List<CategoryTotalDto> ToCategoryTotals(MonthSummary summary, Dictionary<Guid, string> categories) =>
        [.. summary.ByCategory
            .Select(c => new CategoryTotalDto(
                c.CategoryId,
                c.CategoryId is { } id ? categories.GetValueOrDefault(id, CategoryName.Uncategorized) : CategoryName.Uncategorized,
                FormatAmount(c.Total)))
            .OrderBy(c => c.CategoryId is null)
            .ThenBy(c => c.CategoryName, StringComparer.CurrentCultureIgnoreCase)];

    private static string FormatAmount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

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
