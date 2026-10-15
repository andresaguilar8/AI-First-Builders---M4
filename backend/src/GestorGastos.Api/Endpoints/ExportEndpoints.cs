using System.Globalization;
using System.Text;
using GestorGastos.Api.Data;
using GestorGastos.Domain.Export;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Endpoints;

/// <summary>Descarga del historial en CSV (RF-38, RF-39).</summary>
public static class ExportEndpoints
{
    public static RouteGroupBuilder MapExportEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/export.csv", ExportAsync);

        return api;
    }

    /// <summary>Carga todo el historial en memoria (≤ 12.000 filas en 10 años, R-14) y lo escribe en UTF-8 con BOM.</summary>
    private static async Task<IResult> ExportAsync(AppDbContext db, IBusinessCalendar calendar, CancellationToken ct)
    {
        var oneOffs = await db.OneOffExpenses.AsNoTracking().ToListAsync(ct);
        var recurring = await db.RecurringExpenses.AsNoTracking()
            .Include(e => e.Versions)
            .Include(e => e.Instances)
            .AsSplitQuery()
            .ToListAsync(ct);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var csv = CsvExportBuilder.Build([.. oneOffs, .. recurring], categories, calendar.CurrentPeriod);

        var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = utf8WithBom.GetPreamble().Concat(utf8WithBom.GetBytes(csv)).ToArray();
        var fileName = $"gastos-{calendar.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.csv";

        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileName);
    }
}
