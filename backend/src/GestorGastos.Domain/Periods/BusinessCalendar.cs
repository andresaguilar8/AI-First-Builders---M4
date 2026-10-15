namespace GestorGastos.Domain.Periods;

/// <summary>Fecha actual del negocio, siempre en America/Argentina/Buenos_Aires (RNF-07).</summary>
public interface IBusinessCalendar
{
    DateOnly Today { get; }

    Period CurrentPeriod { get; }
}

public sealed class BusinessCalendar(TimeProvider timeProvider) : IBusinessCalendar
{
    private static readonly TimeZoneInfo BuenosAires =
        TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), BuenosAires).DateTime);

    public Period CurrentPeriod => Period.FromDate(Today);
}
