namespace GestorGastos.Domain.Common;

/// <summary>Datos que violan una regla del dominio, con los mensajes agrupados por campo.</summary>
public sealed class DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception(BuildMessage(errors))
{
    public DomainValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    private static string BuildMessage(IReadOnlyDictionary<string, string[]> errors) =>
        "Datos inválidos: " + string.Join("; ", errors.Select(e => $"{e.Key}: {string.Join(" ", e.Value)}"));
}
