using System.Windows;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Views
{
    public partial class CylinderPropertiesWindow : Window
    {
        private readonly CylinderPropertiesViewModel _viewModel;

        public CylinderPropertiesWindow(ISupabaseRepository repository, Cylinder cylinder)
        {
            InitializeComponent();
            _viewModel = new CylinderPropertiesViewModel(repository, cylinder);
            DataContext = _viewModel;

            // ViewModelからのクローズ要求を処理
            _viewModel.RequestClose += () =>
            {
                // ViewModelのDialogResultをWindowのDialogResultに反映
                DialogResult = _viewModel.DialogResult;
                Close();
            };
        }
    }
}
