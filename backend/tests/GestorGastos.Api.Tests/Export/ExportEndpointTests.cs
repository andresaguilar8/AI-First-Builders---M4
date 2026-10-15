using System.Net;
using System.Net.Http.Json;
using System.Text;
using GestorGastos.Api.Tests.Infrastructure;

namespace GestorGastos.Api.Tests.Export;

public sealed class ExportEndpointTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("AC", "AC-59")]
    public async Task Exportar_DevuelveCsvUtf8ConBomYFinDeLineaCrLf()
    {
        using var client = await Factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync(
            "/api/periods/2026-10/expenses",
            new { name = "Luz, gas", amount = "1234.56", dueDate = "2026-10-10", kind = "OneOff" },
            Ct);

        var response = await client.GetAsync("/api/export.csv", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("gastos-2026-10-05.csv", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        var bytes = await response.Content.ReadAsByteArrayAsync(Ct);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Equal(
            "periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago\r\n"
            + "2026-10,\"Luz, gas\",1234.56,Sin categoría,2026-10-10,Pendiente,\r\n",
            Encoding.UTF8.GetString(bytes[3..]));
    }

    [Fact]
    [Trait("AC", "AC-58")]
    public async Task Exportar_IncluyeProyeccionesHastaElMesActual()
    {
        using var client = await Factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/periods/2026-08/expenses", new { name = "Regalo", amount = "100", kind = "OneOff" }, Ct);
        await client.PostAsJsonAsync("/api/periods/2026-08/expenses", new { name = "Alquiler", amount = "100", kind = "Recurring" }, Ct);

        var csv = Encoding.UTF8.GetString((await client.GetByteArrayAsync("/api/export.csv", Ct))[3..]);

        var periods = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)[1..].Select(l => l[..7]);
        Assert.Equal(["2026-08", "2026-08", "2026-09", "2026-10"], periods);
    }
}
