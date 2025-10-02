using System;
using System.Globalization;
using System.Windows.Data;

namespace KdxDesigner.Views
{
    public class SubtractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string paramString && double.TryParse(paramString, out double paramValue))
            {
                return doubleValue - paramValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}