using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Alerts;

/// <summary>
/// Gastos pendientes de cualquier mes cuyo vencimiento efectivo es hoy; los vencidos y los pagados
/// no cuentan (RF-32).
/// </summary>
public static class DueTodayAlert
{
    /// <summary>
    /// Cuenta puntuales y registros mensuales con vencimiento hoy, más las proyecciones del mes
    /// actual: un vencimiento proyectado siempre cae en su propio mes (R-14).
    /// </summary>
    public static int Count(IEnumerable<Expense> expenses, DateOnly today)
    {
        var currentPeriod = Period.FromDate(today);

        return expenses.Sum(expense => expense switch
        {
            OneOffExpense oneOff => IsDueToday(oneOff.Values, oneOff.PaidOn, today) ? 1 : 0,
            RecurringExpense recurring => CountRecurring(recurring, currentPeriod, today),
            _ => 0,
        });
    }

    private static int CountRecurring(RecurringExpense expense, Period currentPeriod, DateOnly today)
    {
        var instances = expense.Instances.Count(i =>
            expense.IsActiveIn(i.Period) && IsDueToday(i.Values, i.PaidOn, today));

        var projected = expense.IsActiveIn(currentPeriod)
            && expense.InstanceIn(currentPeriod) is null
            && expense.VersionIn(currentPeriod).ProjectTo(currentPeriod).DueDate == today;

        return instances + (projected ? 1 : 0);
    }

    private static bool IsDueToday(ExpenseValues values, DateOnly? paidOn, DateOnly today) =>
        paidOn is null && values.DueDate == today;
}
