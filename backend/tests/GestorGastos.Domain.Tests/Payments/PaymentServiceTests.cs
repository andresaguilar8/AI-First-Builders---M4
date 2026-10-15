using GestorGastos.Domain.Common;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Payments;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Payments;

public class PaymentServiceTests
{
    private static readonly Period September = new(2026, 9);
    private static readonly Period October = new(2026, 10);
    private static readonly Period December = new(2026, 12);
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static OneOffExpense OneOff(Period period, DateOnly? dueDate) => ExpenseFactory.CreateOneOff(
        period, ExpenseValues.Create("Luz", null, Amount.Parse("12000"), null, dueDate, period), Now);

    private static RecurringExpense Recurring(Period start, string amount, DateOnly? dueDate) =>
        ExpenseFactory.CreateRecurring(
            start, ExpenseValues.Create("Internet", null, Amount.Parse(amount), null, dueDate, start), Now);

    private static EffectiveExpense In(Expense expense, Period period) =>
        MonthProjector.EffectiveIn(expense, period) ?? throw new InvalidOperationException("No aparece en el mes.");

    [Fact]
    [Trait("AC", "AC-35")]
    public void NoVencido_SeRegistraHoyAunqueSeIndiqueOtraFecha()
    {
        var expense = OneOff(October, new DateOnly(2026, 10, 10));

        PaymentService.MarkPaid(expense, October, Today, paidOn: new DateOnly(2026, 10, 1));

        Assert.Equal(Today, expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-35")]
    public void GastoDeUnMesFuturo_SeRegistraHoy()
    {
        var expense = OneOff(December, new DateOnly(2026, 12, 10));

        PaymentService.MarkPaid(expense, December, Today, paidOn: null);

        Assert.Equal(Today, expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-36")]
    public void Vencido_ConFechaReal_UsaEsaFecha()
    {
        var expense = OneOff(October, new DateOnly(2026, 10, 1));

        PaymentService.MarkPaid(expense, October, Today, new DateOnly(2026, 10, 3));

        Assert.Equal(new DateOnly(2026, 10, 3), expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-37")]
    public void Vencido_SinFecha_UsaHoy()
    {
        var expense = OneOff(October, new DateOnly(2026, 10, 1));

        PaymentService.MarkPaid(expense, October, Today, paidOn: null);

        Assert.Equal(Today, expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-38")]
    public void Vencido_ConFechaFutura_SeRechazaYSiguePendiente()
    {
        var expense = OneOff(October, new DateOnly(2026, 10, 1));

        var error = Assert.Throws<DomainValidationException>(() =>
            PaymentService.MarkPaid(expense, October, Today, new DateOnly(2026, 10, 6)));

        Assert.True(error.Errors.ContainsKey("paidOn"));
        Assert.Null(expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-66")]
    public void Vencido_ConFechaAnteriorAlMesDelGasto_SeAcepta()
    {
        var expense = OneOff(October, new DateOnly(2026, 10, 1));

        PaymentService.MarkPaid(expense, October, Today, new DateOnly(2026, 8, 15));

        Assert.Equal(new DateOnly(2026, 8, 15), expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-39")]
    public void Desmarcar_DejaElGastoPendienteSinFecha()
    {
        var expense = OneOff(October, new DateOnly(2026, 10, 10));
        PaymentService.MarkPaid(expense, October, Today, null);

        PaymentService.Unmark(expense, October);

        Assert.Null(expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-24")]
    public void RecurrentePagadoEnSeptiembre_OctubreSiguePendiente()
    {
        var expense = Recurring(September, "20000", new DateOnly(2026, 9, 15));

        PaymentService.MarkPaid(expense, September, new DateOnly(2026, 9, 10), null);

        Assert.Equal(new DateOnly(2026, 9, 10), In(expense, September).PaidOn);
        var october = In(expense, October);
        Assert.Null(october.PaidOn);
        Assert.Equal(Amount.Parse("20000"), october.Values.Amount);
        Assert.Single(expense.Instances);
    }

    [Fact]
    [Trait("AC", "AC-68")]
    public void Recurrente_DesmarcarCuandoElPagoEraLoUnicoPropio_EliminaElRegistro()
    {
        var expense = Recurring(September, "20000", null);
        PaymentService.MarkPaid(expense, September, Today, null);
        Assert.NotNull(expense.InstanceIn(September));

        PaymentService.Unmark(expense, September);

        Assert.Null(In(expense, September).PaidOn);
        Assert.Empty(expense.Instances);
    }

    [Fact]
    [Trait("AC", "AC-61")]
    public void Recurrente_DesmarcarConCambioSoloEsteMes_ConservaElRegistroSinPago()
    {
        var expense = Recurring(September, "20000", null);
        PaymentService.MarkPaid(expense, September, Today, null);
        RecurringEditor.EditThisMonth(
            expense, September, In(expense, September).Values with { Amount = Amount.Parse("21000") });

        PaymentService.Unmark(expense, September);

        var instance = Assert.Single(expense.Instances);
        Assert.Null(instance.PaidOn);
        Assert.Equal(Amount.Parse("21000"), instance.Amount);
    }

    [Fact]
    public void Recurrente_VencidoEnSuMes_AceptaFechaReal()
    {
        var expense = Recurring(September, "20000", new DateOnly(2026, 9, 15));

        PaymentService.MarkPaid(expense, September, Today, new DateOnly(2026, 9, 20));

        Assert.Equal(new DateOnly(2026, 9, 20), In(expense, September).PaidOn);
    }

    [Fact]
    public void PagarEnUnMesEnQueNoAparece_Falla()
    {
        var expense = Recurring(October, "20000", null);

        Assert.Throws<InvalidOperationException>(() => PaymentService.MarkPaid(expense, September, Today, null));
        Assert.Throws<InvalidOperationException>(() => PaymentService.MarkPaid(OneOff(October, null), September, Today, null));
    }
}
