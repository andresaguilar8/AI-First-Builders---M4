using GestorGastos.Domain.Common;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Expenses;

public class OneOffExpenseTests
{
    private static readonly Period October = new(2026, 10);
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static OneOffExpense Gift(DateOnly? dueDate = null) => ExpenseFactory.CreateOneOff(
        October, ExpenseValues.Create("Regalo", "Cumpleaños", Amount.Parse("15000"), null, dueDate, October), Now);

    [Fact]
    [Trait("AC", "AC-14")]
    public void Update_ReflejaLosNuevosValores()
    {
        var expense = Gift(new DateOnly(2026, 10, 20));
        var values = ExpenseValues.Create(
            "Regalo de Ana", "Aniversario", Amount.Parse("18000"), Guid.NewGuid(), new DateOnly(2026, 10, 25), October);

        expense.Update(values);

        Assert.Equal(values, expense.Values);
        Assert.Equal(October, expense.Period);
    }

    [Fact]
    public void Update_ConservaElPago()
    {
        var expense = Gift();
        expense.SetPaidOn(new DateOnly(2026, 10, 3));

        expense.Update(expense.Values with { Amount = Amount.Parse("20000") });

        Assert.Equal(new DateOnly(2026, 10, 3), expense.PaidOn);
    }

    [Fact]
    public void Update_PermiteQuitarElVencimiento()
    {
        var expense = Gift(new DateOnly(2026, 10, 20));

        expense.Update(expense.Values with { DueDate = null });

        Assert.Null(expense.DueDate);
    }

    [Fact]
    public void Update_ConVencimientoAnteriorAlMes_SeRechazaSinCambios()
    {
        var expense = Gift(new DateOnly(2026, 10, 20));

        var error = Assert.Throws<DomainValidationException>(() =>
            expense.Update(expense.Values with { DueDate = new DateOnly(2026, 9, 30) }));

        Assert.True(error.Errors.ContainsKey("dueDate"));
        Assert.Equal(new DateOnly(2026, 10, 20), expense.DueDate);
    }
}
