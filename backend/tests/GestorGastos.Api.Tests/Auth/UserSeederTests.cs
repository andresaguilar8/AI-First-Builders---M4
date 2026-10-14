using GestorGastos.Api.Auth;
using GestorGastos.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorGastos.Api.Tests.Auth;

public sealed class UserSeederTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static IConfiguration Config(string? email, string? password) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [UserSeeder.EmailKey] = email,
                [UserSeeder.PasswordKey] = password,
            })
            .Build();

    private async Task SeedAsync(IConfiguration configuration)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await UserSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(), configuration);
    }

    [Fact]
    public async Task SeedAsync_SiYaExisteUnUsuario_NoCreaOtro()
    {
        await SeedAsync(Config("otro@test.local", "Otra-Contrasena-1"));

        var users = await Factory.WithDbAsync(db => db.Users.ToListAsync(Ct));
        Assert.Equal(ApiFactory.UserEmail, Assert.Single(users).Email);
    }

    [Fact]
    public async Task PasswordHash_NoContieneLaContrasenaYUsaElFormatoV3DeIdentity()
    {
        var hash = (await Factory.WithDbAsync(db => db.Users.SingleAsync(Ct))).PasswordHash!;

        Assert.DoesNotContain(ApiFactory.UserPassword, hash);
        // Formato v3: PBKDF2-HMAC-SHA512 con salt, marcado con 0x01 en el primer byte (RNF-04).
        Assert.Equal(0x01, Convert.FromBase64String(hash)[0]);
    }

    [Fact]
    public async Task SeedAsync_SinUsuarioYSinVariables_FallaConMensajeClaro()
    {
        await Factory.WithDbAsync(db => db.Users.ExecuteDeleteAsync(Ct));
        try
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SeedAsync(Config(null, null)));
            Assert.Contains(UserSeeder.EmailKey, error.Message);
        }
        finally
        {
            await SeedAsync(Config(ApiFactory.UserEmail, ApiFactory.UserPassword));
        }
    }
}
