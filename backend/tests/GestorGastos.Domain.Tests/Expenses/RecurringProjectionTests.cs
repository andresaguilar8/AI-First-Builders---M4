using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Expenses;

public class RecurringProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static RecurringExpense Recurring(Period start, DateOnly? dueDate) =>
        ExpenseFactory.CreateRecurring(
            start, ExpenseValues.Create("Alquiler", null, Amount.Parse("300000"), null, dueDate, start), Now);

    private static EffectiveExpense? In(Expense expense, Period period) => MonthProjector.EffectiveIn(expense, period);

    [Fact]
    [Trait("AC", "AC-09")]
    public void Recurrente_ApareceEnLosMesesSiguientesSinCrearlos()
    {
        var expense = Recurring(new Period(2026, 10), new DateOnly(2026, 10, 10));

        var november = In(expense, new Period(2026, 11));
        var nextYear = In(expense, new Period(2027, 10));

        Assert.NotNull(november);
        Assert.Equal("Alquiler", november.Values.Name);
        Assert.Equal(new DateOnly(2026, 11, 10), november.Values.DueDate);
        Assert.Null(november.PaidOn);
        Assert.Equal(new DateOnly(2027, 10, 10), nextYear?.Values.DueDate);
        Assert.Empty(expense.Instances);
    }

    [Fact]
    [Trait("AC", "AC-10")]
    public void VencimientoDia30_EnFebreroCaeElUltimoDiaYEnMarzoVuelveAl30()
    {
        var expense = Recurring(new Period(2027, 1), new DateOnly(2027, 1, 30));

        Assert.Equal(new DateOnly(2027, 2, 28), In(expense, new Period(2027, 2))?.Values.DueDate);
        Assert.Equal(new DateOnly(2027, 3, 30), In(expense, new Period(2027, 3))?.Values.DueDate);
    }

    [Fact]
    [Trait("AC", "AC-11")]
    public void CreadoEnOctubreConVencimientoEnNoviembre_ProyectaEl5DeCadaMes()
    {
        var expense = Recurring(new Period(2026, 10), new DateOnly(2026, 11, 5));

        Assert.Equal(new DateOnly(2026, 11, 5), In(expense, new Period(2026, 10))?.Values.DueDate);
        Assert.Equal(new DateOnly(2026, 11, 5), In(expense, new Period(2026, 11))?.Values.DueDate);
        Assert.Equal(new DateOnly(2026, 12, 5), In(expense, new Period(2026, 12))?.Values.DueDate);
    }

    [Fact]
    public void SinVencimiento_ProyectaSinVencimiento()
    {
        var expense = Recurring(new Period(2026, 10), null);

        Assert.Null(In(expense, new Period(2026, 12))?.Values.DueDate);
        Assert.NotNull(In(expense, new Period(2026, 12)));
    }

    [Fact]
    public void Recurrente_NoApareceAntesDeSuMesDeInicio() =>
        Assert.Null(In(Recurring(new Period(2026, 10), null), new Period(2026, 9)));
}
