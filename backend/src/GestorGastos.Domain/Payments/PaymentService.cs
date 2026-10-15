using GestorGastos.Domain.Common;
using GestorGastos.Domain.DueStatus;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Payments;

/// <summary>Marcado y desmarcado de pago de un gasto en el mes visualizado (RF-27 a RF-31).</summary>
public static class PaymentService
{
    /// <summary>
    /// Si el gasto no está Vencido se registra hoy, aunque se indique otra fecha (RF-28). Si está
    /// Vencido se usa la fecha indicada o hoy, que no puede ser posterior a hoy (RF-29, RF-30). En un
    /// recurrente el pago queda en el registro de ese mes (RNF-06).
    /// </summary>
    public static void MarkPaid(Expense expense, Period period, DateOnly today, DateOnly? paidOn)
    {
        var effective = EffectiveIn(expense, period);
        var overdue = DueStatusCalculator.Calculate(effective.Values.DueDate, null, today) == ExpenseDueStatus.Vencido;
        var date = overdue ? paidOn ?? today : today;
        if (date > today)
            throw new DomainValidationException("paidOn", "La fecha de pago no puede ser posterior a hoy.");

        switch (expense)
        {
            case OneOffExpense oneOff:
                oneOff.SetPaidOn(date);
                break;
            case RecurringExpense recurring when recurring.InstanceIn(period) is { } instance:
                instance.SetPaidOn(date);
                break;
            case RecurringExpense recurring:
                recurring.AddInstance(new RecurringInstance(Guid.CreateVersion7(), period, effective.Values, date));
                break;
        }
    }

    /// <summary>
    /// Vuelve el gasto a pendiente. En un recurrente, si el pago era lo único propio del mes, el
    /// registro se elimina (RF-31, RNF-05).
    /// </summary>
    public static void Unmark(Expense expense, Period period)
    {
        EffectiveIn(expense, period);

        switch (expense)
        {
            case OneOffExpense oneOff:
                oneOff.SetPaidOn(null);
                break;
            case RecurringExpense recurring when recurring.InstanceIn(period) is { } instance:
                instance.SetPaidOn(null);
                InstanceNormalizer.Normalize(recurring, period);
                break;
        }
    }

    private static EffectiveExpense EffectiveIn(Expense expense, Period period) =>
        MonthProjector.EffectiveIn(expense, period)
        ?? throw new InvalidOperationException($"El gasto {expense.Id} no aparece en {period}.");
}
