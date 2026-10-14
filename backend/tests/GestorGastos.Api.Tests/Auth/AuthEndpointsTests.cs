using System.Net;
using System.Net.Http.Json;
using GestorGastos.Api.Auth;
using GestorGastos.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Tests.Auth;

public sealed class AuthEndpointsTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password }, Ct);

    [Fact]
    [Trait("AC", "AC-52")]
    public async Task Login_ConCredencialesCorrectas_DevuelveCookieYDaAcceso()
    {
        using var client = Factory.CreateAnonymousClient();

        var login = await LoginAsync(client, ApiFactory.UserEmail, ApiFactory.UserPassword);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith(AuthSetup.CookieName + "="));
        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me", Ct);
        Assert.Equal(ApiFactory.UserEmail, me?.Email);
    }

    [Theory]
    [Trait("AC", "AC-53")]
    [InlineData(ApiFactory.UserEmail, "contraseña-incorrecta")]
    [InlineData("otro@test.local", ApiFactory.UserPassword)]
    [InlineData("", "")]
    public async Task Login_ConCredencialesIncorrectas_DevuelveUnauthorizedSinDatos(string email, string password)
    {
        using var client = Factory.CreateAnonymousClient();

        var login = await LoginAsync(client, email, password);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Empty(await login.Content.ReadAsByteArrayAsync(Ct));
        Assert.False(login.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-53")]
    public async Task Login_FallidosRepetidos_NoBloqueanLaCuenta()
    {
        using var client = Factory.CreateAnonymousClient();
        for (var i = 0; i < 10; i++)
            await LoginAsync(client, ApiFactory.UserEmail, "incorrecta");

        var login = await LoginAsync(client, ApiFactory.UserEmail, ApiFactory.UserPassword);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
    }

    [Theory]
    [Trait("AC", "AC-55")]
    [InlineData("GET", "/api/periods/2026-10")]
    [InlineData("POST", "/api/categories")]
    [InlineData("GET", "/api/auth/me")]
    [InlineData("GET", "/api/export.csv")]
    public async Task SinSesion_LaApiRechazaComoNoAutenticadaYSinCuerpo(string method, string path)
    {
        using var client = Factory.CreateAnonymousClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { name = "Varios" }),
        };

        var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync(Ct));
    }

    [Fact]
    [Trait("AC", "AC-56")]
    public async Task Sesion_ExpiraTras30DiasSinActividad()
    {
        using var client = await Factory.CreateAuthenticatedClientAsync();

        Factory.Time.Advance(TimeSpan.FromDays(30) + TimeSpan.FromMinutes(1));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-56")]
    public async Task Sesion_SeRenuevaConCadaRequest()
    {
        using var client = await Factory.CreateAuthenticatedClientAsync();

        Factory.Time.Advance(TimeSpan.FromDays(29));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me", Ct)).StatusCode);

        // 58 días desde el inicio de sesión, pero solo 29 desde la última actividad.
        Factory.Time.Advance(TimeSpan.FromDays(29));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-57")]
    public async Task Registro_NoExisteYSigueHabiendoUnSoloUsuario()
    {
        var body = new { email = "nuevo@test.local", password = "Otra-Contrasena-1" };
        using var anonymous = Factory.CreateAnonymousClient();
        using var authenticated = await Factory.CreateAuthenticatedClientAsync();

        var withoutSession = await anonymous.PostAsJsonAsync("/api/auth/register", body, Ct);
        var withSession = await authenticated.PostAsJsonAsync("/api/auth/register", body, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, withoutSession.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, withSession.StatusCode);
        Assert.Equal(1, await Factory.WithDbAsync(db => db.Users.CountAsync(Ct)));
    }

    private sealed record MeResponse(string Email);
}
