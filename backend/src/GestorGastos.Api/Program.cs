using System.Text.Json.Serialization;
using GestorGastos.Api.Auth;
using GestorGastos.Api.Data;
using GestorGastos.Api.Endpoints;
using GestorGastos.Api.Services;
using GestorGastos.Api.Validation;
using GestorGastos.Domain.Periods;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IBusinessCalendar, BusinessCalendar>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddAppAuthentication();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainValidationExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<MonthQueryService>();
builder.Services.AddScoped<ExpenseCommandService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await UserSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
        app.Configuration);
}

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Toda la API exige sesión; los endpoints anónimos (login) lo indican con AllowAnonymous.
var api = app.MapGroup("/api").RequireAuthorization();
api.MapAuthEndpoints();
api.MapPeriodEndpoints();
api.MapExpenseEndpoints();

// Rutas de /api inexistentes: 401 sin sesión (no se pueden descubrir rutas) y 404 con sesión,
// p. ej. /api/auth/register (RF-37).
api.Map("{**path}", () => Results.NotFound());

// La SPA (incluido el login) se sirve sin sesión; el resto de las rutas caen en index.html.
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
