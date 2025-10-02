using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KdxDesigner.Views
{
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int categoryId)
            {
                // カテゴリIDに基づいて色を返す
                return categoryId switch
                {
                    13 => new SolidColorBrush(Color.FromRgb(255, 200, 150)), // 工程OFF確認 - オレンジ系
                    15 => new SolidColorBrush(Color.FromRgb(150, 200, 255)), // 期間工程 - 青系
                    16 => new SolidColorBrush(Color.FromRgb(255, 255, 150)), // タイマ工程 - 黄色系
                    17 => new SolidColorBrush(Color.FromRgb(200, 255, 200)), // タイマ - 緑系
                    18 => new SolidColorBrush(Color.FromRgb(255, 150, 255)), // 複合工程 - 紫系
                    19 => new SolidColorBrush(Color.FromRgb(255, 150, 150)), // リセット - 赤系
                    _ => new SolidColorBrush(Color.FromRgb(220, 220, 220))   // デフォルト - グレー
                };
            }
            return new SolidColorBrush(Color.FromRgb(220, 220, 220)); // デフォルト
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}