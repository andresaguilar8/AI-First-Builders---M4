using GestorGastos.Domain.Categories;
using GestorGastos.Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorGastos.Api.Data.Configurations;

/// <summary>Jerarquía TPH en la tabla <c>expenses</c>, discriminada por <c>kind</c> (R-02).</summary>
public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses", table =>
        {
            table.HasCheckConstraint("ck_expenses_period_first_day", "extract(day from period) = 1");
            table.HasCheckConstraint("ck_expenses_start_period_first_day", "extract(day from start_period) = 1");
            table.HasCheckConstraint("ck_expenses_end_period_first_day", "extract(day from end_period) = 1");
            table.HasCheckConstraint("ck_expenses_end_after_start", "end_period IS NULL OR end_period >= start_period");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.HasDiscriminator<string>("kind")
            .HasValue<OneOffExpense>("one_off")
            .HasValue<RecurringExpense>("recurring");
        builder.Property<string>("kind").HasMaxLength(20);
    }
}

public sealed class OneOffExpenseConfiguration : IEntityTypeConfiguration<OneOffExpense>
{
    public void Configure(EntityTypeBuilder<OneOffExpense> builder)
    {
        builder.Ignore(e => e.Values);
        builder.Property(e => e.Name).HasMaxLength(ExpenseValues.NameMaxLength).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(ExpenseValues.DescriptionMaxLength);
        builder.HasOne<Category>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => e.Period).HasFilter("kind = 'one_off'");
    }
}

public sealed class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> builder)
    {
        builder.HasMany(e => e.Versions).WithOne()
            .HasForeignKey("RecurringExpenseId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Instances).WithOne()
            .HasForeignKey("RecurringExpenseId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(e => e.Instances).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(e => new { e.StartPeriod, e.EndPeriod }).HasFilter("kind = 'recurring'");
    }
}
