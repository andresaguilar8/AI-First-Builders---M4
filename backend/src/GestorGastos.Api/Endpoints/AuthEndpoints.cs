using System.Security.Claims;
using GestorGastos.Api.Auth;
using Microsoft.AspNetCore.Identity;

namespace GestorGastos.Api.Endpoints;

/// <summary>Inicio de sesión y verificación de sesión (RF-36). Sin registro (RF-37) ni logout.</summary>
public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth");

        auth.MapPost("/login", LoginAsync).AllowAnonymous();
        auth.MapGet("/me", (ClaimsPrincipal user) => new MeResponse(user.FindFirstValue(ClaimTypes.Email) ?? ""));

        return api;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn)
    {
        var user = string.IsNullOrWhiteSpace(request.Email) ? null : await users.FindByEmailAsync(request.Email);
        if (user is null || string.IsNullOrEmpty(request.Password))
            return Results.Unauthorized();

        // Sin bloqueo por intentos fallidos: fuera de alcance del PRD (riesgo aceptado).
        var result = await signIn.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: false);

        return result.Succeeded ? Results.NoContent() : Results.Unauthorized();
    }

    public sealed record LoginRequest(string? Email, string? Password);

    public sealed record MeResponse(string Email);
}
