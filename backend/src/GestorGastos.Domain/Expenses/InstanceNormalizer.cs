using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

/// <summary>
/// Un registro mensual existe solo si el mes tiene información propia: un pago o algún valor
/// distinto de la proyección de la versión vigente (RNF-05, RNF-06).
/// </summary>
public static class InstanceNormalizer
{
    /// <summary>Elimina los registros sin información propia desde el mes indicado.</summary>
    public static void Normalize(RecurringExpense expense, Period from)
    {
        foreach (var instance in expense.Instances.Where(i => i.Period >= from).ToList())
        {
            if (!HasOwnInformation(expense, instance))
                expense.RemoveInstance(instance);
        }
    }

    public static bool HasOwnInformation(RecurringExpense expense, RecurringInstance instance) =>
        instance.PaidOn is not null
        || instance.Values != expense.VersionIn(instance.Period).ProjectTo(instance.Period);
}
