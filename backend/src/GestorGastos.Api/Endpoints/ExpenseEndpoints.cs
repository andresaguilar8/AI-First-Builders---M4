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
        expenses.MapPut("/{expenseId:guid}", UpdateAsync);

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

    private static async Task<IResult> UpdateAsync(
        Period period,
        Guid expenseId,
        UpdateExpenseRequest request,
        ExpenseCommandService commands,
        MonthQueryService months,
        CancellationToken ct)
    {
        var updated = await commands.UpdateAsync(period, expenseId, request, ct);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(await months.ToDtoAsync(updated, ct));
    }
}
