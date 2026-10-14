using GestorGastos.Domain.Common;

namespace GestorGastos.Domain.Tests.Common;

public class DomainValidationExceptionTests
{
    [Fact]
    public void ConUnCampo_ExponeElMensajeBajoEseCampo()
    {
        var error = new DomainValidationException("amount", "El monto es obligatorio.");

        Assert.Equal(["El monto es obligatorio."], error.Errors["amount"]);
        Assert.Contains("amount", error.Message);
    }
}
