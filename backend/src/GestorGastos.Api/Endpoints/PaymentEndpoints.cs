using System.Text.Json;
using GestorGastos.Api.Contracts;
using GestorGastos.Api.Services;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Api.Endpoints;

/// <summary>Marcado y desmarcado de pago de un gasto del mes (RF-27 a RF-31).</summary>
public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this RouteGroupBuilder api)
    {
        var payment = api.MapGroup("/periods/{period}/expenses/{expenseId:guid}/payment");

        payment.MapPut("/", MarkPaidAsync);
        payment.MapDelete("/", UnmarkPaidAsync);

        return api;
    }

    private static async Task<IResult> MarkPaidAsync(
        Period period,
        Guid expenseId,
        HttpRequest http,
        ExpenseCommandService commands,
        MonthQueryService months,
        CancellationToken ct)
    {
        // El cuerpo es opcional (sin fecha se registra hoy). Se lee a mano: un parámetro de cuerpo
        // exigiría Content-Type JSON incluso en un PUT vacío.
        MarkPaidRequest? request = null;
        if (http.HasJsonContentType())
        {
            try
            {
                request = await http.ReadFromJsonAsync<MarkPaidRequest>(ct);
            }
            catch (JsonException)
            {
                return TypedResults.BadRequest();
            }
        }

        var paid = await commands.MarkPaidAsync(period, expenseId, request?.PaidOn, ct);
        return paid is null ? TypedResults.NotFound() : TypedResults.Ok(await months.ToDtoAsync(paid, ct));
    }

    private static async Task<IResult> UnmarkPaidAsync(
        Period period,
        Guid expenseId,
        ExpenseCommandService commands,
        MonthQueryService months,
        CancellationToken ct)
    {
        var pending = await commands.UnmarkPaidAsync(period, expenseId, ct);
        return pending is null ? TypedResults.NotFound() : TypedResults.Ok(await months.ToDtoAsync(pending, ct));
    }
}
