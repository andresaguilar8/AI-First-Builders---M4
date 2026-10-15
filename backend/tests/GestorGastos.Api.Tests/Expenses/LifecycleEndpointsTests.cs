using System.Net;
using System.Net.Http.Json;
using GestorGastos.Api.Tests.Infrastructure;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Tests.Expenses;

public sealed class LifecycleEndpointsTests(ApiFactory factory) : ApiTestBase(factory)
{
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

    private Task<HttpResponseMessage> DeleteAsync(string period, Guid id) =>
        _api.Client.DeleteAsync($"/api/periods/{period}/expenses/{id}", Ct);

    private Task<HttpResponseMessage> DiscontinueAsync(string period, Guid id) =>
        _api.Client.PostAsync($"/api/periods/{period}/expenses/{id}/discontinue", null, Ct);

    private Task<HttpResponseMessage> ConvertAsync(string period, Guid id, string? to) =>
        _api.Client.PostAsJsonAsync($"/api/periods/{period}/expenses/{id}/convert", new { to }, Ct);

    private Task<int> CountExpensesAsync() => Factory.WithDbAsync(db => db.Expenses.CountAsync(Ct));

    [Fact]
    [Trait("AC", "AC-23")]
    public async Task EliminarPuntual_NoApareceMasEnElMes()
    {
        var id = await _api.CreateAsync("2026-10", "OneOff");

        var response = await DeleteAsync("2026-10", id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await _api.FindInMonthAsync("2026-10", id));
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    [Trait("AC", "AC-23")]
    public async Task EliminarPuntualPagado_EsDefinitivo()
    {
        var id = await _api.CreateAsync("2026-10", "OneOff");
        await _api.PayAsync("2026-10", id);

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync("2026-10", id)).StatusCode);
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    public async Task EliminarRecurrente_DevuelveConflict()
    {
        var id = await _api.CreateAsync("2026-10", "Recurring");

        Assert.Equal(HttpStatusCode.Conflict, (await DeleteAsync("2026-10", id)).StatusCode);
        Assert.NotNull(await _api.FindInMonthAsync("2026-10", id));
    }

    [Fact]
    [Trait("AC", "AC-21")]
    public async Task Baja_ConservaLosMesesAnterioresYNoApareceDesdeElMesVisualizado()
    {
        var id = await _api.CreateAsync("2026-01", "Recurring");
        await _api.PayAsync("2026-08", id);

        var response = await DiscontinueAsync("2026-09", id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(await _api.FindInMonthAsync("2026-01", id));
        Assert.Equal("Pagado", (await _api.FindInMonthAsync("2026-08", id))!.Status);
        Assert.Null(await _api.FindInMonthAsync("2026-09", id));
        Assert.Null(await _api.FindInMonthAsync("2027-01", id));
    }

    [Fact]
    [Trait("AC", "AC-65")]
    public async Task BajaViendoOctubreConPagoEnNoviembre_NoDejaRegistrosEnOctubreNiNoviembre()
    {
        var id = await _api.CreateAsync("2026-09", "Recurring");
        await _api.PayAsync("2026-10", id);
        await _api.PayAsync("2026-11", id);

        await DiscontinueAsync("2026-10", id);

        Assert.Equal(0, await Factory.CountInstancesAsync(id, new Period(2026, 10)));
        Assert.Equal(0, await Factory.CountInstancesAsync(id, new Period(2026, 11)));
        Assert.NotNull(await _api.FindInMonthAsync("2026-09", id));
    }

    [Fact]
    public async Task BajaEnElMesDeInicio_EliminaElGasto()
    {
        var id = await _api.CreateAsync("2026-10", "Recurring");

        Assert.Equal(HttpStatusCode.NoContent, (await DiscontinueAsync("2026-10", id)).StatusCode);
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    public async Task BajaDeUnPuntual_DevuelveConflict()
    {
        var id = await _api.CreateAsync("2026-10", "OneOff");

        Assert.Equal(HttpStatusCode.Conflict, (await DiscontinueAsync("2026-10", id)).StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-18")]
    public async Task PuntualARecurrente_ApareceEnElMesYEnElSiguienteConOtroId()
    {
        var id = await _api.CreateAsync("2026-10", "OneOff", amount: "15000", name: "Regalo", dueDate: "2026-10-20");

        var response = await ConvertAsync("2026-10", id, "Recurring");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseApi.MonthExpense>(Ct);
        Assert.NotEqual(id, created!.Id);
        Assert.Equal(("Recurring", "Regalo", "15000.00"), (created.Kind, created.Name, created.Amount));
        Assert.Null(await _api.FindInMonthAsync("2026-10", id));
        Assert.NotNull(await _api.FindInMonthAsync("2026-10", created.Id));
        Assert.Equal(new DateOnly(2026, 11, 20), (await _api.FindInMonthAsync("2026-11", created.Id))!.DueDate);
    }

    [Fact]
    [Trait("AC", "AC-19")]
    [Trait("AC", "AC-20")]
    public async Task RecurrenteAPuntual_TerminaElMesAnteriorYNoApareceEnNoviembreAunquePagado()
    {
        var id = await _api.CreateAsync("2026-08", "Recurring");
        await _api.PayAsync("2026-10", id);
        await _api.PayAsync("2026-11", id);

        var response = await ConvertAsync("2026-10", id, "OneOff");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseApi.MonthExpense>(Ct);
        Assert.Equal(("OneOff", "Pagado"), (created!.Kind, created.Status));
        Assert.NotNull(await _api.FindInMonthAsync("2026-08", id));
        Assert.NotNull(await _api.FindInMonthAsync("2026-09", id));
        Assert.Null(await _api.FindInMonthAsync("2026-10", id));
        var november = await _api.Client.GetFromJsonAsync<MonthTotals>("/api/periods/2026-11", Ct);
        Assert.Empty(november!.Expenses);
        Assert.Equal("0.00", november.Total);
        Assert.Equal(0, await Factory.CountInstancesAsync(id, new Period(2026, 11)));
    }

    [Theory]
    [InlineData("OneOff", "OneOff")]
    [InlineData("Recurring", "Recurring")]
    public async Task ConvertirAlMismoTipo_DevuelveConflict(string kind, string to)
    {
        var id = await _api.CreateAsync("2026-10", kind);

        Assert.Equal(HttpStatusCode.Conflict, (await ConvertAsync("2026-10", id, to)).StatusCode);
    }

    [Fact]
    public async Task ConvertirSinTipo_DevuelveBadRequest()
    {
        var id = await _api.CreateAsync("2026-10", "OneOff");

        Assert.Equal(HttpStatusCode.BadRequest, (await ConvertAsync("2026-10", id, null)).StatusCode);
    }

    [Theory]
    [InlineData("2026-09")]
    [InlineData("2026-12")]
    public async Task OperarSobreUnRecurrenteFueraDeSusMeses_DevuelveNotFound(string period)
    {
        var id = await _api.CreateAsync("2026-10", "Recurring");
        await DiscontinueAsync("2026-11", id);

        Assert.Equal(HttpStatusCode.NotFound, (await DiscontinueAsync(period, id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ConvertAsync(period, id, "OneOff")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(period, id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.PayAsync(period, id)).StatusCode);
    }

    private sealed record MonthTotals(List<ExpenseApi.MonthExpense> Expenses, string Total);
}
