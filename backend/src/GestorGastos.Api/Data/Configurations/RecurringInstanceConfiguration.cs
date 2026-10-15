using GestorGastos.Domain.Categories;
using GestorGastos.Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorGastos.Api.Data.Configurations;

public sealed class RecurringInstanceConfiguration : IEntityTypeConfiguration<RecurringInstance>
{
    public void Configure(EntityTypeBuilder<RecurringInstance> builder)
    {
        builder.ToTable("recurring_instance", table =>
            table.HasCheckConstraint("ck_recurring_instance_period_first_day", "extract(day from period) = 1"));
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Ignore(i => i.Values);
        builder.Property(i => i.Name).HasMaxLength(ExpenseValues.NameMaxLength);
        builder.Property(i => i.Description).HasMaxLength(ExpenseValues.DescriptionMaxLength);
        builder.HasOne<Category>().WithMany().HasForeignKey(i => i.CategoryId).OnDelete(DeleteBehavior.SetNull);

        // Como máximo un registro por recurrente y mes (RNF-06).
        builder.HasIndex("RecurringExpenseId", nameof(RecurringInstance.Period)).IsUnique();
    }
}
