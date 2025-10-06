using System.Windows;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Views
{
    /// <summary>
    /// IO割付表変換ウィンドウのコードビハインド
    /// </summary>
    public partial class IOConversionWindow : Window
    {
        public IOConversionViewModel ViewModel => (IOConversionViewModel)DataContext;

        public IOConversionWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
