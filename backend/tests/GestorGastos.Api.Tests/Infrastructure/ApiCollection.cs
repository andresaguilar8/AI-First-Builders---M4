namespace GestorGastos.Api.Tests.Infrastructure;

/// <summary>
/// Todas las clases de tests de integración comparten esta colección: una sola base y ninguna
/// corrida en paralelo contra ella.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}

/// <summary>Base de los tests de integración: deja la base vacía y el reloj en su valor inicial antes de cada test.</summary>
[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(ApiFactory factory) : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = factory;

    public virtual async ValueTask InitializeAsync() => await Factory.ResetAsync();

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
