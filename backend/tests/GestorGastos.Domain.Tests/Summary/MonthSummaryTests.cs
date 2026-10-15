using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;
using GestorGastos.Domain.Summary;

namespace GestorGastos.Domain.Tests.Summary;

public class MonthSummaryTests
{
    private static readonly Period September = new(2026, 9);
    private static readonly Guid Housing = Guid.NewGuid();
    private static readonly Guid Services = Guid.NewGuid();
    private static readonly DateOnly PaidOn = new(2026, 9, 3);

    private static EffectiveExpense Expense(string amount, Guid? categoryId, bool paid = false) => new(
        Guid.NewGuid(),
        ExpenseKind.OneOff,
        September,
        ExpenseValues.Create("Gasto", null, Amount.Parse(amount), categoryId, null, September),
        paid ? PaidOn : null);

    [Fact]
    [Trait("AC", "AC-32")]
    public void PorCategoria_SumaPagadosYPendientes()
    {
        var summary = MonthSummary.Calculate(
        [
            Expense("200000", Housing, paid: true),
            Expense("100000", Housing),
            Expense("100000", Services),
        ]);

        Assert.Equal(300000m, summary.ByCategory.Single(c => c.CategoryId == Housing).Total);
        Assert.Equal(100000m, summary.ByCategory.Single(c => c.CategoryId == Services).Total);
        Assert.Equal(2, summary.ByCategory.Count);
    }

    [Fact]
    [Trait("AC", "AC-33")]
    public void SinCategoria_SeAgrupaAparte()
    {
        var summary = MonthSummary.Calculate([Expense("50000", null), Expense("1000", Services)]);

        Assert.Equal(50000m, summary.ByCategory.Single(c => c.CategoryId is null).Total);
    }

    [Fact]
    [Trait("AC", "AC-34")]
    public void Totales_DelMesYPendiente()
    {
        var summary = MonthSummary.Calculate(
        [
            Expense("200000", Housing, paid: true),
            Expense("100000", null, paid: true),
            Expense("60000", Services),
            Expense("40000", null),
        ]);

        Assert.Equal(400000m, summary.Total);
        Assert.Equal(100000m, summary.PendingTotal);
    }

    [Fact]
    public void MesSinGastos_TotalesEnCeroYSinCategorias()
    {
        var summary = MonthSummary.Calculate([]);

        Assert.Equal((0m, 0m), (summary.Total, summary.PendingTotal));
        Assert.Empty(summary.ByCategory);
    }
}
