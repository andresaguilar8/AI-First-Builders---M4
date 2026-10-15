namespace GestorGastos.Domain.Expenses;

/// <summary>Campos que cambiaron entre los valores efectivos de un mes y los editados (RF-11).</summary>
internal sealed class ExpenseChanges(ExpenseValues before, ExpenseValues after)
{
    public ExpenseValues After => after;

    public bool Name => before.Name != after.Name;

    public bool Description => before.Description != after.Description;

    public bool Amount => before.Amount != after.Amount;

    public bool CategoryId => before.CategoryId != after.CategoryId;

    public bool DueDate => before.DueDate != after.DueDate;

    public bool Any => Name || Description || Amount || CategoryId || DueDate;
}
