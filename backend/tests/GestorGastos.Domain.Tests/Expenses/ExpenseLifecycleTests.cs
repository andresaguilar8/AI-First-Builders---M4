using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Payments;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Expenses;

public class ExpenseLifecycleTests
{
    private static readonly Period January = new(2026, 1);
    private static readonly Period August = new(2026, 8);
    private static readonly Period September = new(2026, 9);
    private static readonly Period October = new(2026, 10);
    private static readonly Period November = new(2026, 11);
    private static readonly Period December = new(2026, 12);
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static RecurringExpense Recurring(Period start, DateOnly? dueDate = null) => ExpenseFactory.CreateRecurring(
        start, ExpenseValues.Create("Internet", "Fibra", Amount.Parse("20000"), Guid.NewGuid(), dueDate, start), Now);

    private static OneOffExpense OneOff(Period period, DateOnly? dueDate = null) => ExpenseFactory.CreateOneOff(
        period, ExpenseValues.Create("Regalo", "Cumpleaños", Amount.Parse("15000"), Guid.NewGuid(), dueDate, period), Now);

    private static EffectiveExpense? In(Expense expense, Period period) => MonthProjector.EffectiveIn(expense, period);

    [Fact]
    [Trait("AC", "AC-18")]
    public void PuntualARecurrente_ApareceEnElMesYEnLosSiguientesConSusValores()
    {
        var oneOff = OneOff(October, new DateOnly(2026, 10, 20));

        var recurring = ExpenseLifecycle.ToRecurring(oneOff, October, Now);

        Assert.NotEqual(oneOff.Id, recurring.Id);
        Assert.Equal(October, recurring.StartPeriod);
        Assert.Equal(oneOff.Values, In(recurring, October)?.Values);
        var november = In(recurring, November);
        Assert.Equal(oneOff.Values with { DueDate = new DateOnly(2026, 11, 20) }, november?.Values);
        Assert.Null(In(recurring, September));
        Assert.Empty(recurring.Instances);
    }

    [Fact]
    [Trait("AC", "AC-18")]
    public void PuntualPagadoARecurrente_ConservaElPagoSoloEnElMes()
    {
        var oneOff = OneOff(October);
        PaymentService.MarkPaid(oneOff, October, Today, null);

        var recurring = ExpenseLifecycle.ToRecurring(oneOff, October, Now);

        Assert.Equal(Today, In(recurring, October)?.PaidOn);
        Assert.Null(In(recurring, November)?.PaidOn);
        Assert.Single(recurring.Instances);
    }

    [Fact]
    public void PuntualConVencimientoEnElMesSiguienteARecurrente_GuardaLaFechaExacta()
    {
        var oneOff = OneOff(October, new DateOnly(2026, 11, 5));

        var recurring = ExpenseLifecycle.ToRecurring(oneOff, October, Now);

        Assert.Equal(new DateOnly(2026, 11, 5), In(recurring, October)?.Values.DueDate);
        Assert.Equal(new DateOnly(2026, 12, 5), In(recurring, December)?.Values.DueDate);
    }

    [Fact]
    [Trait("AC", "AC-19")]
    public void RecurrenteAPuntual_TerminaElMesAnteriorYQuedaUnPuntualConLosValoresYElPagoDelMes()
    {
        var recurring = Recurring(August, new DateOnly(2026, 8, 10));
        RecurringEditor.EditThisMonth(
            recurring, October, In(recurring, October)!.Values with { Amount = Amount.Parse("21000") });
        PaymentService.MarkPaid(recurring, October, Today, null);
        var octoberValues = In(recurring, October)!.Values;

        var result = ExpenseLifecycle.ToOneOff(recurring, October, Now);

        Assert.False(result.RemoveOriginal);
        Assert.Equal(September, recurring.EndPeriod);
        Assert.NotNull(In(recurring, August));
        Assert.NotNull(In(recurring, September));
        Assert.Null(In(recurring, October));
        Assert.Null(In(recurring, November));
        var oneOff = Assert.IsType<OneOffExpense>(result.Created);
        Assert.Equal(October, oneOff.Period);
        Assert.Equal(octoberValues, oneOff.Values);
        Assert.Equal(Today, oneOff.PaidOn);
        Assert.Null(In(oneOff, November));
    }

    [Fact]
    [Trait("AC", "AC-20")]
    public void RecurrentePagadoEnNoviembreConvertidoViendoOctubre_NoApareceEnNoviembre()
    {
        var recurring = Recurring(August);
        PaymentService.MarkPaid(recurring, November, Today, null);

        var result = ExpenseLifecycle.ToOneOff(recurring, October, Now);

        Assert.Null(In(recurring, November));
        Assert.Null(In(result.Created, November));
        Assert.Empty(recurring.Instances);
    }

    [Fact]
    [Trait("AC", "AC-21")]
    public void Baja_ConservaLosMesesAnterioresYNoApareceDesdeElMesVisualizado()
    {
        var recurring = Recurring(January);
        for (var period = January; period <= August; period = period.Next())
            PaymentService.MarkPaid(recurring, period, Today, null);

        var outcome = ExpenseLifecycle.Discontinue(recurring, September);

        Assert.Equal(DiscontinueOutcome.Ended, outcome);
        Assert.Equal(August, recurring.EndPeriod);
        for (var period = January; period <= August; period = period.Next())
            Assert.Equal(Today, In(recurring, period)?.PaidOn);
        Assert.Null(In(recurring, September));
        Assert.Null(In(recurring, new Period(2027, 3)));
        Assert.Equal(8, recurring.Instances.Count);
    }

    [Fact]
    [Trait("AC", "AC-22")]
    public void BajaEnElMesPagado_NoApareceYBorraSuRegistro()
    {
        var recurring = Recurring(August);
        PaymentService.MarkPaid(recurring, September, Today, null);

        ExpenseLifecycle.Discontinue(recurring, September);

        Assert.Null(In(recurring, September));
        Assert.Empty(recurring.Instances);
    }

    [Fact]
    [Trait("AC", "AC-65")]
    public void BajaViendoOctubre_BorraLosRegistrosDeOctubreYNoviembre()
    {
        var recurring = Recurring(August);
        PaymentService.MarkPaid(recurring, November, Today, null);
        PaymentService.MarkPaid(recurring, September, Today, null);

        ExpenseLifecycle.Discontinue(recurring, October);

        Assert.Equal([September], recurring.Instances.Select(i => i.Period));
    }

    [Fact]
    public void BajaOConversionEnElMesDeInicio_EliminaElRecurrenteCompleto()
    {
        var discontinued = Recurring(October);
        var converted = Recurring(October);

        Assert.Equal(DiscontinueOutcome.Removed, ExpenseLifecycle.Discontinue(discontinued, October));
        var result = ExpenseLifecycle.ToOneOff(converted, October, Now);

        Assert.True(result.RemoveOriginal);
        Assert.Equal(October, Assert.IsType<OneOffExpense>(result.Created).Period);
        Assert.Null(discontinued.EndPeriod);
    }

    [Fact]
    public void Operar_EnUnMesEnQueElGastoNoAparece_Falla()
    {
        var recurring = Recurring(October);
        ExpenseLifecycle.Discontinue(Recurring(August), October);

        Assert.Throws<InvalidOperationException>(() => ExpenseLifecycle.Discontinue(recurring, September));
        Assert.Throws<InvalidOperationException>(() => ExpenseLifecycle.ToOneOff(recurring, September, Now));
        Assert.Throws<InvalidOperationException>(() => ExpenseLifecycle.ToRecurring(OneOff(October), November, Now));
    }
}
