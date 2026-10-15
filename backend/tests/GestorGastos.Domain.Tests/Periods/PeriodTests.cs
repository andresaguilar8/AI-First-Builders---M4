using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Periods;

public class PeriodTests
{
    [Fact]
    public void Parse_LeeAnioYMesEnFormatoYyyyMm()
    {
        var period = Period.Parse("2026-10");

        Assert.Equal(2026, period.Year);
        Assert.Equal(10, period.Month);
    }

    [Theory]
    [InlineData(2026, 1, "2026-01")]
    [InlineData(2026, 12, "2026-12")]
    [InlineData(987, 3, "0987-03")]
    public void ToString_FormateaComoYyyyMm(int year, int month, string expected) =>
        Assert.Equal(expected, new Period(year, month).ToString());

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("2026-1")]
    [InlineData("26-10")]
    [InlineData("2026/10")]
    [InlineData("2026-00")]
    [InlineData("2026-13")]
    [InlineData("2026-10-01")]
    [InlineData("abcd-ef")]
    public void Parse_RechazaFormatosInvalidos(string value)
    {
        Assert.Throws<FormatException>(() => Period.Parse(value));
        Assert.False(Period.TryParse(value, out _));
    }

    [Fact]
    public void TryParse_AceptaUnPeriodoValido()
    {
        Assert.True(Period.TryParse("2027-02", out var period));
        Assert.Equal(new Period(2027, 2), period);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Constructor_RechazaMesesFueraDeRango(int month) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Period(2026, month));

    [Fact]
    [Trait("AC", "AC-50")]
    public void Previous_DeSeptiembreEsAgostoDelMismoAnio() =>
        Assert.Equal(new Period(2026, 8), new Period(2026, 9).Previous());

    [Fact]
    [Trait("AC", "AC-51")]
    public void Previous_DeEneroEsDiciembreDelAnioAnterior() =>
        Assert.Equal(new Period(2026, 12), new Period(2027, 1).Previous());

    [Fact]
    public void Next_DeDiciembreEsEneroDelAnioSiguiente() =>
        Assert.Equal(new Period(2027, 1), new Period(2026, 12).Next());

    [Theory]
    [InlineData(2026, 10, 31)]
    [InlineData(2026, 9, 30)]
    [InlineData(2027, 2, 28)]
    [InlineData(2028, 2, 29)]
    public void FirstDayYLastDay_CubrenElMesCompleto(int year, int month, int lastDay)
    {
        var period = new Period(year, month);

        Assert.Equal(new DateOnly(year, month, 1), period.FirstDay);
        Assert.Equal(new DateOnly(year, month, lastDay), period.LastDay);
    }

    [Fact]
    public void Contains_IncluyeSoloLosDiasDelMes()
    {
        var october = new Period(2026, 10);

        Assert.True(october.Contains(new DateOnly(2026, 10, 1)));
        Assert.True(october.Contains(new DateOnly(2026, 10, 31)));
        Assert.False(october.Contains(new DateOnly(2026, 9, 30)));
        Assert.False(october.Contains(new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void FromDate_DevuelveElMesDeLaFecha() =>
        Assert.Equal(new Period(2026, 10), Period.FromDate(new DateOnly(2026, 10, 31)));

    [Fact]
    public void Comparacion_OrdenaPorAnioYLuegoPorMes()
    {
        var december2026 = new Period(2026, 12);
        var january2027 = new Period(2027, 1);

        Assert.True(december2026 < january2027);
        Assert.True(january2027 > december2026);
        Assert.True(december2026 <= new Period(2026, 12));
        Assert.True(december2026 >= new Period(2026, 12));
        Assert.Equal(0, december2026.CompareTo(new Period(2026, 12)));
        Assert.Equal(new Period(2026, 12), december2026);
    }
}
