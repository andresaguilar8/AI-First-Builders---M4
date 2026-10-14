using GestorGastos.Api.Auth;

namespace GestorGastos.Api.Tests.Infrastructure;

/// <summary>
/// Guarda la cookie de sesión y la reenvía, ignorando su atributo <c>Expires</c>: ese atributo se
/// calcula con el reloj falso del servidor y un <c>CookieContainer</c> lo compararía con el reloj
/// real. Así la expiración la decide solo el servidor, que es lo que se quiere probar (RNF-08).
/// </summary>
public sealed class SessionCookieHandler : DelegatingHandler
{
    private string? _session;

    public bool HasSession => _session is not null;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_session is not null)
            request.Headers.Add("Cookie", $"{AuthSetup.CookieName}={_session}");

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var prefix = AuthSetup.CookieName + "=";
            foreach (var cookie in cookies.Where(c => c.StartsWith(prefix, StringComparison.Ordinal)))
            {
                var value = cookie[prefix.Length..].Split(';')[0];
                _session = value.Length == 0 ? null : value;
            }
        }

        return response;
    }
}
