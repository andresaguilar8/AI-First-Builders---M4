using GestorGastos.Domain.Alerts;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Payments;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Alerts;

public class DueTodayAlertTests
{
    private static readonly Period August = new(2026, 8);
    private static readonly Period September = new(2026, 9);
    private static readonly Period October = new(2026, 10);
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static OneOffExpense OneOff(Period period, DateOnly? dueDate) => ExpenseFactory.CreateOneOff(
        period, ExpenseValues.Create("Luz", null, Amount.Parse("12000"), null, dueDate, period), Now);

    private static RecurringExpense Recurring(Period start, DateOnly? dueDate) => ExpenseFactory.CreateRecurring(
        start, ExpenseValues.Create("Internet", null, Amount.Parse("20000"), null, dueDate, start), Now);

    [Fact]
    [Trait("AC", "AC-40")]
    public void PendienteQueVenceHoy_Cuenta() =>
        Assert.Equal(1, DueTodayAlert.Count([OneOff(October, Today)], Today));

    [Fact]
    [Trait("AC", "AC-42")]
    public void UnicoQueVenceHoyPagado_NoCuenta()
    {
        var expense = OneOff(October, Today);
        PaymentService.MarkPaid(expense, October, Today, null);

        Assert.Equal(0, DueTodayAlert.Count([expense], Today));
    }

    [Fact]
    [Trait("AC", "AC-67")]
    public void PendienteDeSeptiembreQueVenceHoy_Cuenta() =>
        Assert.Equal(1, DueTodayAlert.Count([OneOff(September, Today)], Today));

    [Fact]
    [Trait("AC", "AC-41")]
    public void RecurranteSoloProyectadoQueVenceHoy_Cuenta() =>
        Assert.Equal(1, DueTodayAlert.Count([Recurring(August, new DateOnly(2026, 8, 5))], Today));

    [Fact]
    public void RecurrenteProyectadoPagadoHoy_NoCuenta()
    {
        var expense = Recurring(August, new DateOnly(2026, 8, 5));
        PaymentService.MarkPaid(expense, October, Today, null);

        Assert.Equal(0, DueTodayAlert.Count([expense], Today));
    }

    [Fact]
    public void RecurrenteConRegistroDeSeptiembreQueVenceHoy_CuentaCadaMesPendiente()
    {
        // Septiembre tiene su fecha exacta (05/10) y octubre proyecta el 5: dos obligaciones pendientes.
        var expense = Recurring(September, new DateOnly(2026, 10, 5));

        Assert.Equal(2, DueTodayAlert.Count([expense], Today));
    }

    [Fact]
    public void RecurrenteConVencimientoDelMesMovidoSoloEsteMes_NoCuentaLaProyeccion()
    {
        var expense = Recurring(August, new DateOnly(2026, 8, 5));
        RecurringEditor.EditThisMonth(
            expense, October, MonthProjector.EffectiveIn(expense, October)!.Values with { DueDate = new DateOnly(2026, 10, 20) });

        Assert.Equal(0, DueTodayAlert.Count([expense], Today));
    }

    [Theory]
    [InlineData("2026-10-04")]
    [InlineData("2026-10-06")]
    [InlineData(null)]
    public void VencidosFuturosYSinVencimiento_NoCuentan(string? dueDate) =>
        Assert.Equal(0, DueTodayAlert.Count(
            [OneOff(October, dueDate is null ? null : DateOnly.Parse(dueDate)), Recurring(October, dueDate is null ? null : DateOnly.Parse(dueDate))],
            Today));
}
