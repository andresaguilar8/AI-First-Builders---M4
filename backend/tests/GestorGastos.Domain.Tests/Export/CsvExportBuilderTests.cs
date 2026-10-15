using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Export;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Payments;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Export;

public class CsvExportBuilderTests
{
    private const string Header = "periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago";

    private static readonly Period August = new(2026, 8);
    private static readonly Period October = new(2026, 10);
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly Dictionary<Guid, string> NoCategories = [];

    private static OneOffExpense OneOff(Period period, string name, DateOnly? dueDate = null, Guid? categoryId = null, string amount = "1000") =>
        ExpenseFactory.CreateOneOff(
            period, ExpenseValues.Create(name, "no se exporta", Amount.Parse(amount), categoryId, dueDate, period), Now);

    private static RecurringExpense Recurring(Period start, string name, DateOnly? dueDate = null) =>
        ExpenseFactory.CreateRecurring(
            start, ExpenseValues.Create(name, null, Amount.Parse("30000"), null, dueDate, start), Now);

    private static string[] Lines(string csv)
    {
        Assert.EndsWith("\r\n", csv);
        return csv[..^2].Split("\r\n");
    }

    [Fact]
    [Trait("AC", "AC-58")]
    public void IncluyeLosMesesProyectadosHastaElMesActualYNingunoPosterior()
    {
        var csv = CsvExportBuilder.Build(
            [OneOff(August, "Regalo"), Recurring(August, "Alquiler")], NoCategories, October);

        var lines = Lines(csv);
        Assert.Equal(Header, lines[0]);
        Assert.Equal(["2026-08", "2026-08", "2026-09", "2026-10"], lines[1..].Select(l => l[..7]));
    }

    [Fact]
    [Trait("AC", "AC-59")]
    public void FormateaLaFilaYEscapaLasComas()
    {
        var csv = CsvExportBuilder.Build(
            [OneOff(October, "Luz, gas", new DateOnly(2026, 10, 10), amount: "1234.56")], NoCategories, October);

        Assert.Equal([Header, "2026-10,\"Luz, gas\",1234.56,Sin categoría,2026-10-10,Pendiente,"], Lines(csv));
    }

    [Fact]
    public void Pagado_IncluyeEstadoYFechaDePagoYElNombreActualDeLaCategoria()
    {
        var categoryId = Guid.NewGuid();
        var expense = OneOff(October, "Internet", categoryId: categoryId, amount: "30000");
        PaymentService.MarkPaid(expense, October, Today, null);

        var csv = CsvExportBuilder.Build([expense], new Dictionary<Guid, string> { [categoryId] = "Servicios" }, October);

        Assert.Equal("2026-10,Internet,30000.00,Servicios,,Pagado,2026-10-05", Lines(csv)[1]);
    }

    [Fact]
    public void ComillasInternas_SeDuplican() =>
        Assert.Equal(
            "2026-10,\"El \"\"bueno\"\"\",1000.00,Sin categoría,,Pendiente,",
            Lines(CsvExportBuilder.Build([OneOff(October, "El \"bueno\"")], NoCategories, October))[1]);

    [Fact]
    public void SinGastos_SoloElEncabezado() =>
        Assert.Equal([Header], Lines(CsvExportBuilder.Build([], NoCategories, October)));

    [Fact]
    public void SoloGastosFuturos_SoloElEncabezado() =>
        Assert.Equal([Header], Lines(CsvExportBuilder.Build([OneOff(new Period(2026, 12), "Regalo")], NoCategories, October)));

    [Fact]
    public void OrdenaPorPeriodoLuegoVencimientoConVaciosAlFinalYLuegoNombre()
    {
        var csv = CsvExportBuilder.Build(
        [
            OneOff(October, "Sin fecha"),
            OneOff(October, "Zeta", new DateOnly(2026, 10, 10)),
            OneOff(October, "Alfa", new DateOnly(2026, 10, 10)),
            OneOff(October, "Primero", new DateOnly(2026, 10, 1)),
            OneOff(August, "Agosto"),
        ], NoCategories, October);

        Assert.Equal(
            ["Agosto", "Primero", "Alfa", "Zeta", "Sin fecha"],
            Lines(csv)[1..].Select(l => l.Split(',')[1]));
    }
}
