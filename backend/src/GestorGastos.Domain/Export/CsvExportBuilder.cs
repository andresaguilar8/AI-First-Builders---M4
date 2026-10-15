using System.Globalization;
using System.Text;
using GestorGastos.Domain.Categories;
using GestorGastos.Domain.Expenses;
using GestorGastos.Domain.Periods;

namespace GestorGastos.Domain.Export;

/// <summary>
/// Historial en CSV (RFC 4180, fin de línea CRLF) desde el primer mes con gastos hasta el mes actual,
/// con una fila por gasto y mes, incluidos los meses solo proyectados (RF-38, RF-39,
/// contracts/csv-export.md). El BOM lo agrega quien escribe el archivo.
/// </summary>
public static class CsvExportBuilder
{
    public const string Header = "periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago";

    private const string LineEnd = "\r\n";

    /// <param name="categoryNames">Nombre actual de cada categoría (refleja renombres, RF-20).</param>
    public static string Build(
        IReadOnlyCollection<Expense> expenses,
        IReadOnlyDictionary<Guid, string> categoryNames,
        Period currentPeriod)
    {
        var csv = new StringBuilder(Header).Append(LineEnd);

        var oneOffsByPeriod = expenses.OfType<OneOffExpense>().ToLookup(e => e.Period);
        var recurring = expenses.OfType<RecurringExpense>().ToList();
        var starts = oneOffsByPeriod.Select(g => g.Key).Concat(recurring.Select(e => e.StartPeriod)).ToList();
        if (starts.Count == 0)
            return csv.ToString();

        for (var period = starts.Min(); period <= currentPeriod; period = period.Next())
        {
            foreach (var expense in MonthProjector.Project(period, [.. oneOffsByPeriod[period], .. recurring]))
                AppendRow(csv, expense, categoryNames);
        }

        return csv.ToString();
    }

    private static void AppendRow(StringBuilder csv, EffectiveExpense expense, IReadOnlyDictionary<Guid, string> categoryNames)
    {
        var values = expense.Values;
        var category = values.CategoryId is { } id
            ? categoryNames.GetValueOrDefault(id, CategoryName.Uncategorized)
            : CategoryName.Uncategorized;

        string[] fields =
        [
            expense.Period.ToString(),
            values.Name,
            values.Amount.ToString(),
            category,
            FormatDate(values.DueDate),
            expense.PaidOn is null ? "Pendiente" : "Pagado",
            FormatDate(expense.PaidOn),
        ];

        csv.AppendJoin(',', fields.Select(Escape)).Append(LineEnd);
    }

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    private static string Escape(string field) =>
        field.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{field.Replace("\"", "\"\"")}\"" : field;
}
