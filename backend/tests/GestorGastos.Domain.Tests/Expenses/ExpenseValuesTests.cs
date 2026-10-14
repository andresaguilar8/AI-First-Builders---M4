using GestorGastos.Domain.Common;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Expenses;

public class ExpenseValuesTests
{
    private static readonly Period October = new(2026, 10);
    private static readonly Amount Amount = Amount.Parse("15000");

    private static DomainValidationException CreateFails(
        string? name = "Regalo",
        string? description = null,
        Amount? amount = null,
        bool omitAmount = false,
        DateOnly? dueDate = null) =>
        Assert.Throws<DomainValidationException>(() =>
            ExpenseValues.Create(name, description, omitAmount ? null : amount ?? Amount, null, dueDate, October));

    [Fact]
    [Trait("AC", "AC-02")]
    public void Create_ConservaTodosLosValores()
    {
        var categoryId = Guid.NewGuid();

        var values = ExpenseValues.Create(
            "Regalo", "Cumpleaños", Amount, categoryId, new DateOnly(2026, 10, 20), October);

        Assert.Equal("Regalo", values.Name);
        Assert.Equal("Cumpleaños", values.Description);
        Assert.Equal(Amount, values.Amount);
        Assert.Equal(categoryId, values.CategoryId);
        Assert.Equal(new DateOnly(2026, 10, 20), values.DueDate);
    }

    [Theory]
    [Trait("AC", "AC-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RechazaNombreAusenteOVacioTrasRecortar(string? name) =>
        Assert.True(CreateFails(name: name).Errors.ContainsKey("name"));

    [Fact]
    [Trait("AC", "AC-03")]
    public void Create_RechazaMontoAusente() =>
        Assert.True(CreateFails(omitAmount: true).Errors.ContainsKey("amount"));

    [Fact]
    public void Create_RecortaNombreYDescripcion()
    {
        var values = ExpenseValues.Create("  Luz  ", "  Edesur ", Amount, null, null, October);

        Assert.Equal("Luz", values.Name);
        Assert.Equal("Edesur", values.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_DescripcionVaciaQuedaSinDescripcion(string? description) =>
        Assert.Null(ExpenseValues.Create("Luz", description, Amount, null, null, October).Description);

    [Fact]
    public void Create_AceptaNombreDeHasta100CaracteresYRechaza101()
    {
        var hundred = new string('a', 100);

        Assert.Equal(hundred, ExpenseValues.Create($" {hundred} ", null, Amount, null, null, October).Name);
        Assert.True(CreateFails(name: hundred + "a").Errors.ContainsKey("name"));
    }

    [Fact]
    public void Create_AceptaDescripcionDeHasta500CaracteresYRechaza501()
    {
        var fiveHundred = new string('d', 500);

        Assert.Equal(fiveHundred, ExpenseValues.Create("Luz", fiveHundred, Amount, null, null, October).Description);
        Assert.True(CreateFails(description: fiveHundred + "d").Errors.ContainsKey("description"));
    }

    [Fact]
    [Trait("AC", "AC-08")]
    public void Create_RechazaVencimientoEnUnMesAnterior() =>
        Assert.True(CreateFails(dueDate: new DateOnly(2026, 9, 30)).Errors.ContainsKey("dueDate"));

    [Fact]
    [Trait("AC", "AC-07")]
    public void Create_AceptaVencimientoEnUnMesPosterior() =>
        Assert.Equal(
            new DateOnly(2026, 11, 5),
            ExpenseValues.Create("Luz", null, Amount, null, new DateOnly(2026, 11, 5), October).DueDate);

    [Fact]
    public void Create_AceptaVencimientoElPrimerDiaDelMes() =>
        Assert.Equal(
            new DateOnly(2026, 10, 1),
            ExpenseValues.Create("Luz", null, Amount, null, new DateOnly(2026, 10, 1), October).DueDate);

    [Fact]
    public void Create_AceptaGastoSinVencimiento() =>
        Assert.Null(ExpenseValues.Create("Luz", null, Amount, null, null, October).DueDate);

    [Fact]
    public void Create_InformaTodosLosErroresJuntos()
    {
        var error = CreateFails(name: " ", omitAmount: true, dueDate: new DateOnly(2026, 9, 1));

        Assert.Equal(["amount", "dueDate", "name"], error.Errors.Keys.Order());
    }
}
