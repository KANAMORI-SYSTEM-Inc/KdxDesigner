using Kdx.Contracts.DTOs;
using System.Globalization;
using System.Windows.Data;

namespace KdxDesigner.Utils
{
    public class NullCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "(なし)";

            if (value is TimerCategory category)
                return category.CategoryName ?? "(なし)";

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}