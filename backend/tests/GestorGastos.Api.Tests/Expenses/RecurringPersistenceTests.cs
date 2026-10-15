using System.Net;
using System.Net.Http.Json;
using GestorGastos.Api.Tests.Infrastructure;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Api.Tests.Expenses;

public sealed class RecurringPersistenceTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static readonly Period November = new(2026, 11);
    private static readonly Period December = new(2026, 12);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private HttpClient _client = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _client = await Factory.CreateAuthenticatedClientAsync();
    }

    public override ValueTask DisposeAsync()
    {
        _client.Dispose();
        return base.DisposeAsync();
    }

    private async Task<Guid> CreateAsync(string period, string kind, string amount = "30000", string name = "Alquiler")
    {
        var response = await _client.PostAsJsonAsync($"/api/periods/{period}/expenses", new { name, amount, kind }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MonthExpense>(Ct))!.Id;
    }

    private Task<HttpResponseMessage> UpdateAsync(string period, Guid id, object body) =>
        _client.PutAsJsonAsync($"/api/periods/{period}/expenses/{id}", body, Ct);

    private async Task<MonthExpense?> FindInMonthAsync(string period, Guid id)
    {
        var month = await _client.GetFromJsonAsync<MonthView>($"/api/periods/{period}", Ct);
        return month!.Expenses.SingleOrDefault(e => e.Id == id);
    }

    [Fact]
    [Trait("AC", "AC-60")]
    public async Task RecurrenteSinCambios_SeVeEnMesesFuturosSinGuardarRegistros()
    {
        var id = await CreateAsync("2026-10", "Recurring");

        Assert.NotNull(await FindInMonthAsync("2026-12", id));
        Assert.NotNull(await FindInMonthAsync("2026-11", id));
        Assert.Equal(0, await Factory.CountInstancesAsync(id, November));
        Assert.Equal(0, await Factory.CountInstancesAsync(id, December));
    }

    [Fact]
    [Trait("AC", "AC-15")]
    public async Task SoloEsteMes_CambiaSoloElMesVisualizado()
    {
        var id = await CreateAsync("2026-08", "Recurring");

        var response = await UpdateAsync("2026-09", id, new { name = "Alquiler", amount = "35000", scope = "ThisMonth" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("35000.00", (await response.Content.ReadFromJsonAsync<MonthExpense>(Ct))!.Amount);
        Assert.Equal("35000.00", (await FindInMonthAsync("2026-09", id))!.Amount);
        Assert.Equal("30000.00", (await FindInMonthAsync("2026-10", id))!.Amount);
        Assert.Equal(1, await Factory.CountInstancesAsync(id, new Period(2026, 9)));
    }

    [Fact]
    [Trait("AC", "AC-16")]
    public async Task EsteMesYLosSiguientes_NoCambiaLosMesesAnteriores()
    {
        var id = await CreateAsync("2026-08", "Recurring", amount: "25000");

        await UpdateAsync("2026-09", id, new { name = "Alquiler", amount = "30000", scope = "ThisAndFollowing" });

        Assert.Equal("25000.00", (await FindInMonthAsync("2026-08", id))!.Amount);
        Assert.Equal("30000.00", (await FindInMonthAsync("2026-09", id))!.Amount);
        Assert.Equal("30000.00", (await FindInMonthAsync("2027-03", id))!.Amount);
    }

    [Fact]
    [Trait("AC", "AC-64")]
    public async Task EsteMesYLosSiguientes_IgualandoUnMesFuturo_BorraSuRegistro()
    {
        var id = await CreateAsync("2026-09", "Recurring");
        await UpdateAsync("2026-11", id, new { name = "Alquiler", amount = "32000", scope = "ThisMonth" });
        Assert.Equal(1, await Factory.CountInstancesAsync(id, November));

        var response = await UpdateAsync("2026-09", id, new { name = "Alquiler", amount = "32000", scope = "ThisAndFollowing" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("32000.00", (await FindInMonthAsync("2026-11", id))!.Amount);
        Assert.Equal(0, await Factory.CountInstancesAsync(id, November));
    }

    [Fact]
    public async Task Recurrente_SinAlcance_DevuelveBadRequestSinCambios()
    {
        var id = await CreateAsync("2026-10", "Recurring");

        var response = await UpdateAsync("2026-10", id, new { name = "Alquiler", amount = "35000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.True(problem!.Errors.ContainsKey("scope"));
        Assert.Equal("30000.00", (await FindInMonthAsync("2026-10", id))!.Amount);
    }

    [Fact]
    [Trait("AC", "AC-14")]
    public async Task Puntual_SinAlcance_ReflejaLosNuevosValores()
    {
        var id = await CreateAsync("2026-10", "OneOff", amount: "15000", name: "Regalo");

        var response = await UpdateAsync("2026-10", id, new
        {
            name = "Regalo de Ana",
            description = "Aniversario",
            amount = "18000",
            dueDate = "2026-10-25",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expense = await FindInMonthAsync("2026-10", id);
        Assert.Equal(
            ("Regalo de Ana", "Aniversario", "18000.00", new DateOnly(2026, 10, 25)),
            (expense!.Name, expense.Description, expense.Amount, expense.DueDate!.Value));
    }

    [Fact]
    public async Task Editar_ConDatosInvalidos_DevuelveBadRequest()
    {
        var id = await CreateAsync("2026-10", "OneOff");

        var response = await UpdateAsync("2026-10", id, new { name = "", amount = "0", dueDate = "2026-09-01" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.Equal(["amount", "dueDate", "name"], problem!.Errors.Keys.Order());
    }

    [Theory]
    [InlineData("OneOff", "2026-11")]
    [InlineData("Recurring", "2026-09")]
    public async Task Editar_EnUnMesEnQueNoAparece_DevuelveNotFound(string kind, string period)
    {
        var id = await CreateAsync("2026-10", kind);

        var response = await UpdateAsync(period, id, new { name = "Otro", amount = "1", scope = "ThisMonth" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Editar_UnGastoInexistente_DevuelveNotFound() =>
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await UpdateAsync("2026-10", Guid.NewGuid(), new { name = "Otro", amount = "1" })).StatusCode);

    private sealed record MonthView(List<MonthExpense> Expenses);

    private sealed record MonthExpense(Guid Id, string Name, string? Description, string Amount, DateOnly? DueDate);

    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
