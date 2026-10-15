using GestorGastos.Api.Services;

namespace GestorGastos.Api.Endpoints;

/// <summary>Alerta de los gastos pendientes que vencen hoy (RF-32).</summary>
public static class AlertEndpoints
{
    public static RouteGroupBuilder MapAlertEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/alerts/due-today", (DueTodayAlertService alerts, CancellationToken ct) => alerts.GetAsync(ct));

        return api;
    }
}
