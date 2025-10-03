using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using KdxDesigner.ViewModels;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Views
{
    public partial class MemoryProfileView : Window
    {
        public MemoryProfileView(MainViewModel mainViewModel, ISupabaseRepository repository)
        {
            InitializeComponent();
            DataContext = new MemoryProfileViewModel(mainViewModel, repository);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class BoolToDefaultTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDefault && isDefault)
            {
                return "(デフォルト)";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}