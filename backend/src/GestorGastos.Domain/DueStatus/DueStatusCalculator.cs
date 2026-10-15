namespace GestorGastos.Domain.DueStatus;

public enum ExpenseDueStatus
{
    Pagado,
    AlDia,
    ProximoAVencer,
    VenceHoy,
    Vencido,
}

/// <summary>Estado de vencimiento calculado contra la fecha actual en Buenos Aires (RF-33).</summary>
public static class DueStatusCalculator
{
    private const int UpcomingDays = 3;

    public static ExpenseDueStatus Calculate(DateOnly? dueDate, DateOnly? paidOn, DateOnly today)
    {
        if (paidOn is not null)
            return ExpenseDueStatus.Pagado;
        if (dueDate is null)
            return ExpenseDueStatus.AlDia;

        var days = dueDate.Value.DayNumber - today.DayNumber;

        return days switch
        {
            < 0 => ExpenseDueStatus.Vencido,
            0 => ExpenseDueStatus.VenceHoy,
            <= UpcomingDays => ExpenseDueStatus.ProximoAVencer,
            _ => ExpenseDueStatus.AlDia,
        };
    }
}
