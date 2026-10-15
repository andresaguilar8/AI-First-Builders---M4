using Microsoft.Extensions.Time.Testing;

namespace GestorGastos.Api.Tests.Infrastructure;

/// <summary>
/// Reloj de los tests de integración. Delega en un <see cref="FakeTimeProvider"/> que se reemplaza
/// al fijar una hora, así puede ir hacia atrás entre tests sin reconstruir el host.
/// </summary>
public sealed class TestClock(DateTimeOffset utcNow) : TimeProvider
{
    private FakeTimeProvider _fake = new(utcNow);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override long TimestampFrequency => _fake.TimestampFrequency;

    public override DateTimeOffset GetUtcNow() => _fake.GetUtcNow();

    public override long GetTimestamp() => _fake.GetTimestamp();

    // Los timers de la infraestructura siguen al reloj real para no quedar congelados.
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        System.CreateTimer(callback, state, dueTime, period);

    public void SetUtcNow(DateTimeOffset utcNow) => _fake = new FakeTimeProvider(utcNow);

    public void Advance(TimeSpan delta) => _fake.Advance(delta);
}
