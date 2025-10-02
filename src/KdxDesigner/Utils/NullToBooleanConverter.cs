using System;
using System.Globalization;
using System.Windows.Data;

namespace KdxDesigner.Utils.Converters
{
    public class NullToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// ソース（ViewModelのプロパティ）からターゲット（UIのプロパティ）へ変換します。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value が null でなければ true を、null ならば false を返す
            return value != null;
        }

        /// <summary>
        /// ターゲット（UI）からソース（ViewModel）へ戻す際の変換（今回は使用しない）。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // このコンバータは一方向で使うため、逆変換は実装しない
            throw new NotImplementedException();
        }
    }
}