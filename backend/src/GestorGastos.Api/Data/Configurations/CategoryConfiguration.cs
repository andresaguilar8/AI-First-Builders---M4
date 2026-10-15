using GestorGastos.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorGastos.Api.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("category");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(Category.NameMaxLength);
        builder.Property(c => c.NormalizedName).HasMaxLength(Category.NameMaxLength);
        builder.HasIndex(c => c.NormalizedName).IsUnique();
    }
}
