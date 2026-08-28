using System.Globalization;

namespace IronReign.Helpers;

public class BoolToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is bool b && b;
        return isSelected ? Color.FromArgb("#11131A") : Color.FromArgb("#F5F7FA");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}