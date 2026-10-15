using System.Net;
using GestorGastos.Api.Auth;
using GestorGastos.Domain.Categories;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Tests.Infrastructure;

/// <summary>Verifica el propio harness antes de que lo usen las historias.</summary>
public sealed class ApiFactoryTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task AlIniciar_AplicaMigracionesYCreaExactamenteUnUsuario()
    {
        var users = await Factory.WithDbAsync(db => db.Users.ToListAsync(TestContext.Current.CancellationToken));

        var user = Assert.Single(users);
        Assert.Equal(ApiFactory.UserEmail, user.Email);
    }

    [Fact]
    public async Task CreateAuthenticatedClientAsync_ObtieneLaCookieDeSesion()
    {
        using var anonymous = Factory.CreateAnonymousClient();
        var response = await anonymous.PostAsync(ApiFactory.TestSignInPath, null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith(AuthSetup.CookieName + "=", setCookie);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetAsync_BorraLosDatosYReiniciaElReloj()
    {
        await Factory.WithDbAsync(async db =>
        {
            db.Categories.Add(new Category(Guid.NewGuid(), "Varios", "varios"));
            return await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });
        Factory.Time.Advance(TimeSpan.FromDays(3));

        await Factory.ResetAsync();

        Assert.Equal(0, await Factory.WithDbAsync(db => db.Categories.CountAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(ApiFactory.DefaultNow, Factory.Time.GetUtcNow());
    }

    [Fact]
    public async Task CountInstancesAsync_DevuelveCeroSinRegistros() =>
        Assert.Equal(0, await Factory.CountInstancesAsync(Guid.NewGuid(), new Period(2026, 10)));
}
