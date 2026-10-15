using GestorGastos.Api.Contracts;
using GestorGastos.Api.Data;
using GestorGastos.Domain.Common;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Payments;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Services;

/// <summary>Resultado de una operación sobre un gasto del mes.</summary>
public enum CommandOutcome
{
    Done,

    /// <summary>El gasto no existe o no aparece en el mes (404).</summary>
    NotFound,

    /// <summary>La operación no corresponde al tipo del gasto (409).</summary>
    WrongKind,
}

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

    /// <summary>Elimina definitivamente un puntual, pendiente o pagado; un recurrente se da de baja (RF-16).</summary>
    public async Task<CommandOutcome> DeleteAsync(Period period, Guid expenseId, CancellationToken ct)
    {
        var expense = await FindInMonthAsync(period, expenseId, ct);
        if (expense is not OneOffExpense)
            return expense is null ? CommandOutcome.NotFound : CommandOutcome.WrongKind;

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return CommandOutcome.Done;
    }

    /// <summary>Da de baja un recurrente desde el mes (RF-14, RF-15).</summary>
    public async Task<CommandOutcome> DiscontinueAsync(Period period, Guid expenseId, CancellationToken ct)
    {
        var expense = await FindInMonthAsync(period, expenseId, ct);
        if (expense is not RecurringExpense recurring)
            return expense is null ? CommandOutcome.NotFound : CommandOutcome.WrongKind;

        if (ExpenseLifecycle.Discontinue(recurring, period) == DiscontinueOutcome.Removed)
            db.Expenses.Remove(recurring);
        await db.SaveChangesAsync(ct);
        return CommandOutcome.Done;
    }

    /// <summary>Cambia el tipo del gasto desde el mes; el gasto resultante tiene otro id (RF-12, RF-13).</summary>
    public async Task<(CommandOutcome Outcome, EffectiveExpense? Expense)> ConvertAsync(
        Period period,
        Guid expenseId,
        ConvertExpenseRequest request,
        CancellationToken ct)
    {
        if (request.To is null)
            throw new DomainValidationException("to", "Indicá el tipo al que se cambia el gasto.");

        var expense = await FindInMonthAsync(period, expenseId, ct);
        if (expense is null)
            return (CommandOutcome.NotFound, null);

        Expense created;
        switch (expense)
        {
            case RecurringExpense recurring when request.To == ExpenseKind.OneOff:
                var result = ExpenseLifecycle.ToOneOff(recurring, period, time.GetUtcNow());
                if (result.RemoveOriginal)
                    db.Expenses.Remove(recurring);
                created = result.Created;
                break;
            case OneOffExpense oneOff when request.To == ExpenseKind.Recurring:
                created = ExpenseLifecycle.ToRecurring(oneOff, period, time.GetUtcNow());
                db.Expenses.Remove(oneOff);
                break;
            default:
                return (CommandOutcome.WrongKind, null);
        }

        db.Expenses.Add(created);
        await db.SaveChangesAsync(ct);
        return (CommandOutcome.Done, MonthProjector.EffectiveIn(created, period));
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
