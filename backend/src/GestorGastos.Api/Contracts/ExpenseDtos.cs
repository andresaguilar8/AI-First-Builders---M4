using GestorGastos.Domain.Common;
using GestorGastos.Domain.DueStatus;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Api.Contracts;

/// <summary>Valores de un gasto tal como llegan del formulario; los montos viajan como string (R-08).</summary>
public record ExpenseValuesRequest(
    string? Name,
    string? Description,
    string? Amount,
    Guid? CategoryId,
    DateOnly? DueDate)
{
    /// <summary>Valida todos los campos a la vez y devuelve los errores juntos (400 por campo).</summary>
    public ExpenseValues ToValues(Period period)
    {
        Amount? amount = null;
        string[]? amountErrors = null;
        if (!string.IsNullOrWhiteSpace(Amount))
        {
            try
            {
                amount = Domain.Money.Amount.Parse(Amount.Trim());
            }
            catch (DomainValidationException e)
            {
                amountErrors = e.Errors["amount"];
            }
        }

        try
        {
            return ExpenseValues.Create(Name, Description, amount, CategoryId, DueDate, period);
        }
        catch (DomainValidationException e) when (amountErrors is not null)
        {
            var errors = e.Errors.ToDictionary();
            errors["amount"] = amountErrors;
            throw new DomainValidationException(errors);
        }
    }
}

public sealed record CreateExpenseRequest(
    string? Name,
    string? Description,
    string? Amount,
    Guid? CategoryId,
    DateOnly? DueDate,
    ExpenseKind? Kind)
    : ExpenseValuesRequest(Name, Description, Amount, CategoryId, DueDate);

public enum EditScope
{
    ThisMonth,
    ThisAndFollowing,
}

/// <summary>Edición de un gasto; <see cref="Scope"/> es obligatorio para recurrentes (RF-10).</summary>
public sealed record UpdateExpenseRequest(
    string? Name,
    string? Description,
    string? Amount,
    Guid? CategoryId,
    DateOnly? DueDate,
    EditScope? Scope)
    : ExpenseValuesRequest(Name, Description, Amount, CategoryId, DueDate);

public sealed record CategoryRef(Guid Id, string Name);

public sealed record MonthExpenseDto(
    Guid Id,
    ExpenseKind Kind,
    string Name,
    string? Description,
    string Amount,
    CategoryRef? Category,
    DateOnly? DueDate,
    DateOnly? PaidOn,
    ExpenseDueStatus Status);

public sealed record CategoryTotalDto(Guid? CategoryId, string CategoryName, string Total);

public sealed record MonthViewDto(
    string Period,
    DateOnly Today,
    IReadOnlyList<MonthExpenseDto> Expenses,
    string Total,
    string PendingTotal,
    IReadOnlyList<CategoryTotalDto> ByCategory);
