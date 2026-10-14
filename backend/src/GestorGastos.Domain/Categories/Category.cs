namespace GestorGastos.Domain.Categories;

/// <summary>
/// Agrupador de gastos. <see cref="NormalizedName"/> garantiza la unicidad sin distinguir
/// mayúsculas, acentos ni espacios repetidos (RF-19).
/// </summary>
public sealed class Category
{
    public const int NameMaxLength = 50;

    // Para EF Core.
    private Category()
    {
        Name = null!;
        NormalizedName = null!;
    }

    public Category(Guid id, string name, string normalizedName)
    {
        Id = id;
        Name = name;
        NormalizedName = normalizedName;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string NormalizedName { get; private set; }
}
