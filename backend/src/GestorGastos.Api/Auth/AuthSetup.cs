using GestorGastos.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace GestorGastos.Api.Auth;

/// <summary>Identity con cookie de sesión que expira tras 30 días sin actividad (R-06, RNF-08).</summary>
public static class AuthSetup
{
    public const string CookieName = "gg.session";

    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = SessionLifetime;
                options.SlidingExpiration = false;
                options.Events.OnValidatePrincipal = context =>
                {
                    // Renovar en cada request: la sesión vence a los 30 días exactos sin actividad.
                    context.ShouldRenew = true;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToLogin = context =>
                {
                    // Sin redirección ni cuerpo: la respuesta no expone datos (AC-55).
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();

        // Sin MapIdentityApi: expondría /register (prohibido por RF-37).
        services.AddIdentityCore<AppUser>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        return services;
    }
}
