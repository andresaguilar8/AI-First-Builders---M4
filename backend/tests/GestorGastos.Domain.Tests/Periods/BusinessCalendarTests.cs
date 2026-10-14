using GestorGastos.Domain.Periods;
using Microsoft.Extensions.Time.Testing;

namespace GestorGastos.Domain.Tests.Periods;

public class BusinessCalendarTests
{
    private static BusinessCalendar CalendarAt(DateTimeOffset utcNow) =>
        new(new FakeTimeProvider(utcNow));

    [Fact]
    [Trait("AC", "AC-48")]
    public void Today_UsaLaFechaDeBuenosAiresAunqueEnUtcYaSeaElDiaSiguiente()
    {
        // 22:00 del 05/10/2026 en Buenos Aires = 01:00 del 06/10/2026 en UTC.
        var calendar = CalendarAt(new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 5), calendar.Today);
        Assert.Equal(new Period(2026, 10), calendar.CurrentPeriod);
    }

    [Fact]
    public void Today_CambiaALaMedianocheDeBuenosAires()
    {
        // 00:00 del 06/10/2026 en Buenos Aires = 03:00 UTC.
        var calendar = CalendarAt(new DateTimeOffset(2026, 10, 6, 3, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 6), calendar.Today);
    }

    [Fact]
    public void CurrentPeriod_SigueEnElMesAnteriorHastaLaMedianocheDeBuenosAires()
    {
        // 23:30 del 31/10/2026 en Buenos Aires = 02:30 del 01/11/2026 en UTC.
        var calendar = CalendarAt(new DateTimeOffset(2026, 11, 1, 2, 30, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 31), calendar.Today);
        Assert.Equal(new Period(2026, 10), calendar.CurrentPeriod);
    }

    [Fact]
    public void Today_ReflejaElAvanceDelReloj()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 5, 15, 0, 0, TimeSpan.Zero));
        var calendar = new BusinessCalendar(time);

        time.Advance(TimeSpan.FromDays(1));

        Assert.Equal(new DateOnly(2026, 10, 6), calendar.Today);
    }
}
