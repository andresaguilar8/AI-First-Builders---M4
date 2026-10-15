using GestorGastos.Api.Contracts;
using GestorGastos.Api.Services;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Api.Endpoints;

/// <summary>Gastos del mes visualizado.</summary>
public static class ExpenseEndpoints
{
    public static RouteGroupBuilder MapExpenseEndpoints(this RouteGroupBuilder api)
    {
        var expenses = api.MapGroup("/periods/{period}/expenses");

        expenses.MapPost("/", CreateAsync);

        return api;
    }

    private static async Task<IResult> CreateAsync(
        Period period,
        CreateExpenseRequest request,
        ExpenseCommandService commands,
        MonthQueryService months,
        CancellationToken ct)
    {
        var created = await commands.CreateAsync(period, request, ct);
        return TypedResults.Created((string?)null, await months.ToDtoAsync(created, ct));
    }
}
