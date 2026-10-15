using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>
/// Edición de un recurrente desde el mes visualizado, con alcance "Solo este mes" o "Este mes y los
/// siguientes". Ninguna de las dos toca los meses anteriores (RF-10, RF-11, RF-17, R-04).
/// </summary>
public static class RecurringEditor
{
    /// <summary>Guarda los valores en el registro del mes; conserva el pago (RF-09, RF-10).</summary>
    public static void EditThisMonth(RecurringExpense expense, Period period, ExpenseValues values)
    {
        expense.EnsureActiveIn(period);
        ExpenseValues.EnsureDueDateIn(values.DueDate, period);

        SetMonthValues(expense, period, values);
        InstanceNormalizer.Normalize(expense, period);
    }

    /// <summary>
    /// Aplica los campos que cambiaron respecto de los valores efectivos del mes a la versión vigente
    /// desde ese mes (creándola si hace falta), a las versiones posteriores y a los registros de los
    /// meses siguientes; conserva los pagos y las demás modificaciones propias (RF-11).
    /// </summary>
    public static void EditThisAndFollowing(RecurringExpense expense, Period period, ExpenseValues values)
    {
        expense.EnsureActiveIn(period);
        ExpenseValues.EnsureDueDateIn(values.DueDate, period);

        var changes = new ExpenseChanges(MonthProjector.EffectiveIn(expense, period)!.Values, values);
        if (!changes.Any)
            return;

        var current = expense.VersionIn(period);
        if (current.EffectiveFrom != period)
            expense.AddVersion(current.CopyFrom(period));

        foreach (var version in expense.Versions.Where(v => v.EffectiveFrom >= period))
            version.Apply(changes);
        foreach (var instance in expense.Instances.Where(i => i.Period > period))
            instance.Apply(changes);

        // En el mes visualizado vale la fecha exacta elegida, aunque caiga en un mes posterior.
        SetMonthValues(expense, period, values);
        MergeIdenticalVersions(expense);
        InstanceNormalizer.Normalize(expense, period);
    }

    private static void SetMonthValues(RecurringExpense expense, Period period, ExpenseValues values)
    {
        if (expense.InstanceIn(period) is { } instance)
            instance.Update(values);
        else
            expense.AddInstance(new RecurringInstance(Guid.CreateVersion7(), period, values, paidOn: null));
    }

    /// <summary>Una versión igual a la anterior no aporta nada: se elimina (las proyecciones no cambian).</summary>
    private static void MergeIdenticalVersions(RecurringExpense expense)
    {
        RecurringVersion? previous = null;
        foreach (var version in expense.Versions.OrderBy(v => v.EffectiveFrom).ToList())
        {
            if (previous is not null && version.HasSameValuesAs(previous))
                expense.RemoveVersion(version);
            else
                previous = version;
        }
    }
}
