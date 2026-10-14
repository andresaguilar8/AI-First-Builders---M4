using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorGastos.Api.Data;

/// <summary>
/// Usada solo por <c>dotnet ef</c> para generar migraciones: no abre conexiones, así que no
/// necesita credenciales (principio II).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=gastos").Options);
}
