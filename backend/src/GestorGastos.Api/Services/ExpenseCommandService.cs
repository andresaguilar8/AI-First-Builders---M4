using GestorGastos.Api.Contracts;
using GestorGastos.Api.Data;
using GestorGastos.Domain.Common;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Payments;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Services;

/// <summary>Operaciones que modifican gastos en el mes visualizado.</summary>
public sealed class ExpenseCommandService(AppDbContext db, TimeProvider time, IBusinessCalendar calendar)
{
    /// <summary>Crea un gasto puntual o recurrente en el mes (RF-01 a RF-05).</summary>
    public async Task<EffectiveExpense> CreateAsync(Period period, CreateExpenseRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var values = await ToValuesAsync(period, request, errors, ct);
        if (request.Kind is null)
            errors["kind"] = ["El tipo es obligatorio."];
        ThrowIfAny(errors);

        var now = time.GetUtcNow();
        Expense expense = request.Kind == ExpenseKind.Recurring
            ? ExpenseFactory.CreateRecurring(period, values!, now)
            : ExpenseFactory.CreateOneOff(period, values!, now);

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);

        return MonthProjector.EffectiveIn(expense, period)!;
    }

    /// <summary>
    /// Edita un gasto que aparece en el mes; conserva su pago (RF-09). Un recurrente exige el
    /// alcance (RF-10). Devuelve <c>null</c> si el gasto no aparece en ese mes.
    /// </summary>
    public async Task<EffectiveExpense?> UpdateAsync(
        Period period,
        Guid expenseId,
        UpdateExpenseRequest request,
        CancellationToken ct)
    {
        var expense = await FindInMonthAsync(period, expenseId, ct);
        if (expense is null)
            return null;

        var errors = new Dictionary<string, string[]>();
        var values = await ToValuesAsync(period, request, errors, ct);
        if (expense is RecurringExpense && request.Scope is null)
            errors["scope"] = ["Indicá si el cambio es solo para este mes o también para los siguientes."];
        ThrowIfAny(errors);

        switch (expense)
        {
            case OneOffExpense oneOff:
                oneOff.Update(values!);
                break;
            case RecurringExpense recurring when request.Scope == EditScope.ThisMonth:
                RecurringEditor.EditThisMonth(recurring, period, values!);
                break;
            case RecurringExpense recurring:
                RecurringEditor.EditThisAndFollowing(recurring, period, values!);
                break;
        }

        await db.SaveChangesAsync(ct);

        return MonthProjector.EffectiveIn(expense, period);
    }

    /// <summary>Marca el gasto como pagado en el mes (RF-27 a RF-30); <c>null</c> si no aparece en él.</summary>
    public async Task<EffectiveExpense?> MarkPaidAsync(Period period, Guid expenseId, DateOnly? paidOn, CancellationToken ct)
    {
        var expense = await FindInMonthAsync(period, expenseId, ct);
        if (expense is null)
            return null;

        PaymentService.MarkPaid(expense, period, calendar.Today, paidOn);
        await db.SaveChangesAsync(ct);

        return MonthProjector.EffectiveIn(expense, period);
    }

    /// <summary>Desmarca el pago del mes (RF-31); <c>null</c> si el gasto no aparece en él.</summary>
    public async Task<EffectiveExpense?> UnmarkPaidAsync(Period period, Guid expenseId, CancellationToken ct)
    {
        var expense = await FindInMonthAsync(period, expenseId, ct);
        if (expense is null)
            return null;

        PaymentService.Unmark(expense, period);
        await db.SaveChangesAsync(ct);

        return MonthProjector.EffectiveIn(expense, period);
    }

    /// <summary>
    /// El gasto con sus versiones y registros completos, solo si aparece en el mes: únicamente se
    /// opera sobre gastos del mes visualizado (RF-09).
    /// </summary>
    private async Task<Expense?> FindInMonthAsync(Period period, Guid expenseId, CancellationToken ct)
    {
        var expense = await db.Expenses.FindAsync([expenseId], ct);
        if (expense is RecurringExpense recurring)
        {
            await db.Entry(recurring).Collection(e => e.Versions).LoadAsync(ct);
            await db.Entry(recurring).Collection(e => e.Instances).LoadAsync(ct);
        }

        return expense is not null && MonthProjector.EffectiveIn(expense, period) is not null ? expense : null;
    }

    /// <summary>Valida los valores y que la categoría exista; acumula los errores por campo.</summary>
    private async Task<ExpenseValues?> ToValuesAsync(
        Period period,
        ExpenseValuesRequest request,
        Dictionary<string, string[]> errors,
        CancellationToken ct)
    {
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

        if (request.CategoryId is { } categoryId && !await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            errors["categoryId"] = ["La categoría no existe."];

        return values;
    }

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
            throw new DomainValidationException(errors);
    }
}
