using GestorGastos.Api.Contracts;
using GestorGastos.Api.Data;
using GestorGastos.Domain.Common;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Services;

/// <summary>Operaciones que modifican gastos en el mes visualizado.</summary>
public sealed class ExpenseCommandService(AppDbContext db, TimeProvider time)
{
    /// <summary>Crea un gasto puntual o recurrente en el mes (RF-01 a RF-05).</summary>
    public async Task<EffectiveExpense> CreateAsync(Period period, CreateExpenseRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        ExpenseValues? values = null;
        try
        {
            values = request.ToValues(period);
        }
        catch (DomainValidationException e)
        {
            foreach (var (field, messages) in e.Errors)
                errors[field] = messages;
        }

        if (request.Kind is null)
            errors["kind"] = ["El tipo es obligatorio."];
        if (request.CategoryId is { } categoryId && !await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            errors["categoryId"] = ["La categoría no existe."];
        if (errors.Count > 0)
            throw new DomainValidationException(errors);

        var now = time.GetUtcNow();
        Expense expense = request.Kind == ExpenseKind.Recurring
            ? ExpenseFactory.CreateRecurring(period, values!, now)
            : ExpenseFactory.CreateOneOff(period, values!, now);

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);

        return MonthProjector.EffectiveIn(expense, period)!;
    }
}
