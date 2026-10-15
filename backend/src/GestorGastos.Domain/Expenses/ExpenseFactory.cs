using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>Alta de gastos en el mes visualizado (RF-01, RF-02, RF-04, RF-05).</summary>
public static class ExpenseFactory
{
    public static OneOffExpense CreateOneOff(Period period, ExpenseValues values, DateTimeOffset createdAt) =>
        new(Guid.CreateVersion7(createdAt), createdAt, period, values);

    /// <summary>
    /// Cabecera desde el mes más una versión con sus valores. Si el vencimiento cae fuera del mes, la
    /// proyección no lo reproduce y la fecha exacta se guarda en el registro de ese mes (R-03, AC-07).
    /// </summary>
    public static RecurringExpense CreateRecurring(Period period, ExpenseValues values, DateTimeOffset createdAt)
    {
        var expense = new RecurringExpense(Guid.CreateVersion7(createdAt), createdAt, period);
        var version = RecurringVersion.From(period, values);
        expense.AddVersion(version);

        if (values != version.ProjectTo(period))
            expense.AddInstance(new RecurringInstance(Guid.CreateVersion7(), period, values, paidOn: null));

        return expense;
    }
}
