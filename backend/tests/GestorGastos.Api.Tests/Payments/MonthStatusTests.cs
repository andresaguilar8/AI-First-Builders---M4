using System.Net;
using GestorGastos.Api.Tests.Infrastructure;

namespace GestorGastos.Api.Tests.Payments;

public sealed class MonthStatusTests(ApiFactory factory) : ApiTestBase(factory)
{
    private ExpenseApi _api = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _api = new ExpenseApi(await Factory.CreateAuthenticatedClientAsync());
    }

    public override ValueTask DisposeAsync()
    {
        _api.Client.Dispose();
        return base.DisposeAsync();
    }

    [Theory]
    [Trait("AC", "AC-43")]
    [Trait("AC", "AC-44")]
    [Trait("AC", "AC-45")]
    [Trait("AC", "AC-46")]
    [Trait("AC", "AC-47")]
    [InlineData("2026-10-09", "AlDia")]
    [InlineData(null, "AlDia")]
    [InlineData("2026-10-08", "ProximoAVencer")]
    [InlineData("2026-10-06", "ProximoAVencer")]
    [InlineData("2026-10-05", "VenceHoy")]
    [InlineData("2026-10-04", "Vencido")]
    public async Task ConsultarMes_DevuelveElEstadoDeVencimiento(string? dueDate, string status)
    {
        var id = await _api.CreateAsync("2026-10", "OneOff", dueDate: dueDate);

        Assert.Equal(status, (await _api.FindInMonthAsync("2026-10", id))!.Status);
    }

    [Theory]
    [InlineData("OneOff")]
    [InlineData("Recurring")]
    public async Task MarcarYDesmarcar_ElMesMuestraPagadoYLuegoSuEstadoDeVencimiento(string kind)
    {
        var id = await _api.CreateAsync("2026-10", kind, dueDate: "2026-10-04");

        Assert.Equal(HttpStatusCode.OK, (await _api.PayAsync("2026-10", id)).StatusCode);
        var paid = await _api.FindInMonthAsync("2026-10", id);
        Assert.Equal(("Pagado", new DateOnly(2026, 10, 5)), (paid!.Status, paid.PaidOn!.Value));

        Assert.Equal(HttpStatusCode.OK, (await _api.UnpayAsync("2026-10", id)).StatusCode);
        var pending = await _api.FindInMonthAsync("2026-10", id);
        Assert.Equal(("Vencido", (DateOnly?)null), (pending!.Status, pending.PaidOn));
    }
}
