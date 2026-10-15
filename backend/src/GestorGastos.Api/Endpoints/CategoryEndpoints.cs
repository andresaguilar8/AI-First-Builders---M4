using GestorGastos.Api.Contracts;
using GestorGastos.Api.Data;
using GestorGastos.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestorGastos.Api.Endpoints;

/// <summary>
/// ABM de categorías (RF-18 a RF-21). Renombrar y eliminar se reflejan en todos los meses por la FK
/// (<c>ON DELETE SET NULL</c> al eliminar).
/// </summary>
public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this RouteGroupBuilder api)
    {
        var categories = api.MapGroup("/categories");

        categories.MapGet("/", ListAsync);
        categories.MapPost("/", CreateAsync);
        categories.MapPut("/{categoryId:guid}", RenameAsync);
        categories.MapDelete("/{categoryId:guid}", DeleteAsync);

        return api;
    }

    private static async Task<List<CategoryDto>> ListAsync(AppDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().Select(c => new CategoryDto(c.Id, c.Name)).ToListAsync(ct);
        return [.. categories.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    private static async Task<IResult> CreateAsync(CategoryInput input, AppDbContext db, CancellationToken ct)
    {
        var name = CategoryName.Create(input.Name);
        if (await IsTakenAsync(db, name, exceptId: null, ct))
            return TypedResults.Conflict();

        var category = Category.Create(name);
        db.Categories.Add(category);
        return await TrySaveAsync(db, ct)
            ? TypedResults.Created($"/api/categories/{category.Id}", ToDto(category))
            : TypedResults.Conflict();
    }

    private static async Task<IResult> RenameAsync(
        Guid categoryId,
        CategoryInput input,
        AppDbContext db,
        CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([categoryId], ct);
        if (category is null)
            return TypedResults.NotFound();

        var name = CategoryName.Create(input.Name);
        if (await IsTakenAsync(db, name, categoryId, ct))
            return TypedResults.Conflict();

        category.Rename(name);
        return await TrySaveAsync(db, ct) ? TypedResults.Ok(ToDto(category)) : TypedResults.Conflict();
    }

    /// <summary>Sus gastos pasan a "Sin categoría" en todos los meses (RF-21).</summary>
    private static async Task<IResult> DeleteAsync(Guid categoryId, AppDbContext db, CancellationToken ct)
    {
        var deleted = await db.Categories.Where(c => c.Id == categoryId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    /// <summary>Nombre reservado o igual a otra categoría sin distinguir mayúsculas ni acentos (RF-19).</summary>
    private static async Task<bool> IsTakenAsync(AppDbContext db, CategoryName name, Guid? exceptId, CancellationToken ct) =>
        name.IsReserved
        || await db.Categories.AnyAsync(c => c.NormalizedName == name.Normalized && c.Id != exceptId, ct);

    /// <summary>Guarda; <c>false</c> si el índice único rechazó el nombre (alta concurrente).</summary>
    private static async Task<bool> TrySaveAsync(AppDbContext db, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private static CategoryDto ToDto(Category category) => new(category.Id, category.Name);
}
