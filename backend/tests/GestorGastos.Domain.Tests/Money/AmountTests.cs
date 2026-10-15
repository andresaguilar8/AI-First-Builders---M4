using GestorGastos.Domain.Common;
using GestorGastos.Domain.Money;

namespace GestorGastos.Domain.Tests.Money;

public class AmountTests
{
    [Theory]
    [Trait("AC", "AC-05")]
    [InlineData("0")]
    [InlineData("-100")]
    [InlineData("10.999")]
    public void Create_RechazaCeroNegativosYMasDeDosDecimales(string value)
    {
        var error = Assert.Throws<DomainValidationException>(() => Amount.Create(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.True(error.Errors.ContainsKey("amount"));
    }

    [Theory]
    [Trait("AC", "AC-05")]
    [InlineData("0")]
    [InlineData("0.00")]
    [InlineData("-100")]
    [InlineData("10.999")]
    [InlineData("1,5")]
    [InlineData("1.234,56")]
    [InlineData("")]
    [InlineData(" 12")]
    [InlineData("abc")]
    public void Parse_RechazaMontosInvalidos(string value)
    {
        var error = Assert.Throws<DomainValidationException>(() => Amount.Parse(value));

        Assert.True(error.Errors.ContainsKey("amount"));
    }

    [Fact]
    [Trait("AC", "AC-06")]
    public void Parse_AceptaMontoConPuntoDecimal()
    {
        var amount = Amount.Parse("1234.56");

        Assert.Equal(1234.56m, amount.Value);
        Assert.Equal("1234.56", amount.ToString());
    }

    [Fact]
    [Trait("AC", "AC-62")]
    public void Parse_AceptaElMontoMaximo()
    {
        var amount = Amount.Parse("999999999999.99");

        Assert.Equal(Amount.Max, amount.Value);
    }

    [Fact]
    [Trait("AC", "AC-62")]
    public void Create_RechazaUnBillonOMas()
    {
        Assert.Throws<DomainValidationException>(() => Amount.Create(1_000_000_000_000m));
        Assert.Throws<DomainValidationException>(() => Amount.Parse("1000000000000"));
    }

    [Theory]
    [InlineData("15", "15.00")]
    [InlineData("15.5", "15.50")]
    [InlineData("0.01", "0.01")]
    public void ToString_UsaPuntoYDosDecimalesSinSeparadorDeMiles(string value, string expected) =>
        Assert.Equal(expected, Amount.Parse(value).ToString());
}
