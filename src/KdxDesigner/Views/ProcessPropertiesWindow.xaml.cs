using System.Windows;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
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
                // 非モーダルウィンドウではDialogResultは不要
                Close();
            };
        }
    }
}