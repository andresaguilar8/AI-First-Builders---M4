using GestorGastos.Domain.Common;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Expenses;

public class CreateExpenseTests
{
    private static readonly Period October = new(2026, 10);
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static ExpenseValues Values(string name = "Alquiler", string amount = "300000", DateOnly? dueDate = null) =>
        ExpenseValues.Create(name, null, Amount.Parse(amount), null, dueDate, October);

    [Fact]
    [Trait("AC", "AC-02")]
    public void CreateOneOff_ConservaTodosLosValoresEnElMesVisualizado()
    {
        var categoryId = Guid.NewGuid();
        var values = ExpenseValues.Create(
            "Regalo", "Cumpleaños", Amount.Parse("15000"), categoryId, new DateOnly(2026, 10, 20), October);

        var expense = ExpenseFactory.CreateOneOff(October, values, Now);

        Assert.NotEqual(Guid.Empty, expense.Id);
        Assert.Equal(Now, expense.CreatedAt);
        Assert.Equal(October, expense.Period);
        Assert.Equal(values, expense.Values);
        Assert.Null(expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-01")]
    public void CreateRecurring_CreaCabeceraYVersionDesdeElMesVisualizado()
    {
        var expense = ExpenseFactory.CreateRecurring(October, Values(dueDate: new DateOnly(2026, 10, 10)), Now);

        Assert.Equal(October, expense.StartPeriod);
        Assert.Null(expense.EndPeriod);
        var version = Assert.Single(expense.Versions);
        Assert.Equal(October, version.EffectiveFrom);
        Assert.Equal("Alquiler", version.Name);
        Assert.Equal(Amount.Parse("300000"), version.Amount);
        Assert.Equal(10, version.DueDay);
        Assert.Empty(expense.Instances);
    }

    [Fact]
    [Trait("AC", "AC-01")]
    public void CreateRecurring_SinVencimiento_GuardaVersionSinDia()
    {
        var expense = ExpenseFactory.CreateRecurring(October, Values(), Now);

        Assert.Null(Assert.Single(expense.Versions).DueDay);
        Assert.Empty(expense.Instances);
    }

    [Fact]
    [Trait("AC", "AC-07")]
    public void CreateOneOff_ConVencimientoEnUnMesPosterior_LoConserva()
    {
        var expense = ExpenseFactory.CreateOneOff(October, Values(dueDate: new DateOnly(2026, 11, 5)), Now);

        Assert.Equal(new DateOnly(2026, 11, 5), expense.DueDate);
    }

    [Fact]
    [Trait("AC", "AC-07")]
    public void CreateRecurring_ConVencimientoFueraDelMes_GuardaLaFechaExactaEnElRegistroDelMes()
    {
        var expense = ExpenseFactory.CreateRecurring(October, Values(dueDate: new DateOnly(2026, 11, 5)), Now);

        Assert.Equal(5, Assert.Single(expense.Versions).DueDay);
        var instance = Assert.Single(expense.Instances);
        Assert.Equal(October, instance.Period);
        Assert.Equal(new DateOnly(2026, 11, 5), instance.DueDate);
        Assert.Null(instance.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-03")]
    public void Create_SinNombreOSinMonto_SeRechaza()
    {
        var error = Assert.Throws<DomainValidationException>(() =>
            ExpenseValues.Create(" ", null, null, null, null, October));

        Assert.True(error.Errors.ContainsKey("name"));
        Assert.True(error.Errors.ContainsKey("amount"));
    }

    [Fact]
    [Trait("AC", "AC-08")]
    public void Create_ConVencimientoAnteriorAlMes_SeRechaza()
    {
        var error = Assert.Throws<DomainValidationException>(() => Values(dueDate: new DateOnly(2026, 9, 30)));

        Assert.True(error.Errors.ContainsKey("dueDate"));
    }
}
