using System.Net;
using System.Net.Http.Json;
using GestorGastos.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Tests.Categories;

public sealed class CategoryEndpointsTests(ApiFactory factory) : ApiTestBase(factory)
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

    private Task<HttpResponseMessage> PostCategoryAsync(string name) =>
        _client.PostAsJsonAsync("/api/categories", new { name }, Ct);

    private async Task<Guid> CreateCategoryAsync(string name)
    {
        var response = await PostCategoryAsync(name);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Category>(Ct))!.Id;
    }

    private Task<HttpResponseMessage> RenameAsync(Guid id, string name) =>
        _client.PutAsJsonAsync($"/api/categories/{id}", new { name }, Ct);

    private async Task<List<Category>> ListAsync() =>
        (await _client.GetFromJsonAsync<List<Category>>("/api/categories", Ct))!;

    private async Task<Guid> CreateExpenseAsync(string period, string kind, Guid? categoryId, string amount = "1000")
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/periods/{period}/expenses", new { name = "Gasto", amount, kind, categoryId }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MonthExpense>(Ct))!.Id;
    }

    private async Task<MonthView> GetMonthAsync(string period) =>
        (await _client.GetFromJsonAsync<MonthView>($"/api/periods/{period}", Ct))!;

    private async Task<string?> CategoryNameInAsync(string period, Guid expenseId) =>
        (await GetMonthAsync(period)).Expenses.Single(e => e.Id == expenseId).Category?.Name;

    [Fact]
    [Trait("AC", "AC-25")]
    public async Task Crear_QuedaDisponibleParaAsociarlaAGastos()
    {
        var response = await PostCategoryAsync("  Educación ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<Category>(Ct);
        Assert.Equal("Educación", category!.Name);
        Assert.Equal([category], await ListAsync());
        var expenseId = await CreateExpenseAsync("2026-10", "OneOff", category.Id);
        Assert.Equal("Educación", await CategoryNameInAsync("2026-10", expenseId));
    }

    [Fact]
    public async Task Listar_OrdenaPorNombre()
    {
        await CreateCategoryAsync("Vivienda");
        await CreateCategoryAsync("Educación");
        await CreateCategoryAsync("servicios");

        Assert.Equal(["Educación", "servicios", "Vivienda"], (await ListAsync()).Select(c => c.Name));
    }

    [Theory]
    [Trait("AC", "AC-26")]
    [InlineData("Educación")]
    [InlineData("educacion")]
    [InlineData("EDUCACIÓN")]
    public async Task CrearORenombrarConUnNombreExistente_DevuelveConflict(string name)
    {
        await CreateCategoryAsync("Educación");
        var health = await CreateCategoryAsync("Salud");

        Assert.Equal(HttpStatusCode.Conflict, (await PostCategoryAsync(name)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await RenameAsync(health, name)).StatusCode);
        Assert.Equal(["Educación", "Salud"], (await ListAsync()).Select(c => c.Name));
    }

    [Theory]
    [Trait("AC", "AC-27")]
    [InlineData("Sin categoría")]
    [InlineData("sin categoria")]
    public async Task CrearORenombrarComoSinCategoria_DevuelveConflict(string name)
    {
        var other = await CreateCategoryAsync("Varios");

        Assert.Equal(HttpStatusCode.Conflict, (await PostCategoryAsync(name)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await RenameAsync(other, name)).StatusCode);
        Assert.Equal(["Varios"], (await ListAsync()).Select(c => c.Name));
    }

    [Fact]
    public async Task Renombrar_ConSuMismoNombreEnOtraForma_SeAcepta()
    {
        var id = await CreateCategoryAsync("educacion");

        var response = await RenameAsync(id, "Educación");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Educación", (await response.Content.ReadFromJsonAsync<Category>(Ct))!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CrearSinNombre_DevuelveBadRequest(string name) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await PostCategoryAsync(name)).StatusCode);

    [Fact]
    public async Task CrearConNombreDe51Caracteres_DevuelveBadRequest() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await PostCategoryAsync(new string('a', 51))).StatusCode);

    [Fact]
    [Trait("AC", "AC-28")]
    public async Task Renombrar_SeVeEnTodosLosMeses()
    {
        var services = await CreateCategoryAsync("Servicios");
        var august = await CreateExpenseAsync("2026-08", "OneOff", services);
        var recurring = await CreateExpenseAsync("2026-08", "Recurring", services);

        Assert.Equal(HttpStatusCode.OK, (await RenameAsync(services, "Servicios del hogar")).StatusCode);

        Assert.Equal("Servicios del hogar", await CategoryNameInAsync("2026-08", august));
        Assert.Equal("Servicios del hogar", await CategoryNameInAsync("2026-08", recurring));
        Assert.Equal("Servicios del hogar", await CategoryNameInAsync("2026-10", recurring));
    }

    [Fact]
    [Trait("AC", "AC-29")]
    public async Task EliminarSinGastos_NoApareceMas()
    {
        var id = await CreateCategoryAsync("Varios");

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/categories/{id}", Ct)).StatusCode);
        Assert.Empty(await ListAsync());
    }

    [Fact]
    [Trait("AC", "AC-30")]
    public async Task EliminarConGastos_LosPasaASinCategoriaEnTodosLosMeses()
    {
        var leisure = await CreateCategoryAsync("Ocio");
        var august = await CreateExpenseAsync("2026-08", "OneOff", leisure);
        var recurring = await CreateExpenseAsync("2026-08", "Recurring", leisure);
        // Un registro propio de octubre (versión + instancia con la categoría).
        await _client.PutAsJsonAsync(
            $"/api/periods/2026-10/expenses/{recurring}",
            new { name = "Gasto", amount = "2000", categoryId = leisure, scope = "ThisMonth" },
            Ct);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/categories/{leisure}", Ct)).StatusCode);

        Assert.Null(await CategoryNameInAsync("2026-08", august));
        Assert.Null(await CategoryNameInAsync("2026-08", recurring));
        Assert.Null(await CategoryNameInAsync("2026-10", recurring));
        Assert.Null(await CategoryNameInAsync("2026-11", recurring));
        var summary = Assert.Single((await GetMonthAsync("2026-10")).ByCategory);
        Assert.Equal((null, "Sin categoría"), (summary.CategoryId, summary.CategoryName));
    }

    [Fact]
    [Trait("AC", "AC-31")]
    public async Task AsociarCategoriaAUnGastoSinCategoria_LaMuestra()
    {
        var category = await CreateCategoryAsync("Varios");
        var expense = await CreateExpenseAsync("2026-10", "OneOff", null);

        await _client.PutAsJsonAsync(
            $"/api/periods/2026-10/expenses/{expense}", new { name = "Gasto", amount = "1000", categoryId = category }, Ct);

        Assert.Equal("Varios", await CategoryNameInAsync("2026-10", expense));
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task OperarSobreUnaCategoriaInexistente_DevuelveNotFound(string method)
    {
        var url = $"/api/categories/{Guid.NewGuid()}";
        var response = method == "PUT"
            ? await _client.PutAsJsonAsync(url, new { name = "Otra" }, Ct)
            : await _client.DeleteAsync(url, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-32")]
    [Trait("AC", "AC-33")]
    [Trait("AC", "AC-34")]
    public async Task ConsultarMes_DevuelveTotalesYResumenPorCategoria()
    {
        var housing = await CreateCategoryAsync("Vivienda");
        var services = await CreateCategoryAsync("Servicios");
        var paid = await CreateExpenseAsync("2026-09", "OneOff", housing, "200000");
        await _client.PutAsync($"/api/periods/2026-09/expenses/{paid}/payment", null, Ct);
        await CreateExpenseAsync("2026-09", "Recurring", housing, "100000");
        await CreateExpenseAsync("2026-09", "OneOff", services, "100000");
        await CreateExpenseAsync("2026-09", "OneOff", null, "50000");

        var month = await GetMonthAsync("2026-09");

        Assert.Equal(
            [new(services, "Servicios", "100000.00"), new(housing, "Vivienda", "300000.00"), new(null, "Sin categoría", "50000.00")],
            month.ByCategory);
        Assert.Equal(("450000.00", "250000.00"), (month.Total, month.PendingTotal));
        Assert.Equal(1, await Factory.WithDbAsync(db => db.Categories.CountAsync(c => c.Id == housing, Ct)));
    }

    private sealed record Category(Guid Id, string Name);

    private sealed record CategoryRef(Guid Id, string Name);

    private sealed record MonthExpense(Guid Id, CategoryRef? Category);

    private sealed record CategoryTotal(Guid? CategoryId, string CategoryName, string Total);

    private sealed record MonthView(List<MonthExpense> Expenses, string Total, string PendingTotal, List<CategoryTotal> ByCategory);
}
