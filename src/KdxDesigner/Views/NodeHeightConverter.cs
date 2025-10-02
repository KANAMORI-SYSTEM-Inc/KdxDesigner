using System;
using System.Globalization;
using System.Windows.Data;

namespace KdxDesigner.Views
{
    public class NodeHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool showId && values[1] is bool showBlockNumber)
            {
                if (showId && showBlockNumber)
                    return 60.0; // 両方表示
                else if (showId || showBlockNumber)
                    return 55.0; // どちらか一方を表示
                else
                    return 45.0; // 両方非表示
            }
            return 45.0; // デフォルト
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}