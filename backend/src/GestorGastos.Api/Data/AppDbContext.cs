using System.Text.RegularExpressions;
using GestorGastos.Api.Auth;
using GestorGastos.Domain.Categories;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Api.Data;

public sealed partial class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<OneOffExpense> OneOffExpenses => Set<OneOffExpense>();

    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();

    public DbSet<RecurringVersion> RecurringVersions => Set<RecurringVersion>();

    public DbSet<RecurringInstance> RecurringInstances => Set<RecurringInstance>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Period>().HaveConversion<PeriodConverter>().HaveColumnType("date");
        configurationBuilder.Properties<Amount>().HaveConversion<AmountConverter>().HavePrecision(14, 2);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        UseSnakeCaseForDomainColumns(builder);
    }

    /// <summary>Nombres de columnas en snake_case para las tablas del dominio; Identity conserva los suyos.</summary>
    private static void UseSnakeCaseForDomainColumns(ModelBuilder builder)
    {
        var domainAssembly = typeof(Expense).Assembly;

        foreach (var entity in builder.Model.GetEntityTypes().Where(e => e.ClrType.Assembly == domainAssembly))
        {
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string name) => WordBoundary().Replace(name, "$1_$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
