using System.Windows;
using System.Windows.Controls;

namespace KdxDesigner.Views
{
    /// <summary>
    /// メモリ設定進捗ウィンドウ
    /// </summary>
    public partial class MemoryProgressWindow : Window
    {
        public MemoryProgressWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 自動スクロール
            LogScrollViewer.ScrollToEnd();
        }
    }
}
