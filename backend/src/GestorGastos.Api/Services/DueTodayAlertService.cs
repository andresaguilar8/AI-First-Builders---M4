using GestorGastos.Api.Contracts;
using GestorGastos.Api.Data;
using GestorGastos.Domain.Alerts;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Services;

/// <summary>
/// Carga solo lo que puede vencer hoy (R-14): puntuales y registros mensuales pendientes con
/// vencimiento hoy, y los recurrentes activos en el mes actual con su versión vigente; el conteo lo
/// decide <see cref="DueTodayAlert"/> (RF-32).
/// </summary>
public sealed class DueTodayAlertService(AppDbContext db, IBusinessCalendar calendar)
{
    public async Task<DueTodayAlertDto> GetAsync(CancellationToken ct)
    {
        var today = calendar.Today;
        var current = calendar.CurrentPeriod;

        var oneOffs = await db.OneOffExpenses.AsNoTracking()
            .Where(e => e.DueDate == today && e.PaidOn == null)
            .ToListAsync(ct);

        var recurring = await db.RecurringExpenses.AsNoTracking()
            .Where(e => (e.StartPeriod <= current && (e.EndPeriod == null || e.EndPeriod >= current))
                || e.Instances.Any(i => i.DueDate == today && i.PaidOn == null))
            .Include(e => e.Versions.Where(v => v.EffectiveFrom <= current))
            .Include(e => e.Instances.Where(i => i.Period == current || (i.DueDate == today && i.PaidOn == null)))
            .AsSplitQuery()
            .ToListAsync(ct);

        return new DueTodayAlertDto(today, DueTodayAlert.Count([.. oneOffs, .. recurring], today));
    }
}
