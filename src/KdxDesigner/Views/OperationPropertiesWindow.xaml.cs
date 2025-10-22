using System.Windows;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Views
{
    public partial class OperationPropertiesWindow : Window
    {
        private readonly OperationViewModel _viewModel;
        private readonly ISupabaseRepository _repository;

        public OperationPropertiesWindow(ISupabaseRepository repository, Operation operation, int? plcId = null)
        {
            InitializeComponent();
            _repository = repository;
            _viewModel = new OperationViewModel(repository, operation, plcId);
            DataContext = _viewModel;

            // ViewModelからのクローズ要求を処理
            _viewModel.SetCloseAction(async (result) =>
            {
                if (result)
                {
                    // 保存処理
                    var updatedOperation = _viewModel.GetOperation();
                    await _repository.UpdateOperationAsync(updatedOperation);
                    DialogResult = true;
                }
                else
                {
                    DialogResult = false;
                }
                Close();
            });
        }
    }
}
