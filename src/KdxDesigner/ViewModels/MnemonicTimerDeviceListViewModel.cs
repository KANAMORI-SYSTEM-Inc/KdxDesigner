using CommunityToolkit.Mvvm.ComponentModel;
using Kdx.Contracts.DTOs;
using KdxDesigner.Models;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace KdxDesigner.ViewModels
{
    public partial class MnemonicTimerDeviceListViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private readonly MainViewModel _mainViewModel;
        private readonly List<MnemonicTimerDeviceViewModel> _allTimerDevices;

        [ObservableProperty]
        private ObservableCollection<MnemonicTypeInfo> _mnemonicTypes;

        [ObservableProperty]
        private MnemonicTypeInfo? _selectedMnemonicType;

        [ObservableProperty]
        private string? _filterText;

        public ICollectionView FilteredTimerDevices { get; }

        public MnemonicTimerDeviceListViewModel(ISupabaseRepository repository, MainViewModel mainViewModel)
        {
            _repository = repository;
            _mainViewModel = mainViewModel;
            _allTimerDevices = new List<MnemonicTimerDeviceViewModel>();
            
            // MnemonicTypesの初期化
            _mnemonicTypes = new ObservableCollection<MnemonicTypeInfo>(MnemonicTypeInfo.GetAll());
            
            // フィルタリング用のCollectionView
            FilteredTimerDevices = CollectionViewSource.GetDefaultView(_allTimerDevices);
            FilteredTimerDevices.Filter = FilterTimerDevices;
            
            LoadTimerDevices();
        }

        partial void OnSelectedMnemonicTypeChanged(MnemonicTypeInfo? value)
        {
            FilteredTimerDevices.Refresh();
        }

        partial void OnFilterTextChanged(string? value)
        {
            FilteredTimerDevices.Refresh();
        }

        private bool FilterTimerDevices(object item)
        {
            if (item is not MnemonicTimerDeviceViewModel vm)
                return false;

            // MnemonicTypeでフィルタ
            if (SelectedMnemonicType != null && vm.MnemonicId != SelectedMnemonicType.Id)
                return false;

            // テキストフィルタ
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var searchTerm = FilterText.ToLower();
                return (vm.RecordName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.TimerName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.CategoryName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.ProcessTimerDevice?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.TimerDevice?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.Comment1?.ToLower().Contains(searchTerm) ?? false);
            }

            return true;
        }

        private async void LoadTimerDevices()
        {
            _allTimerDevices.Clear();

            if (_mainViewModel.SelectedPlc == null)
                return;

            // MnemonicTimerDeviceを取得
            var timerDevices = await _repository.GetMnemonicTimerDevicesAsync();
            var timers = await _repository.GetTimersAsync();
            var timerCategories = await _repository.GetTimerCategoryAsync();

            // MnemonicIdごとにレコード情報を取得
            var processes = await _repository.GetProcessesAsync();
            var processDetails = await _repository.GetProcessDetailsAsync();
            var operations = await _repository.GetOperationsAsync();
            var cylinders = await _repository.GetCyListAsync(_mainViewModel.SelectedPlc.Id);

            foreach (var device in timerDevices.Where(d => d.PlcId == _mainViewModel.SelectedPlc.Id))
            {
                var vm = new MnemonicTimerDeviceViewModel(device);
                
                // Mnemonic名を設定
                var mnemonicType = MnemonicTypes.FirstOrDefault(m => m.Id == device.MnemonicId);
                vm.MnemonicName = mnemonicType?.TableName ?? $"Unknown({device.MnemonicId})";
                
                // レコード名を設定
                switch (device.MnemonicId)
                {
                    case 1: // Process
                        var process = processes.FirstOrDefault(p => p.Id == device.RecordId);
                        vm.RecordName = process?.ProcessName ?? $"ID: {device.RecordId}";
                        break;
                    case 2: // ProcessDetail
                        var detail = processDetails.FirstOrDefault(p => p.Id == device.RecordId);
                        vm.RecordName = detail?.DetailName ?? $"ID: {device.RecordId}";
                        break;
                    case 3: // Operation
                        var operation = operations.FirstOrDefault(o => o.Id == device.RecordId);
                        vm.RecordName = operation?.OperationName ?? $"ID: {device.RecordId}";
                        break;
                    case 4: // CY
                        var cylinder = cylinders.FirstOrDefault(c => c.Id == device.RecordId);
                        vm.RecordName = cylinder?.CYNum ?? $"ID: {device.RecordId}";
                        break;
                }
                
                // タイマー名を設定
                var timer = timers.FirstOrDefault(t => t.ID == device.TimerId);
                vm.TimerName = timer?.TimerName ?? $"ID: {device.TimerId}";
                
                // カテゴリ名を設定
                if (device.TimerCategoryId.HasValue)
                {
                    var category = timerCategories.FirstOrDefault(c => c.Id == device.TimerCategoryId.Value);
                    vm.CategoryName = category?.CategoryName ?? $"ID: {device.TimerCategoryId}";
                }
                
                _allTimerDevices.Add(vm);
            }
            
            FilteredTimerDevices.Refresh();
        }
    }
}