using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KdxDesigner.ViewModels.IOEditor;

namespace KdxDesigner.Views
{
    public partial class IOSearchWindow : Window
    {
        public IOSearchViewModel ViewModel => (IOSearchViewModel)DataContext;

        public IOSearchWindow()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedIO != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("IOを選択してください。", "選択エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void IODataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedIO != null)
            {
                OKButton_Click(sender, e);
            }
        }
    }
}