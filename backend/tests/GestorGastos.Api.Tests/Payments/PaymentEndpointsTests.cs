using System.Net;
using System.Net.Http.Json;
using GestorGastos.Api.Tests.Infrastructure;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Api.Tests.Payments;

public sealed class PaymentEndpointsTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static readonly Period September = new(2026, 9);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

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

    private async Task<DueToday> GetAlertAsync() =>
        (await _api.Client.GetFromJsonAsync<DueToday>("/api/alerts/due-today", Ct))!;

    [Fact]
    [Trait("AC", "AC-61")]
    public async Task PagarYLuegoEditarSoloEsteMes_GuardaExactamenteUnRegistro()
    {
        var id = await _api.CreateAsync("2026-09", "Recurring");
        Assert.Equal(0, await Factory.CountInstancesAsync(id, September));

        await _api.PayAsync("2026-09", id);
        var edit = await _api.Client.PutAsJsonAsync(
            $"/api/periods/2026-09/expenses/{id}", new { name = "Internet", amount = "21000", scope = "ThisMonth" }, Ct);

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal(1, await Factory.CountInstancesAsync(id, September));
        var expense = await _api.FindInMonthAsync("2026-09", id);
        Assert.Equal(("21000.00", "Pagado"), (expense!.Amount, expense.Status));
    }

    [Fact]
    [Trait("AC", "AC-68")]
    public async Task PagarYDesmarcar_NoDejaRegistros()
    {
        var id = await _api.CreateAsync("2026-09", "Recurring");

        var pay = await _api.PayAsync("2026-09", id);
        Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
        Assert.Equal(1, await Factory.CountInstancesAsync(id, September));
        await _api.UnpayAsync("2026-09", id);

        Assert.Equal(0, await Factory.CountInstancesAsync(id, September));
        Assert.Null((await _api.FindInMonthAsync("2026-09", id))!.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-48")]
    public async Task A22HorasDeBuenosAires_ElVencimientoDeHoySigueSiendoHoy()
    {
        var id = await _api.CreateAsync("2026-10", "OneOff", dueDate: "2026-10-05");

        Factory.Time.SetUtcNow(new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero));

        Assert.Equal("VenceHoy", (await _api.FindInMonthAsync("2026-10", id))!.Status);
        Assert.Equal(new DateOnly(2026, 10, 5), (await GetAlertAsync()).Today);
    }

    [Fact]
    [Trait("AC", "AC-36")]
    public async Task Vencido_ConFechaReal_LaRegistra()
    {
        var id = await _api.CreateAsync("2026-10", "OneOff", dueDate: "2026-10-01");

        var response = await _api.PayAsync("2026-10", id, "2026-10-03");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new DateOnly(2026, 10, 3), (await response.Content.ReadFromJsonAsync<ExpenseApi.MonthExpense>(Ct))!.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-38")]
    public async Task Vencido_ConFechaFutura_DevuelveBadRequestYSiguePendiente()
    {
        var id = await _api.CreateAsync("2026-10", "Recurring", dueDate: "2026-10-01");

        var response = await _api.PayAsync("2026-10", id, "2026-10-06");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Vencido", (await _api.FindInMonthAsync("2026-10", id))!.Status);
        Assert.Equal(0, await Factory.CountInstancesAsync(id, new Period(2026, 10)));
    }

    [Fact]
    [Trait("AC", "AC-35")]
    public async Task NoVencido_IgnoraLaFechaIndicadaYRegistraHoy()
    {
        var id = await _api.CreateAsync("2026-12", "OneOff", dueDate: "2026-12-10");

        await _api.PayAsync("2026-12", id, "2026-10-01");

        Assert.Equal(new DateOnly(2026, 10, 5), (await _api.FindInMonthAsync("2026-12", id))!.PaidOn);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task PagoEnUnMesEnQueNoAparece_DevuelveNotFound(string method)
    {
        var id = await _api.CreateAsync("2026-10", "OneOff");

        var response = method == "PUT" ? await _api.PayAsync("2026-11", id) : await _api.UnpayAsync("2026-11", id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-40")]
    [Trait("AC", "AC-41")]
    [Trait("AC", "AC-67")]
    public async Task Alerta_CuentaLosPendientesDeCualquierMesQueVencenHoy()
    {
        await _api.CreateAsync("2026-10", "OneOff", name: "Luz", dueDate: "2026-10-05");
        await _api.CreateAsync("2026-09", "OneOff", name: "Gas", dueDate: "2026-10-05");
        var projected = await _api.CreateAsync("2026-08", "Recurring", name: "Internet", dueDate: "2026-08-05");
        await _api.CreateAsync("2026-10", "OneOff", name: "Vencido", dueDate: "2026-10-04");
        await _api.CreateAsync("2026-10", "OneOff", name: "Mañana", dueDate: "2026-10-06");

        var alert = await GetAlertAsync();
        Assert.Equal((new DateOnly(2026, 10, 5), 3), (alert.Today, alert.Count));

        await _api.PayAsync("2026-10", projected);
        Assert.Equal(2, (await GetAlertAsync()).Count);
    }

    [Fact]
    [Trait("AC", "AC-42")]
    public async Task Alerta_SinPendientesQueVenzanHoy_EsCero()
    {
        var id = await _api.CreateAsync("2026-10", "OneOff", dueDate: "2026-10-05");
        await _api.PayAsync("2026-10", id);

        Assert.Equal(0, (await GetAlertAsync()).Count);
    }

    private sealed record DueToday(DateOnly Today, int Count);
}
