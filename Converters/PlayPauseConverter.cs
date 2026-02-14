using System.Globalization;
using System.Windows.Data;
using FontAwesome.WPF;

namespace MoodScan.Converters
{
    public class PlayPauseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
            {
                return isPlaying ? FontAwesomeIcon.Pause : FontAwesomeIcon.Play;
            }
            return FontAwesomeIcon.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
