using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
using System.Windows;
using Process = Kdx.Contracts.DTOs.Process;

namespace KdxDesigner.Views
{
    public partial class ProcessPropertiesWindow : Window
    {
        private readonly ProcessPropertiesViewModel _viewModel;

        public ProcessPropertiesWindow(ISupabaseRepository repository, Process process)
        {
            InitializeComponent();
            _viewModel = new ProcessPropertiesViewModel(repository, process);
            DataContext = _viewModel;

            // ViewModelからのクローズ要求を処理
            _viewModel.RequestClose += () =>
            {
                // ShowDialog()で表示された場合のみDialogResultを設定可能
                // Show()で表示された場合は設定するとInvalidOperationExceptionが発生する
                if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
                {
                    DialogResult = _viewModel.DialogResult;
                }
                Close();
            };
        }
    }
}
