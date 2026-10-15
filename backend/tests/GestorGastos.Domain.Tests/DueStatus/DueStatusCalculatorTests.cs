using GestorGastos.Domain.DueStatus;

namespace GestorGastos.Domain.Tests.DueStatus;

public class DueStatusCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    private static ExpenseDueStatus Pending(DateOnly? dueDate) =>
        DueStatusCalculator.Calculate(dueDate, paidOn: null, Today);

    [Fact]
    [Trait("AC", "AC-43")]
    public void VenceEnMasDeTresDias_EstaAlDia() =>
        Assert.Equal(ExpenseDueStatus.AlDia, Pending(new DateOnly(2026, 10, 9)));

    [Fact]
    [Trait("AC", "AC-44")]
    public void SinVencimiento_EstaAlDia() =>
        Assert.Equal(ExpenseDueStatus.AlDia, Pending(null));

    [Theory]
    [Trait("AC", "AC-45")]
    [InlineData(8)]
    [InlineData(6)]
    public void VenceEntreUnoYTresDias_EstaProximoAVencer(int day) =>
        Assert.Equal(ExpenseDueStatus.ProximoAVencer, Pending(new DateOnly(2026, 10, day)));

    [Fact]
    [Trait("AC", "AC-46")]
    public void VenceHoy_EstaVenceHoy() =>
        Assert.Equal(ExpenseDueStatus.VenceHoy, Pending(Today));

    [Fact]
    [Trait("AC", "AC-47")]
    public void VencioAyer_EstaVencido() =>
        Assert.Equal(ExpenseDueStatus.Vencido, Pending(new DateOnly(2026, 10, 4)));

    [Fact]
    public void ContandoDiasCalendarioEntreMeses_VencidoYProximo()
    {
        Assert.Equal(ExpenseDueStatus.Vencido, Pending(new DateOnly(2026, 9, 30)));
        Assert.Equal(ExpenseDueStatus.ProximoAVencer, DueStatusCalculator.Calculate(
            new DateOnly(2026, 11, 2), null, new DateOnly(2026, 10, 30)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026-10-04")]
    [InlineData("2026-10-05")]
    public void Pagado_EstaPagadoSinImportarElVencimiento(string? dueDate) =>
        Assert.Equal(ExpenseDueStatus.Pagado, DueStatusCalculator.Calculate(
            dueDate is null ? null : DateOnly.Parse(dueDate), new DateOnly(2026, 10, 1), Today));
}
