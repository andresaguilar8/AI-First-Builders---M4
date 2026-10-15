using GestorGastos.Domain.Categories;
using GestorGastos.Domain.Common;

namespace GestorGastos.Domain.Tests.Categories;

public class CategoryNameTests
{
    [Theory]
    [InlineData("Educación", "educacion")]
    [InlineData("  EDUCACIÓN  ", "educacion")]
    [InlineData("Gastos   del  hogar", "gastos del hogar")]
    [InlineData("Ñandú", "nandu")]
    public void Create_NormalizaSinMayusculasAcentosNiEspaciosRepetidos(string name, string normalized) =>
        Assert.Equal(normalized, CategoryName.Create(name).Normalized);

    [Fact]
    public void Create_RecortaLosEspaciosDelNombreVisible() =>
        Assert.Equal("Educación", CategoryName.Create("  Educación ").Value);

    [Theory]
    [Trait("AC", "AC-26")]
    [InlineData("Educación")]
    [InlineData("educacion")]
    [InlineData("EDUCACIÓN")]
    public void NombresQueSoloDifierenEnMayusculasOAcentos_Colisionan(string name) =>
        Assert.Equal(CategoryName.Create("Educación").Normalized, CategoryName.Create(name).Normalized);

    [Fact]
    public void EspaciosRepetidosInternos_Colisionan() =>
        Assert.Equal(CategoryName.Create("Gastos hogar").Normalized, CategoryName.Create("Gastos  hogar").Normalized);

    [Theory]
    [Trait("AC", "AC-27")]
    [InlineData("Sin categoría")]
    [InlineData("sin categoria")]
    [InlineData("  SIN   CATEGORÍA ")]
    public void SinCategoria_EstaReservado(string name) =>
        Assert.True(CategoryName.Create(name).IsReserved);

    [Fact]
    public void OtroNombre_NoEstaReservado() =>
        Assert.False(CategoryName.Create("Sin categorizar").IsReserved);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NombreVacio_SeRechaza(string? name) =>
        Assert.True(Assert.Throws<DomainValidationException>(() => CategoryName.Create(name)).Errors.ContainsKey("name"));

    [Fact]
    public void NombreDeHasta50Caracteres_SeAceptaYDe51SeRechaza()
    {
        Assert.Equal(50, CategoryName.Create(new string('a', 50)).Value.Length);
        Assert.Throws<DomainValidationException>(() => CategoryName.Create(new string('a', 51)));
    }

    [Fact]
    public void Rename_CambiaNombreYNormalizado()
    {
        var category = Category.Create(CategoryName.Create("Servicios"));

        category.Rename(CategoryName.Create("Servicios del hogar"));

        Assert.Equal(("Servicios del hogar", "servicios del hogar"), (category.Name, category.NormalizedName));
    }
}
