using KdxDesigner.ViewModels;

using System.ComponentModel;
using System.Windows;

namespace KdxDesigner.Views
{
    public partial class IOSelectView : Window
    {
        // ViewModelをプロパティとして保持
        public IOSelectViewModel ViewModel { get; }

        public IOSelectView(IOSelectViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            // ViewModelのプロパティ変更を購読
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // DialogResult プロパティが変更されたら、このウィンドウのDialogResultに反映する
            if (e.PropertyName == nameof(IOSelectViewModel.DialogResult))
            {
                // ViewModelのDialogResultがnullでない場合のみ（つまり、ConfirmかCancelが押されたとき）
                if (ViewModel.DialogResult.HasValue)
                {
                    this.DialogResult = ViewModel.DialogResult;
                    // 一度DialogResultが設定されたら、イベントハンドラの購読を解除してクリーンアップ
                    ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                    this.Close();
                }
            }
        }
    }
}