using KdxDesigner.ViewModels;
using System.Windows;

namespace KdxDesigner.Views
{
    /// <summary>
    /// OutputWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class OutputWindow : Window
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="mainViewModel">MainViewModelへの参照</param>
        public OutputWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();

            // ViewModelを作成してDataContextに設定
            DataContext = new OutputViewModel(mainViewModel);
        }

        /// <summary>
        /// 閉じるボタンのクリックイベント
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
