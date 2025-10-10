using KdxDesigner.ViewModels;
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

            // DataContextが設定された時にRequestCloseイベントを購読
            this.DataContextChanged += (sender, e) =>
            {
                if (this.DataContext is ProcessDetailPropertiesViewModel viewModel)
                {
                    viewModel.RequestClose += () => this.Close();
                }
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // イベントハンドラをクリア
            if (this.DataContext is ProcessDetailPropertiesViewModel viewModel)
            {
                viewModel.ClearEventHandlers();
            }
        }
    }
}
