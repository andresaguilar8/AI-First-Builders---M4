using GestorGastos.Domain.Common;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Money;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Tests.Expenses;

public class RecurringEditorTests
{
    private static readonly Period August = new(2026, 8);
    private static readonly Period September = new(2026, 9);
    private static readonly Period October = new(2026, 10);
    private static readonly Period November = new(2026, 11);
    private static readonly Period December = new(2026, 12);
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static RecurringExpense Recurring(
        Period start,
        string amount = "30000",
        DateOnly? dueDate = null,
        string name = "Alquiler",
        string? description = null) =>
        ExpenseFactory.CreateRecurring(
            start, ExpenseValues.Create(name, description, Amount.Parse(amount), null, dueDate, start), Now);

    private static EffectiveExpense In(RecurringExpense expense, Period period) =>
        MonthProjector.EffectiveIn(expense, period) ?? throw new InvalidOperationException("No aparece en el mes.");

    private static ExpenseValues WithAmount(RecurringExpense expense, Period period, string amount) =>
        In(expense, period).Values with { Amount = Amount.Parse(amount) };

    /// <summary>Registra un pago directamente en el modelo (el flujo de pago se prueba en PaymentServiceTests).</summary>
    private static void PayIn(RecurringExpense expense, Period period, DateOnly paidOn)
    {
        if (expense.InstanceIn(period) is { } instance)
            instance.SetPaidOn(paidOn);
        else
            expense.AddInstance(new RecurringInstance(Guid.NewGuid(), period, In(expense, period).Values, paidOn));
    }

    [Fact]
    [Trait("AC", "AC-15")]
    public void SoloEsteMes_CambiaSoloElMesVisualizado()
    {
        var expense = Recurring(August);

        RecurringEditor.EditThisMonth(expense, September, WithAmount(expense, September, "35000"));

        Assert.Equal(Amount.Parse("35000"), In(expense, September).Values.Amount);
        Assert.Equal(Amount.Parse("30000"), In(expense, October).Values.Amount);
        Assert.Equal(Amount.Parse("30000"), In(expense, August).Values.Amount);
        Assert.Single(expense.Instances);
        Assert.Single(expense.Versions);
    }

    [Fact]
    [Trait("AC", "AC-16")]
    public void EsteMesYLosSiguientes_NoCambiaLosMesesAnteriores()
    {
        var expense = Recurring(August, "25000");

        RecurringEditor.EditThisAndFollowing(expense, September, WithAmount(expense, September, "30000"));

        Assert.Equal(Amount.Parse("25000"), In(expense, August).Values.Amount);
        Assert.Equal(Amount.Parse("30000"), In(expense, September).Values.Amount);
        Assert.Equal(Amount.Parse("30000"), In(expense, December).Values.Amount);
        Assert.Equal([August, September], expense.Versions.Select(v => v.EffectiveFrom).Order());
        Assert.Empty(expense.Instances);
    }

    [Fact]
    [Trait("AC", "AC-12")]
    public void EsteMesYLosSiguientes_CambiaElDiaDeVencimiento()
    {
        var expense = Recurring(August, dueDate: new DateOnly(2026, 8, 10));

        RecurringEditor.EditThisAndFollowing(
            expense, September, In(expense, September).Values with { DueDate = new DateOnly(2026, 9, 15) });

        Assert.Equal(new DateOnly(2026, 8, 10), In(expense, August).Values.DueDate);
        Assert.Equal(new DateOnly(2026, 9, 15), In(expense, September).Values.DueDate);
        Assert.Equal(new DateOnly(2026, 10, 15), In(expense, October).Values.DueDate);
        Assert.Empty(expense.Instances);
    }

    [Fact]
    [Trait("AC", "AC-17")]
    public void EsteMesYLosSiguientes_SobrescribeElMontoDeUnMesFuturoPagadoYConservaElPago()
    {
        var expense = Recurring(August);
        RecurringEditor.EditThisMonth(expense, November, WithAmount(expense, November, "32000"));
        PayIn(expense, November, new DateOnly(2026, 10, 1));

        RecurringEditor.EditThisAndFollowing(expense, September, WithAmount(expense, September, "35000"));

        var november = In(expense, November);
        Assert.Equal(Amount.Parse("35000"), november.Values.Amount);
        Assert.Equal(new DateOnly(2026, 10, 1), november.PaidOn);
        Assert.Equal(Amount.Parse("30000"), In(expense, August).Values.Amount);
    }

    [Fact]
    [Trait("AC", "AC-17")]
    public void EsteMesYLosSiguientes_SoloPropagaLosCamposQueCambiaron()
    {
        var expense = Recurring(August);
        var ownCategory = Guid.NewGuid();
        RecurringEditor.EditThisMonth(expense, November, In(expense, November).Values with { CategoryId = ownCategory });

        RecurringEditor.EditThisAndFollowing(expense, September, WithAmount(expense, September, "35000"));

        var november = In(expense, November);
        Assert.Equal(Amount.Parse("35000"), november.Values.Amount);
        Assert.Equal(ownCategory, november.Values.CategoryId);
        Assert.Null(In(expense, October).Values.CategoryId);
    }

    [Fact]
    [Trait("AC", "AC-63")]
    public void EsteMesYLosSiguientes_NombreYDescripcionNoSonRetroactivos()
    {
        var expense = Recurring(August, name: "Internet", description: "Plan 100 MB");

        RecurringEditor.EditThisAndFollowing(
            expense, September, In(expense, September).Values with { Name = "Fibra", Description = "Plan 300 MB" });

        Assert.Equal(("Internet", "Plan 100 MB"), (In(expense, August).Values.Name, In(expense, August).Values.Description));
        Assert.Equal(("Fibra", "Plan 300 MB"), (In(expense, September).Values.Name, In(expense, September).Values.Description));
        Assert.Equal(("Fibra", "Plan 300 MB"), (In(expense, October).Values.Name, In(expense, October).Values.Description));
    }

    [Fact]
    [Trait("AC", "AC-64")]
    public void EsteMesYLosSiguientes_EliminaElRegistroQueQuedaIgualALaVersionYSinPago()
    {
        var expense = Recurring(August);
        RecurringEditor.EditThisMonth(expense, November, WithAmount(expense, November, "32000"));
        Assert.NotNull(expense.InstanceIn(November));

        RecurringEditor.EditThisAndFollowing(expense, September, WithAmount(expense, September, "32000"));

        Assert.Equal(Amount.Parse("32000"), In(expense, November).Values.Amount);
        Assert.Null(expense.InstanceIn(November));
        Assert.Empty(expense.Instances);
    }

    [Fact]
    public void SoloEsteMes_VolviendoALosValoresDeLaVersion_EliminaElRegistro()
    {
        var expense = Recurring(August);
        RecurringEditor.EditThisMonth(expense, September, WithAmount(expense, September, "35000"));

        RecurringEditor.EditThisMonth(expense, September, WithAmount(expense, September, "30000"));

        Assert.Empty(expense.Instances);
    }

    [Fact]
    public void EsteMesYLosSiguientes_AplicaElCambioAVersionesPosterioresYFusionaLasIdenticas()
    {
        var expense = Recurring(August, "25000");
        RecurringEditor.EditThisAndFollowing(expense, December, WithAmount(expense, December, "40000"));

        RecurringEditor.EditThisAndFollowing(expense, September, WithAmount(expense, September, "40000"));

        Assert.Equal([August, September], expense.Versions.Select(v => v.EffectiveFrom).Order());
        Assert.Equal(Amount.Parse("40000"), In(expense, December).Values.Amount);

        RecurringEditor.EditThisAndFollowing(expense, September, WithAmount(expense, September, "25000"));

        Assert.Equal(August, Assert.Single(expense.Versions).EffectiveFrom);
        Assert.Equal(Amount.Parse("25000"), In(expense, December).Values.Amount);
    }

    [Fact]
    public void EsteMesYLosSiguientes_CambiaUnCampoEnVersionesPosterioresSinPisarLosDemas()
    {
        var expense = Recurring(August, "25000");
        RecurringEditor.EditThisAndFollowing(expense, December, WithAmount(expense, December, "40000"));

        RecurringEditor.EditThisAndFollowing(expense, September, In(expense, September).Values with { Name = "Casa" });

        Assert.Equal(("Casa", Amount.Parse("25000")), (In(expense, October).Values.Name, In(expense, October).Values.Amount));
        Assert.Equal(("Casa", Amount.Parse("40000")), (In(expense, December).Values.Name, In(expense, December).Values.Amount));
        Assert.Equal("Alquiler", In(expense, August).Values.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Editar_ConservaElPagoDelMes(bool thisAndFollowing)
    {
        var expense = Recurring(August);
        PayIn(expense, September, new DateOnly(2026, 9, 3));

        var values = WithAmount(expense, September, "31000");
        if (thisAndFollowing)
            RecurringEditor.EditThisAndFollowing(expense, September, values);
        else
            RecurringEditor.EditThisMonth(expense, September, values);

        Assert.Equal(new DateOnly(2026, 9, 3), In(expense, September).PaidOn);
        Assert.Equal(Amount.Parse("31000"), In(expense, September).Values.Amount);
    }

    [Fact]
    public void EsteMesYLosSiguientes_ConVencimientoEnElMesSiguiente_GuardaLaFechaExactaEnElMes()
    {
        var expense = Recurring(August, dueDate: new DateOnly(2026, 8, 10));

        RecurringEditor.EditThisAndFollowing(
            expense, September, In(expense, September).Values with { DueDate = new DateOnly(2026, 10, 5) });

        Assert.Equal(new DateOnly(2026, 10, 5), In(expense, September).Values.DueDate);
        Assert.Equal(new DateOnly(2026, 10, 5), In(expense, October).Values.DueDate);
        Assert.Equal(new DateOnly(2026, 11, 5), In(expense, November).Values.DueDate);
        Assert.Equal(new DateOnly(2026, 8, 10), In(expense, August).Values.DueDate);
    }

    [Fact]
    public void EsteMesYLosSiguientes_QuitarElVencimiento_ProyectaSinVencimiento()
    {
        var expense = Recurring(August, dueDate: new DateOnly(2026, 8, 10));

        RecurringEditor.EditThisAndFollowing(expense, September, In(expense, September).Values with { DueDate = null });

        Assert.Null(In(expense, September).Values.DueDate);
        Assert.Null(In(expense, December).Values.DueDate);
        Assert.Null(expense.VersionIn(September).DueDay);
        Assert.Equal(new DateOnly(2026, 8, 10), In(expense, August).Values.DueDate);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Editar_ConVencimientoAnteriorAlMes_SeRechaza(bool thisAndFollowing)
    {
        var expense = Recurring(August);
        var values = In(expense, September).Values with { DueDate = new DateOnly(2026, 8, 31) };

        var error = Assert.Throws<DomainValidationException>(() =>
        {
            if (thisAndFollowing)
                RecurringEditor.EditThisAndFollowing(expense, September, values);
            else
                RecurringEditor.EditThisMonth(expense, September, values);
        });

        Assert.True(error.Errors.ContainsKey("dueDate"));
        Assert.Empty(expense.Instances);
    }

    [Fact]
    public void Editar_EnUnMesEnQueNoAparece_FallaSinCambios()
    {
        var expense = Recurring(September);

        Assert.Throws<InvalidOperationException>(() =>
            RecurringEditor.EditThisMonth(expense, August, In(expense, September).Values));
    }
}
