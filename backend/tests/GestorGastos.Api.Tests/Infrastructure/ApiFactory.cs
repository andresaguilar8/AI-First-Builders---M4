using GestorGastos.Api.Auth;
using GestorGastos.Api.Data;
using GestorGastos.Domain.Periods;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GestorGastos.Api.Tests.Infrastructure;

/// <summary>
/// Host de la API sobre un PostgreSQL 16 real (Testcontainers) con reloj falso y un usuario de
/// prueba. Una sola instancia por corrida, compartida por la colección "Api".
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string UserEmail = "andres@test.local";
    public const string UserPassword = "Contrasena-De-Prueba-1";

    /// <summary>Endpoint solo de tests que inicia sesión sin pasar por POST /api/auth/login.</summary>
    public const string TestSignInPath = "/test/sign-in";

    /// <summary>Momento inicial del reloj: 05/10/2026 12:00 en Buenos Aires.</summary>
    public static readonly DateTimeOffset DefaultNow = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();

    public TestClock Time { get; } = new(DefaultNow);

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" evita cargar los user-secrets de desarrollo.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting(UserSeeder.EmailKey, UserEmail);
        builder.UseSetting(UserSeeder.PasswordKey, UserPassword);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Time);
            services.AddSingleton<IStartupFilter, TestSignInStartupFilter>();
        });
    }

    /// <summary>
    /// Cliente sin sesión que guarda la cookie de sesión si el servidor la emite. La cookie es
    /// <c>Secure</c>, por eso la base es https.
    /// </summary>
    public HttpClient CreateAnonymousClient() =>
        CreateDefaultClient(new Uri("https://localhost"), new SessionCookieHandler());

    /// <summary>
    /// Cliente con la cookie de sesión del usuario de prueba, obtenida sin el endpoint de login
    /// para que las historias US2 a US7 no dependan de US1.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateAnonymousClient();
        var response = await client.PostAsync(TestSignInPath, null);
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Borra gastos y categorías (conserva el usuario) y vuelve el reloj a <see cref="DefaultNow"/>.</summary>
    public async Task ResetAsync()
    {
        Time.SetUtcNow(DefaultNow);
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE recurring_instance, recurring_version, expenses, category");
    }

    /// <summary>Cantidad de registros mensuales guardados de un recurrente en un mes (RNF-05, RNF-06).</summary>
    public async Task<int> CountInstancesAsync(Guid expenseId, Period period)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.RecurringInstances.CountAsync(i =>
            EF.Property<Guid>(i, "RecurringExpenseId") == expenseId && i.Period == period);
    }

    /// <summary>Ejecuta una acción con un <see cref="AppDbContext"/> propio.</summary>
    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private sealed class TestSignInStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Map(TestSignInPath, signIn => signIn.Run(async context =>
            {
                var users = context.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var signInManager = context.RequestServices.GetRequiredService<SignInManager<AppUser>>();
                var user = await users.FindByEmailAsync(UserEmail)
                    ?? throw new InvalidOperationException("No existe el usuario de prueba.");
                await signInManager.SignInAsync(user, isPersistent: true);
                context.Response.StatusCode = StatusCodes.Status204NoContent;
            }));
            next(app);
        };
    }
}
