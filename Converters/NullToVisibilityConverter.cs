using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MediaScanner.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || (value is string str && string.IsNullOrEmpty(str)))
        {
            return Visibility.Visible; // Show placeholder when null
        }
        
        return Visibility.Collapsed; // Hide placeholder when has value
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}