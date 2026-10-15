namespace GestorGastos.Api.Contracts;

public sealed record CategoryInput(string? Name);

public sealed record CategoryDto(Guid Id, string Name);
