using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using GestorGastos.Api.Tests.Infrastructure;
using GestorGastos.Domain.Categories;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Payments;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Tests.Performance;

/// <summary>
/// RNF-01 / SC-003: con 10 años × 100 gastos por mes, cada operación responde en menos de 2 s en el
/// p95. El host corre en proceso (TestServer), así que el tiempo medido es el del servidor. Es
/// explícito: se corre antes de cada merge con <c>dotnet test -- --explicit only</c>.
/// </summary>
public sealed class PerformanceTests(ApiFactory factory, ITestOutputHelper output) : ApiTestBase(factory)
{
    private const int Years = 10;
    private const int OneOffsPerMonth = 70;
    private const int RecurringCount = 30;
    private const int Samples = 20;

    private static readonly TimeSpan P95Limit = TimeSpan.FromSeconds(2);
    private static readonly Period Current = new(2026, 10);
    private static readonly Period First = new(2016, 11);
    private static readonly DateOnly Today = new(2026, 10, 5);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly Dictionary<string, List<TimeSpan>> _timings = [];

    [Fact(Explicit = true)]
    [Trait("Category", "Performance")]
    public async Task OperacionesDeRnf01_RespondenEnMenosDe2SegundosEnElP95()
    {
        var seed = await SeedAsync();
        using var client = await Factory.CreateAuthenticatedClientAsync();
        using var anonymous = Factory.CreateAnonymousClient();

        for (var i = 0; i < Samples; i++)
        {
            await MeasureAsync("iniciar sesión", () => anonymous.PostAsJsonAsync(
                "/api/auth/login", new { email = ApiFactory.UserEmail, password = ApiFactory.UserPassword }, Ct));
            await MeasureAsync("alerta del día", () => client.GetAsync("/api/alerts/due-today", Ct));
            await MeasureAsync("consultar mes y resumen", () => client.GetAsync($"/api/periods/{Period(i)}", Ct));
            await MeasureAsync("navegar a un mes futuro", () => client.GetAsync($"/api/periods/{Current.Next()}", Ct));
        }

        for (var i = 0; i < Samples; i++)
        {
            var period = Period(i).ToString();
            await MeasureAsync("crear gasto", () => client.PostAsJsonAsync(
                $"/api/periods/{period}/expenses", new { name = $"Nuevo {i}", amount = "1000", kind = "OneOff" }, Ct));
            await MeasureAsync("editar recurrente (este mes y los siguientes)", () => client.PutAsJsonAsync(
                $"/api/periods/{First}/expenses/{seed.Recurring[0]}",
                new { name = "Recurrente 0", amount = $"{5000 + i}", scope = "ThisAndFollowing" },
                Ct));
            await MeasureAsync("editar recurrente (solo este mes)", () => client.PutAsJsonAsync(
                $"/api/periods/{period}/expenses/{seed.Recurring[1]}",
                new { name = "Recurrente 1", amount = $"{7000 + i}", scope = "ThisMonth" },
                Ct));
            await MeasureAsync("marcar pago", () => client.PutAsync(
                $"/api/periods/{Current}/expenses/{seed.Recurring[2]}/payment", null, Ct));
            await MeasureAsync("desmarcar pago", () => client.DeleteAsync(
                $"/api/periods/{Current}/expenses/{seed.Recurring[2]}/payment", Ct));
            await MeasureAsync("eliminar puntual", () => client.DeleteAsync(
                $"/api/periods/{Current}/expenses/{seed.OneOffsInCurrent[i]}", Ct));
            await MeasureAsync("cambiar puntual a recurrente", () => client.PostAsJsonAsync(
                $"/api/periods/{Current}/expenses/{seed.OneOffsInCurrent[Samples + i]}/convert", new { to = "Recurring" }, Ct));
            await MeasureAsync("crear categoría", () => client.PostAsJsonAsync("/api/categories", new { name = $"Nueva {i}" }, Ct));
        }

        // Cada baja y cada cambio a puntual recorren un recurrente con años de versiones e instancias.
        for (var i = 3; i < 3 + 10; i++)
        {
            await MeasureAsync("dar de baja recurrente", () => client.PostAsync(
                $"/api/periods/{Current}/expenses/{seed.Recurring[i]}/discontinue", null, Ct));
            await MeasureAsync("cambiar recurrente a puntual", () => client.PostAsJsonAsync(
                $"/api/periods/{Current}/expenses/{seed.Recurring[i + 10]}/convert", new { to = "OneOff" }, Ct));
        }

        // Las categorías sembradas están asociadas a miles de gastos, versiones e instancias.
        for (var i = 0; i < seed.Categories.Count; i++)
        {
            var id = seed.Categories[i];
            await MeasureAsync("renombrar categoría", () => client.PutAsJsonAsync(
                $"/api/categories/{id}", new { name = $"Renombrada {i}" }, Ct));
        }

        foreach (var id in seed.Categories)
            await MeasureAsync("eliminar categoría", () => client.DeleteAsync($"/api/categories/{id}", Ct));

        for (var i = 0; i < 5; i++)
            await MeasureAsync("exportar CSV", () => client.GetAsync("/api/export.csv", Ct));

        var slow = new List<string>();
        foreach (var (operation, timings) in _timings)
        {
            var p95 = Percentile95(timings);
            output.WriteLine($"{operation,-48} n={timings.Count,3}  p95={p95.TotalMilliseconds,8:0.0} ms");
            if (p95 >= P95Limit)
                slow.Add($"{operation}: {p95.TotalMilliseconds:0} ms");
        }

        Assert.Empty(slow);
    }

    private static Period Period(int index)
    {
        var period = First;
        for (var i = 0; i < index * 6; i++)
            period = period.Next();
        return period;
    }

    private async Task MeasureAsync(string operation, Func<Task<HttpResponseMessage>> request)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await request();
        _ = await response.Content.ReadAsByteArrayAsync(Ct);
        stopwatch.Stop();

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.NoContent,
            $"{operation} respondió {(int)response.StatusCode}");

        if (!_timings.TryGetValue(operation, out var timings))
            _timings[operation] = timings = [];
        timings.Add(stopwatch.Elapsed);
    }

    private static TimeSpan Percentile95(List<TimeSpan> timings)
    {
        var sorted = timings.Order().ToList();
        return sorted[(int)Math.Ceiling(sorted.Count * 0.95) - 1];
    }

    /// <summary>
    /// 120 meses con 100 gastos cada uno: 70 puntuales y 30 recurrentes activos todo el período, con
    /// un aumento anual ("Este mes y los siguientes") y la mitad de los meses pagados.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        var now = Factory.Time.GetUtcNow();
        var categories = Enumerable.Range(0, 10).Select(i => Category.Create(CategoryName.Create($"Categoría {i}"))).ToList();
        var expenses = new List<Expense>();
        var oneOffsInCurrent = new List<Guid>();

        for (var r = 0; r < RecurringCount; r++)
        {
            var recurring = ExpenseFactory.CreateRecurring(
                First,
                ExpenseValues.Create($"Recurrente {r}", null, Amount.Parse("5000"), categories[r % 10].Id, First.FirstDay.AddDays(r % 28), First),
                now);
            var period = First;
            for (var month = 0; month < Years * 12; month++, period = period.Next())
            {
                if (month > 0 && month % 12 == 0)
                {
                    var values = MonthProjector.EffectiveIn(recurring, period)!.Values;
                    RecurringEditor.EditThisAndFollowing(
                        recurring, period, values with { Amount = Amount.Create(values.Amount.Value + 500) });
                }

                if (month % 2 == 0 && period < Current)
                    PaymentService.MarkPaid(recurring, period, Today, null);
            }

            expenses.Add(recurring);
        }

        for (var (period, month) = (First, 0); month < Years * 12; month++, period = period.Next())
        {
            for (var i = 0; i < OneOffsPerMonth; i++)
            {
                var oneOff = ExpenseFactory.CreateOneOff(
                    period,
                    ExpenseValues.Create($"Puntual {i}", "Detalle", Amount.Parse($"{100 + i}.50"), categories[i % 10].Id, period.FirstDay.AddDays(i % 28), period),
                    now);
                if (i % 3 == 0 && period < Current)
                    PaymentService.MarkPaid(oneOff, period, Today, null);
                expenses.Add(oneOff);
                if (period == Current)
                    oneOffsInCurrent.Add(oneOff.Id);
            }
        }

        await Factory.WithDbAsync(async db =>
        {
            db.Categories.AddRange(categories);
            db.Expenses.AddRange(expenses);
            await db.SaveChangesAsync(Ct);
            return await db.Database.ExecuteSqlRawAsync("ANALYZE", Ct);
        });

        return new Seed(
            [.. expenses.OfType<RecurringExpense>().Select(e => e.Id)],
            oneOffsInCurrent,
            [.. categories.Select(c => c.Id)]);
    }

    private sealed record Seed(List<Guid> Recurring, List<Guid> OneOffsInCurrent, List<Guid> Categories);
}
