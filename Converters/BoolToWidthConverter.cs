using System.Globalization;
using System.Windows.Data;

namespace MoodScan.Converters
{
    public class BoolToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? 250.0 : 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)value;
            return width > 0;
        }
    }
}
