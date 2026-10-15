using System.Net;
using System.Net.Http.Json;

namespace GestorGastos.Api.Tests.Infrastructure;

/// <summary>Atajos sobre la API de gastos para armar escenarios en los tests de integración.</summary>
public sealed class ExpenseApi(HttpClient client)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public HttpClient Client => client;

    public async Task<Guid> CreateAsync(
        string period,
        string kind,
        string amount = "20000",
        string name = "Internet",
        string? dueDate = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/periods/{period}/expenses", new { name, amount, kind, dueDate }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MonthExpense>(Ct))!.Id;
    }

    public async Task<MonthExpense?> FindInMonthAsync(string period, Guid id)
    {
        var month = await client.GetFromJsonAsync<MonthView>($"/api/periods/{period}", Ct);
        return month!.Expenses.SingleOrDefault(e => e.Id == id);
    }

    public Task<HttpResponseMessage> PayAsync(string period, Guid id, string? paidOn = null) =>
        paidOn is null
            ? client.PutAsync($"/api/periods/{period}/expenses/{id}/payment", null, Ct)
            : client.PutAsJsonAsync($"/api/periods/{period}/expenses/{id}/payment", new { paidOn }, Ct);

    public Task<HttpResponseMessage> UnpayAsync(string period, Guid id) =>
        client.DeleteAsync($"/api/periods/{period}/expenses/{id}/payment", Ct);

    public sealed record MonthView(DateOnly Today, List<MonthExpense> Expenses);

    public sealed record MonthExpense(
        Guid Id,
        string Kind,
        string Name,
        string Amount,
        DateOnly? DueDate,
        DateOnly? PaidOn,
        string Status);
}
