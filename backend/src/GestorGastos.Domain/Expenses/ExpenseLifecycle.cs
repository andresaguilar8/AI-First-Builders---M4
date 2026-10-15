using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

public enum DiscontinueOutcome
{
    /// <summary>El recurrente termina en el mes anterior y conserva sus meses previos.</summary>
    Ended,

    /// <summary>El recurrente no tiene meses visibles: se elimina por completo (R-03).</summary>
    Removed,
}

/// <param name="Created">Gasto nuevo en el mes visualizado.</param>
/// <param name="RemoveOriginal">Si el gasto original debe eliminarse.</param>
public sealed record ConversionResult(Expense Created, bool RemoveOriginal);

/// <summary>Baja de recurrentes y cambio de tipo puntual ↔ recurrente desde el mes visualizado (RF-12 a RF-15).</summary>
public static class ExpenseLifecycle
{
    /// <summary>
    /// El recurrente deja de aparecer desde el mes, aunque esos meses estuvieran pagados, y se borran
    /// sus registros desde ese mes; los meses anteriores no cambian (RF-14, RF-15).
    /// </summary>
    public static DiscontinueOutcome Discontinue(RecurringExpense expense, Period period)
    {
        expense.EnsureActiveIn(period);

        foreach (var instance in expense.Instances.Where(i => i.Period >= period).ToList())
            expense.RemoveInstance(instance);

        if (period == expense.StartPeriod)
            return DiscontinueOutcome.Removed;

        expense.EndAt(period.Previous());
        return DiscontinueOutcome.Ended;
    }

    /// <summary>
    /// En el mes queda un puntual nuevo con los valores y el pago que el recurrente tenía en él, y el
    /// recurrente se da de baja desde ese mes (RF-13).
    /// </summary>
    public static ConversionResult ToOneOff(RecurringExpense expense, Period period, DateTimeOffset createdAt)
    {
        expense.EnsureActiveIn(period);
        var effective = MonthProjector.EffectiveIn(expense, period)!;

        var oneOff = ExpenseFactory.CreateOneOff(period, effective.Values, createdAt);
        oneOff.SetPaidOn(effective.PaidOn);

        return new ConversionResult(oneOff, Discontinue(expense, period) == DiscontinueOutcome.Removed);
    }

    /// <summary>
    /// Un recurrente desde el mes con los valores del puntual como vigentes; el pago del puntual queda
    /// en el registro de ese mes. El puntual se elimina (RF-12).
    /// </summary>
    public static RecurringExpense ToRecurring(OneOffExpense expense, Period period, DateTimeOffset createdAt)
    {
        if (expense.Period != period)
            throw new InvalidOperationException($"El gasto {expense.Id} no aparece en {period}.");

        var recurring = ExpenseFactory.CreateRecurring(period, expense.Values, createdAt);
        if (expense.PaidOn is { } paidOn)
        {
            if (recurring.InstanceIn(period) is { } instance)
                instance.SetPaidOn(paidOn);
            else
                recurring.AddInstance(new RecurringInstance(Guid.CreateVersion7(), period, expense.Values, paidOn));
        }

        return recurring;
    }
}
