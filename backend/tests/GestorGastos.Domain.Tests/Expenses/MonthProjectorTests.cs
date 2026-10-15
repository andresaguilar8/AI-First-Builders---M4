using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Expenses;

public class MonthProjectorTests
{
    private static readonly Period October = new(2026, 10);
    private static readonly Period November = new(2026, 11);
    private static readonly Period December = new(2026, 12);
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static OneOffExpense OneOff(Period period, string name = "Regalo", DateOnly? dueDate = null) =>
        ExpenseFactory.CreateOneOff(
            period, ExpenseValues.Create(name, null, Amount.Parse("15000"), null, dueDate, period), Now);

    [Fact]
    [Trait("AC", "AC-13")]
    public void Puntual_NoApareceEnElMesSiguiente()
    {
        var expense = OneOff(October);

        Assert.Single(MonthProjector.Project(October, [expense]));
        Assert.Empty(MonthProjector.Project(November, [expense]));
    }

    [Fact]
    [Trait("AC", "AC-04")]
    public void PuntualCreadoViendoDiciembre_ApareceEnDiciembreYNoEnOctubre()
    {
        var expense = OneOff(December);

        Assert.Empty(MonthProjector.Project(October, [expense]));
        var effective = Assert.Single(MonthProjector.Project(December, [expense]));
        Assert.Equal(expense.Id, effective.ExpenseId);
        Assert.Equal(ExpenseKind.OneOff, effective.Kind);
        Assert.Equal(December, effective.Period);
    }

    [Fact]
    public void MesSinGastos_DevuelveListaVacia() =>
        Assert.Empty(MonthProjector.Project(October, []));

    [Fact]
    [Trait("AC", "AC-01")]
    public void Recurrente_ApareceEnSuMesDeInicioComoRecurrente()
    {
        var expense = ExpenseFactory.CreateRecurring(
            October, ExpenseValues.Create("Alquiler", null, Amount.Parse("300000"), null, null, October), Now);

        var effective = Assert.Single(MonthProjector.Project(October, [expense]));

        Assert.Equal(ExpenseKind.Recurring, effective.Kind);
        Assert.Equal("Alquiler", effective.Values.Name);
        Assert.Null(effective.PaidOn);
        Assert.Empty(MonthProjector.Project(October.Previous(), [expense]));
    }

    [Fact]
    [Trait("AC", "AC-07")]
    public void Recurrente_ConVencimientoFueraDelMes_UsaLaFechaExactaDelRegistro()
    {
        var expense = ExpenseFactory.CreateRecurring(
            October,
            ExpenseValues.Create("Seguro", null, Amount.Parse("8000"), null, new DateOnly(2026, 11, 5), October),
            Now);

        var effective = Assert.Single(MonthProjector.Project(October, [expense]));

        Assert.Equal(new DateOnly(2026, 11, 5), effective.Values.DueDate);
    }

    [Fact]
    public void Project_OrdenaPorVencimientoConLosVaciosAlFinalYLuegoPorNombre()
    {
        var expenses = new Expense[]
        {
            OneOff(October, "Sin fecha"),
            OneOff(October, "Zeta", new DateOnly(2026, 10, 10)),
            OneOff(October, "Alfa", new DateOnly(2026, 10, 10)),
            OneOff(October, "Primero", new DateOnly(2026, 10, 1)),
        };

        var names = MonthProjector.Project(October, expenses).Select(e => e.Values.Name);

        Assert.Equal(["Primero", "Alfa", "Zeta", "Sin fecha"], names);
    }
}
