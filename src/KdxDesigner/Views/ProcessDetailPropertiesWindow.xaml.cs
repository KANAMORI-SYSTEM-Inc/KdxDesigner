using System.Windows;

namespace KdxDesigner.Views
{
    /// <summary>
    /// ProcessDetailPropertiesWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ProcessDetailPropertiesWindow : Window
    {
        public ProcessDetailPropertiesWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}