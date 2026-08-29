using System.Globalization;

namespace IronReign.Helpers;

public class ScheduledDaysToTextConverter : IValueConverter
{
    private static readonly string[] Labels = { "D", "L", "M", "X", "J", "V", "S" };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string;

        if (string.IsNullOrWhiteSpace(text))
            return "Sin días asignados";

        var days = text.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .OrderBy(d => (d + 6) % 7)
            .Select(d => Labels[d]);

        return string.Join(" · ", days);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
