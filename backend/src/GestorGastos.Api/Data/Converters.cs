using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GestorGastos.Api.Data;

/// <summary>Un período se guarda como el primer día del mes (<c>date</c>).</summary>
public sealed class PeriodConverter() : ValueConverter<Period, DateOnly>(
    period => period.FirstDay,
    date => Period.FromDate(date));

public sealed class AmountConverter() : ValueConverter<Amount, decimal>(
    amount => amount.Value,
    value => Amount.Create(value));
