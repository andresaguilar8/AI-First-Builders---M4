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
        expenses.MapDelete("/{expenseId:guid}", DeleteAsync);
        expenses.MapPost("/{expenseId:guid}/discontinue", DiscontinueAsync);
        expenses.MapPost("/{expenseId:guid}/convert", ConvertAsync);

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

    private static async Task<IResult> DeleteAsync(
        Period period,
        Guid expenseId,
        ExpenseCommandService commands,
        CancellationToken ct) =>
        ToResult(await commands.DeleteAsync(period, expenseId, ct));

    private static async Task<IResult> DiscontinueAsync(
        Period period,
        Guid expenseId,
        ExpenseCommandService commands,
        CancellationToken ct) =>
        ToResult(await commands.DiscontinueAsync(period, expenseId, ct));

    private static async Task<IResult> ConvertAsync(
        Period period,
        Guid expenseId,
        ConvertExpenseRequest request,
        ExpenseCommandService commands,
        MonthQueryService months,
        CancellationToken ct)
    {
        var (outcome, created) = await commands.ConvertAsync(period, expenseId, request, ct);
        return created is null ? ToResult(outcome) : TypedResults.Ok(await months.ToDtoAsync(created, ct));
    }

    private static IResult ToResult(CommandOutcome outcome) => outcome switch
    {
        CommandOutcome.Done => TypedResults.NoContent(),
        CommandOutcome.NotFound => TypedResults.NotFound(),
        _ => TypedResults.Conflict(),
    };
}
