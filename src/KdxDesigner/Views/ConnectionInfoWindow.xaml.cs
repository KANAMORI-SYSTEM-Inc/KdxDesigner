using System.Windows;

namespace KdxDesigner.Views
{
    /// <summary>
    /// ConnectionInfoWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConnectionInfoWindow : Window
    {
        public ConnectionInfoWindow()
        {
            InitializeComponent();
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}