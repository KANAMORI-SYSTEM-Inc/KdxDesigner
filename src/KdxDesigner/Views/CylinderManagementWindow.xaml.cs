using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Views
{
    /// <summary>
    /// シリンダー管理ウィンドウ
    /// </summary>
    public partial class CylinderManagementWindow : Window
    {
        public CylinderManagementWindow(ISupabaseRepository repository, int plcId)
        {
            InitializeComponent();
            DataContext = new CylinderManagementViewModel(repository, plcId);
        }

        /// <summary>
        /// シリンダーグリッドのダブルクリックイベント
        /// </summary>
        private void CylinderGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is CylinderManagementViewModel vm && vm.SelectedCylinder != null)
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid?.SelectedItem != null)
                {
                    vm.EditCylinderCommand.Execute(null);
                }
            }
        }
    }
}
