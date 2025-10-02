using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KdxDesigner.Views
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // パラメータが"Inverse"の場合は反転
                if (parameter as string == "Inverse")
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                // 通常の変換
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                if (parameter as string == "Inverse")
                {
                    return visibility != Visibility.Visible;
                }
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}