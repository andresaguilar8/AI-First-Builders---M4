using GestorGastos.Api.Services;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Api.Endpoints;

/// <summary>Consulta de un mes (RF-33 a RF-35).</summary>
public static class PeriodEndpoints
{
    public static RouteGroupBuilder MapPeriodEndpoints(this RouteGroupBuilder api)
    {
        // Period se enlaza con su TryParse: un período mal formado devuelve 400.
        api.MapGet("/periods/{period}", (Period period, MonthQueryService months, CancellationToken ct) =>
            months.GetAsync(period, ct));

        return api;
    }
}
