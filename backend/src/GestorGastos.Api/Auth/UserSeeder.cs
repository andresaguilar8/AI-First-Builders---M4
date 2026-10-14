using Microsoft.AspNetCore.Identity;

namespace GestorGastos.Api.Auth;

/// <summary>
/// Crea el único usuario a partir de <c>SEED_USER_EMAIL</c> y <c>SEED_USER_PASSWORD</c> si todavía no
/// existe ninguno (R-07, RF-37). Las credenciales nunca están en el código (principio II).
/// </summary>
public static class UserSeeder
{
    public const string EmailKey = "SEED_USER_EMAIL";
    public const string PasswordKey = "SEED_USER_PASSWORD";

    public static async Task SeedAsync(UserManager<AppUser> users, IConfiguration configuration)
    {
        if (users.Users.Any())
            return;

        var email = configuration[EmailKey];
        var password = configuration[PasswordKey];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"No hay ningún usuario y faltan {EmailKey} o {PasswordKey}. Definí ambas variables " +
                "de entorno (o user-secrets en desarrollo) para crear el usuario inicial.");
        }

        var result = await users.CreateAsync(new AppUser { UserName = email, Email = email }, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "No se pudo crear el usuario inicial: " + string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
