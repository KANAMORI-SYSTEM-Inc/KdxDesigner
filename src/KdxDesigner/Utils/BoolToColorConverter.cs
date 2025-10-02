using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KdxDesigner.Utils
{
    /// <summary>
    /// bool値を色に変換するコンバーター
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorPair = colors.Split('|');
                if (colorPair.Length == 2)
                {
                    var trueColor = colorPair[0];
                    var falseColor = colorPair[1];
                    
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(boolValue ? trueColor : falseColor);
                        return new SolidColorBrush(color);
                    }
                    catch
                    {
                        // カラー変換に失敗した場合はデフォルト色を返す
                        return new SolidColorBrush(Colors.Gray);
                    }
                }
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}