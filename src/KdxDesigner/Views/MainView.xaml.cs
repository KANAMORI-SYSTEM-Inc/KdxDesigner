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

        private void ProcessGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && DataContext is MainViewModel vm)
            {
                if (vm.SelectedProcess != null)
                {
                    vm.DeleteSelectedProcessCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void ProcessDetailGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && DataContext is MainViewModel vm)
            {
                if (vm.SelectedProcessDetail != null)
                {
                    vm.DeleteSelectedProcessDetailCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void OperationGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && DataContext is MainViewModel vm)
            {
                var grid = sender as DataGrid;
                if (grid?.SelectedItem != null)
                {
                    vm.DeleteSelectedOperationCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void ProcessGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedProcess != null)
            {
                // ヘッダーやスクロールバーをダブルクリックした場合は無視
                var dataGrid = sender as DataGrid;
                if (dataGrid?.SelectedItem != null)
                {
                    vm.EditProcessCommand.Execute(null);
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
