using Kdx.Contracts.DTOs;
using KdxDesigner.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KdxDesigner.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = App.Services!.GetRequiredService<MainViewModel>(); // 修正: 'App.Services' を型名でアクセス  
        }

        private void ProcessGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selected = ProcessGrid.SelectedItems.Cast<Process>().ToList();
                vm.UpdateSelectedProcesses(selected);

                // 単一選択用のSelectedProcessもセット
                if (ProcessGrid.SelectedItem is Process selectedProcess)
                {
                    vm.SelectedProcess = selectedProcess;
                }
            }
        }

        private void DetailGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selected = (sender as DataGrid)?.SelectedItem as ProcessDetail;
                if (selected != null)
                {
                    vm.OnProcessDetailSelected(selected);
                }
            }
        }


        private async void OperationGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // ヘッダーやスクロールバーをダブルクリックした場合は無視
                var dataGrid = sender as DataGrid;
                if (dataGrid?.SelectedItem is Operation selectedOperation)
                {
                    var plcId = vm.SelectedPlc?.Id;
                    var window = new OperationPropertiesWindow(vm.Repository!, selectedOperation, plcId)
                    {
                        Owner = this
                    };
                    if (window.ShowDialog() == true)
                    {
                        // 更新後にOperationリストを再読み込み
                        await vm.ReloadOperationsAsync();
                    }
                }
            }
        }
    }
}
