using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Expenses;

public enum ExpenseKind
{
    OneOff,
    Recurring,
}

/// <summary>Valores con los que un gasto aparece en un mes y su pago en ese mes.</summary>
public sealed record EffectiveExpense(
    Guid ExpenseId,
    ExpenseKind Kind,
    Period Period,
    ExpenseValues Values,
    DateOnly? PaidOn);
