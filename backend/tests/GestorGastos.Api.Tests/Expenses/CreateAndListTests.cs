using System.Net;
using System.Net.Http.Json;
using GestorGastos.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Tests.Expenses;

public sealed class CreateAndListTests(ApiFactory factory) : ApiTestBase(factory)
{
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

    private Task<HttpResponseMessage> CreateAsync(string period, object body) =>
        _client.PostAsJsonAsync($"/api/periods/{period}/expenses", body, Ct);

    private async Task<MonthView> GetMonthAsync(string period) =>
        (await _client.GetFromJsonAsync<MonthView>($"/api/periods/{period}", Ct))!;

    private Task<int> CountExpensesAsync() => Factory.WithDbAsync(db => db.Expenses.CountAsync(Ct));

    [Fact]
    [Trait("AC", "AC-06")]
    public async Task Crear_ConMontoCanonico_DevuelveCreatedYApareceEnElMes()
    {
        var response = await CreateAsync("2026-10", new { name = "Luz", amount = "1234.56", kind = "OneOff" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MonthExpense>(Ct);
        Assert.Equal("1234.56", created!.Amount);
        var listed = Assert.Single((await GetMonthAsync("2026-10")).Expenses);
        Assert.Equal(created.Id, listed.Id);
        Assert.Equal("Luz", listed.Name);
        Assert.Equal("1234.56", listed.Amount);
        Assert.Equal("OneOff", listed.Kind);
        Assert.Equal("AlDia", listed.Status);
    }

    [Fact]
    [Trait("AC", "AC-02")]
    public async Task Crear_PuntualConTodosLosCampos_LosDevuelveEnElMes()
    {
        var categoryId = await Factory.WithDbAsync(async db =>
        {
            var category = new GestorGastos.Domain.Categories.Category(Guid.NewGuid(), "Varios", "varios");
            db.Categories.Add(category);
            await db.SaveChangesAsync(Ct);
            return category.Id;
        });

        var response = await CreateAsync("2026-10", new
        {
            name = "Regalo",
            description = "Cumpleaños",
            amount = "15000",
            categoryId,
            dueDate = "2026-10-20",
            kind = "OneOff",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var expense = Assert.Single((await GetMonthAsync("2026-10")).Expenses);
        Assert.Equal("Cumpleaños", expense.Description);
        Assert.Equal("15000.00", expense.Amount);
        Assert.Equal(new CategoryRef(categoryId, "Varios"), expense.Category);
        Assert.Equal(new DateOnly(2026, 10, 20), expense.DueDate);
        Assert.Null(expense.PaidOn);
    }

    [Fact]
    [Trait("AC", "AC-01")]
    public async Task Crear_Recurrente_ApareceComoRecurrente()
    {
        var response = await CreateAsync("2026-10", new { name = "Alquiler", amount = "300000", kind = "Recurring" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var expense = Assert.Single((await GetMonthAsync("2026-10")).Expenses);
        Assert.Equal("Recurring", expense.Kind);
        Assert.Equal("300000.00", expense.Amount);
    }

    [Fact]
    [Trait("AC", "AC-07")]
    public async Task Crear_RecurrenteConVencimientoEnElMesSiguiente_ConservaLaFecha()
    {
        await CreateAsync("2026-10", new { name = "Seguro", amount = "8000", dueDate = "2026-11-05", kind = "Recurring" });

        var expense = Assert.Single((await GetMonthAsync("2026-10")).Expenses);
        Assert.Equal(new DateOnly(2026, 11, 5), expense.DueDate);
    }

    [Fact]
    [Trait("AC", "AC-04")]
    public async Task Crear_ViendoDiciembre_ApareceEnDiciembreYNoEnOctubre()
    {
        await CreateAsync("2026-12", new { name = "Regalo", amount = "15000", kind = "OneOff" });

        Assert.Single((await GetMonthAsync("2026-12")).Expenses);
        Assert.Empty((await GetMonthAsync("2026-10")).Expenses);
    }

    [Theory]
    [Trait("AC", "AC-05")]
    [Trait("AC", "AC-62")]
    [InlineData("0")]
    [InlineData("-100")]
    [InlineData("10.999")]
    [InlineData("1000000000000")]
    public async Task Crear_ConMontoInvalido_DevuelveBadRequestYNoCreaNada(string amount)
    {
        var response = await CreateAsync("2026-10", new { name = "Luz", amount, kind = "OneOff" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.True(problem!.Errors.ContainsKey("amount"));
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    [Trait("AC", "AC-62")]
    public async Task Crear_ConMontoMaximo_DevuelveCreated()
    {
        var response = await CreateAsync("2026-10", new { name = "Casa", amount = "999999999999.99", kind = "OneOff" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("999999999999.99", Assert.Single((await GetMonthAsync("2026-10")).Expenses).Amount);
    }

    [Fact]
    [Trait("AC", "AC-03")]
    public async Task Crear_SinNombreNiMonto_DevuelveErroresPorCampoYNoCreaNada()
    {
        var response = await CreateAsync("2026-10", new { name = "  ", kind = "OneOff" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.True(problem!.Errors.ContainsKey("name"));
        Assert.True(problem.Errors.ContainsKey("amount"));
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    public async Task Crear_ConNombreDe101Caracteres_DevuelveBadRequest()
    {
        var response = await CreateAsync("2026-10", new { name = new string('a', 101), amount = "100", kind = "OneOff" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    [Trait("AC", "AC-08")]
    public async Task Crear_ConVencimientoAnteriorAlMes_DevuelveBadRequestYNoCreaNada()
    {
        var response = await CreateAsync("2026-10", new { name = "Luz", amount = "100", dueDate = "2026-09-30", kind = "Recurring" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Ct);
        Assert.True(problem!.Errors.ContainsKey("dueDate"));
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    public async Task Crear_SinTipoOConCategoriaInexistente_DevuelveBadRequest()
    {
        var withoutKind = await CreateAsync("2026-10", new { name = "Luz", amount = "100" });
        var unknownCategory = await CreateAsync("2026-10", new { name = "Luz", amount = "100", categoryId = Guid.NewGuid(), kind = "OneOff" });

        Assert.True((await withoutKind.Content.ReadFromJsonAsync<ValidationProblem>(Ct))!.Errors.ContainsKey("kind"));
        Assert.True((await unknownCategory.Content.ReadFromJsonAsync<ValidationProblem>(Ct))!.Errors.ContainsKey("categoryId"));
        Assert.Equal(0, await CountExpensesAsync());
    }

    [Fact]
    public async Task ConsultarMes_SinGastos_DevuelveListaVaciaTotalesEnCeroYHoyEnBuenosAires()
    {
        // 01:00 UTC del 06/10 = 22:00 del 05/10 en Buenos Aires.
        Factory.Time.SetUtcNow(new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero));

        var month = await GetMonthAsync("2026-11");

        Assert.Equal("2026-11", month.Period);
        Assert.Equal(new DateOnly(2026, 10, 5), month.Today);
        Assert.Empty(month.Expenses);
        Assert.Equal("0.00", month.Total);
        Assert.Equal("0.00", month.PendingTotal);
        Assert.Empty(month.ByCategory);
    }

    [Fact]
    public async Task ConsultarMes_ConPeriodoInvalido_DevuelveBadRequest() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/periods/2026-13", Ct)).StatusCode);

    private sealed record MonthView(
        string Period,
        DateOnly Today,
        List<MonthExpense> Expenses,
        string Total,
        string PendingTotal,
        List<CategoryTotal> ByCategory);

    private sealed record MonthExpense(
        Guid Id,
        string Kind,
        string Name,
        string? Description,
        string Amount,
        CategoryRef? Category,
        DateOnly? DueDate,
        DateOnly? PaidOn,
        string Status);

    private sealed record CategoryRef(Guid Id, string Name);

    private sealed record CategoryTotal(Guid? CategoryId, string CategoryName, string Total);

    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
