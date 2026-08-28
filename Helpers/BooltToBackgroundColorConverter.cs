using System.Globalization;

namespace IronReign.Helpers;

public class BoolToBackgroundColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is bool b && b;
        return isSelected ? Color.FromArgb("#FCA311") : Color.FromArgb("#232734");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}