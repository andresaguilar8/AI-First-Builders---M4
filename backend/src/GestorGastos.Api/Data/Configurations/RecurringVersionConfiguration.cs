using GestorGastos.Domain.Categories;
using GestorGastos.Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorGastos.Api.Data.Configurations;

public sealed class RecurringVersionConfiguration : IEntityTypeConfiguration<RecurringVersion>
{
    public void Configure(EntityTypeBuilder<RecurringVersion> builder)
    {
        builder.ToTable("recurring_version", table =>
        {
            table.HasCheckConstraint("ck_recurring_version_effective_from_first_day", "extract(day from effective_from) = 1");
            table.HasCheckConstraint("ck_recurring_version_due_day", "due_day BETWEEN 1 AND 31");
        });
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Name).HasMaxLength(ExpenseValues.NameMaxLength);
        builder.Property(v => v.Description).HasMaxLength(ExpenseValues.DescriptionMaxLength);
        builder.Property(v => v.DueDay).HasColumnType("smallint");
        builder.HasOne<Category>().WithMany().HasForeignKey(v => v.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex("RecurringExpenseId", nameof(RecurringVersion.EffectiveFrom)).IsUnique();
    }
}
