using GestorGastos.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace GestorGastos.Api.Validation;

/// <summary>Convierte una <see cref="DomainValidationException"/> en un 400 con <c>errors</c> por campo (RFC 9457).</summary>
public sealed class DomainValidationExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainValidationException validation)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new HttpValidationProblemDetails(validation.Errors.ToDictionary())
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos inválidos",
            },
        });
    }
}
