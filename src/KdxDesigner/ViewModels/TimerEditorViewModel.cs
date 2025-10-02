using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Kdx.Contracts.DTOs;
using KdxDesigner.Models;
using Kdx.Contracts.Interfaces;
using KdxDesigner.Services.MemonicTimerDevice;
using KdxDesigner.Views;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.ViewModels
{
    public partial class TimerEditorViewModel : ObservableObject
    {
        private readonly IAccessRepository _repository;
        private readonly MainViewModel _mainViewModel;
        private readonly MnemonicTimerDeviceService _timerDeviceService;
        private readonly List<TimerViewModel> _allTimers;

        [ObservableProperty]
        private ObservableCollection<MnemonicTypeInfo> _mnemonicTypes;

        [ObservableProperty]
        private MnemonicTypeInfo? _selectedMnemonicType;

        [ObservableProperty]
        private string? _filterText;

        [ObservableProperty]
        private TimerViewModel? _selectedTimer;

        [ObservableProperty]
        private bool _isTimerSelected;

        [ObservableProperty]
        private bool _canSaveSelectedTimer;

        [ObservableProperty]
        private ObservableCollection<TimerCategory> _timerCategories;

        [ObservableProperty]
        private ObservableCollection<Cycle> _cycles;

        [ObservableProperty]
        private bool _isLoading;
        
        [ObservableProperty]
        private bool _showUnassignedOnly;
        
        [ObservableProperty]
        private bool _hasChanges;
        
        [ObservableProperty]
        private ObservableCollection<TimerCategory?> _timerCategoriesWithNull = new();

        public ICollectionView FilteredTimers { get; }

        public TimerEditorViewModel(IAccessRepository repository, MainViewModel mainViewModel)
        {
            _repository = repository;
            _mainViewModel = mainViewModel;
            _timerDeviceService = new MnemonicTimerDeviceService(repository, mainViewModel);
            _allTimers = new List<TimerViewModel>();
            
            // MnemonicTypesの初期化
            _mnemonicTypes = new ObservableCollection<MnemonicTypeInfo>(MnemonicTypeInfo.GetAll());
            
            // タイマーカテゴリとサイクルの初期化
            _timerCategories = new ObservableCollection<TimerCategory>(_repository.GetTimerCategory());
            _cycles = new ObservableCollection<Cycle>(_repository.GetCycles());
            
            // nullを含むカテゴリリストを作成
            UpdateTimerCategoriesWithNull();
            
            // フィルタリング用のCollectionView
            FilteredTimers = CollectionViewSource.GetDefaultView(_allTimers);
            FilteredTimers.Filter = FilterTimers;
            
            LoadTimers();
        }

        partial void OnSelectedMnemonicTypeChanged(MnemonicTypeInfo? value)
        {
            FilteredTimers.Refresh();
        }

        partial void OnFilterTextChanged(string? value)
        {
            FilteredTimers.Refresh();
        }

        partial void OnSelectedTimerChanged(TimerViewModel? value)
        {
            IsTimerSelected = value != null;
            UpdateCanSave();
        }

        private bool FilterTimers(object item)
        {
            if (item is not TimerViewModel vm)
                return false;

            // MnemonicType未設定のみ表示フィルタ
            if (ShowUnassignedOnly && vm.MnemonicId != null)
                return false;

            // MnemonicTypeでフィルタ
            if (SelectedMnemonicType != null && vm.MnemonicId != SelectedMnemonicType.Id)
                return false;

            // テキストフィルタ
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var searchTerm = FilterText.ToLower();
                return (vm.TimerName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.CategoryName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.CycleName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.RecordIdsDisplay?.ToLower().Contains(searchTerm) ?? false);
            }

            return true;
        }

        private async void LoadTimers()
        {
            IsLoading = true;
            
            await Task.Run(() =>
            {
                Thread.Sleep(100); // UIの更新を確実にするための短い遅延
            });
            
            _allTimers.Clear();
            
            if (_mainViewModel.SelectedPlc == null)
            {
                IsLoading = false;
                return;
            }

            // Timerを取得
            var timers = _repository.GetTimers();
            var cycles = _repository.GetCycles();
            var timerCategories = _repository.GetTimerCategory();

            foreach (var timer in timers)
            {
                var vm = new TimerViewModel(timer);
                vm.LoadFromModel();
                
                // カテゴリ名を設定
                if (timer.TimerCategoryId.HasValue)
                {
                    var category = timerCategories.FirstOrDefault(c => c.Id == timer.TimerCategoryId.Value);
                    vm.CategoryName = category?.CategoryName ?? $"ID: {timer.TimerCategoryId}";
                }
                
                // サイクル名を設定
                if (timer.CycleId.HasValue)
                {
                    var cycle = cycles.FirstOrDefault(c => c.Id == timer.CycleId.Value);
                    vm.CycleName = cycle?.CycleName ?? $"ID: {timer.CycleId}";
                }
                
                // MnemonicType名を設定
                if (timer.MnemonicId.HasValue)
                {
                    var mnemonicType = MnemonicTypes.FirstOrDefault(m => m.Id == timer.MnemonicId.Value);
                    vm.MnemonicTypeName = mnemonicType?.TableName ?? $"ID: {timer.MnemonicId}";
                }
                
                // 中間テーブルからRecordIdsを読み込む
                var recordIds = _repository.GetTimerRecordIds(timer.ID);
                vm.RecordIds = recordIds;
                
                // PropertyChangedイベントを監視
                vm.PropertyChanged += OnTimerPropertyChanged;
                
                // MnemonicType変更イベントを監視
                vm.OnMnemonicTypeChanged += OnTimerMnemonicTypeChanged;
                
                _allTimers.Add(vm);
            }
            
            FilteredTimers.Refresh();
            IsLoading = false;
        }

        private void OnTimerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimerViewModel.IsDirty))
            {
                UpdateCanSave();
            }
        }
        
        private void OnTimerMnemonicTypeChanged(object? sender, EventArgs e)
        {
            if (sender is TimerViewModel timer)
            {
                var result = MessageBox.Show(
                    "MnemonicTypeが変更されました。\n既存のRecordIdsをクリアしますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    // RecordIdsをクリア
                    _repository.DeleteAllTimerRecordIds(timer.ID);
                    timer.RecordIds = new List<int>();
                }
            }
        }

        private void UpdateCanSave()
        {
            CanSaveSelectedTimer = SelectedTimer?.IsDirty ?? false;
            HasChanges = _allTimers.Any(t => t.IsDirty);
        }
        
        private void UpdateTimerCategoriesWithNull()
        {
            TimerCategoriesWithNull.Clear();
            TimerCategoriesWithNull.Add(null); // nullオプションを追加
            foreach (var category in TimerCategories)
            {
                TimerCategoriesWithNull.Add(category);
            }
        }

        [RelayCommand]
        private void AddTimer()
        {
            // 新しいタイマーを作成
            var newTimer = new Timer
            {
                ID = GetNextTimerId(),
                CycleId = _mainViewModel.SelectedCycle?.Id,
                TimerName = "新規タイマー"
            };
            
            _repository.AddTimer(newTimer);
            
            // RecordIdsプロパティは削除されたため、中間テーブルへの同期は不要
            
            LoadTimers();
        }

        private int GetNextTimerId()
        {
            // データベースから直接最大IDを取得
            var timers = _repository.GetTimers();
            var maxId = timers.Any() ? timers.Max(t => t.ID) : 0;
            return maxId + 1;
        }

        private void SyncRecordIdsToTable(int timerId, string recordIdsText)
        {
            // 既存のRecordIdsを削除
            _repository.DeleteAllTimerRecordIds(timerId);
            
            // RecordIdsをパースして中間テーブルに追加
            var idStrings = recordIdsText.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var idStr in idStrings)
            {
                if (int.TryParse(idStr.Trim(), out int recordId))
                {
                    _repository.AddTimerRecordId(timerId, recordId);
                }
            }
        }

        [RelayCommand]
        private void DeleteTimer()
        {
            if (SelectedTimer == null)
                return;
                
            var result = MessageBox.Show(
                $"タイマーを削除しますか？\n\nタイマー名: {SelectedTimer.TimerName}",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                // RecordIdsを先に削除
                _repository.DeleteAllTimerRecordIds(SelectedTimer.ID);
                
                // タイマー本体を削除
                _repository.DeleteTimer(SelectedTimer.ID);
                
                LoadTimers();
            }
        }

        [RelayCommand]
        private void SaveSelectedTimer()
        {
            if (SelectedTimer == null || !SelectedTimer.IsDirty)
                return;
                
            try
            {
                var timer = SelectedTimer.GetModel();
                
                // タイマー本体を保存
                _repository.UpdateTimer(timer);
                
                // RecordIdsを中間テーブルに保存（既存のデータを削除してから追加）
                _repository.DeleteAllTimerRecordIds(SelectedTimer.ID);
                foreach (var recordId in SelectedTimer.RecordIds)
                {
                    _repository.AddTimerRecordId(SelectedTimer.ID, recordId);
                }
                
                // カテゴリ名を更新
                if (SelectedTimer.TimerCategoryId.HasValue)
                {
                    var category = TimerCategories.FirstOrDefault(c => c.Id == SelectedTimer.TimerCategoryId.Value);
                    SelectedTimer.CategoryName = category?.CategoryName ?? $"ID: {SelectedTimer.TimerCategoryId}";
                }
                else
                {
                    SelectedTimer.CategoryName = null;
                }
                
                // サイクル名を更新
                if (SelectedTimer.CycleId.HasValue)
                {
                    var cycle = Cycles.FirstOrDefault(c => c.Id == SelectedTimer.CycleId.Value);
                    SelectedTimer.CycleName = cycle?.CycleName ?? $"ID: {SelectedTimer.CycleId}";
                }
                
                // MnemonicType名を更新
                if (SelectedTimer.MnemonicId.HasValue)
                {
                    var mnemonicType = MnemonicTypes.FirstOrDefault(m => m.Id == SelectedTimer.MnemonicId.Value);
                    SelectedTimer.MnemonicTypeName = mnemonicType?.TableName ?? $"ID: {SelectedTimer.MnemonicId}";
                }
                
                SelectedTimer.ResetDirty();
                UpdateCanSave();
                FilteredTimers.Refresh(); // グリッドを更新（再読み込みではなく表示のみ更新）
                
                MessageBox.Show("保存しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void GenerateMnemonicTimerDevices()
        {
            if (_mainViewModel.SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "TimerテーブルからMnemonicTimerDeviceを生成します。\n既存のデータは削除されます。\n続行しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 既存のMnemonicTimerDeviceを削除
                    _repository.DeleteAllMnemonicTimerDevices();
                    // 必要なデータを取得
                    var timers = _repository.GetTimers();
                    var details = _repository.GetProcessDetails();
                    var operations = _repository.GetOperations();
                    var cylinders = _repository.GetCYs();
                    
                    int timerCount = 0;
                    int deviceStartT = 0; // TODO: 実際の値を取得する必要がある
                    
                    // MnemonicTimerDeviceを生成
                    _timerDeviceService.SaveWithDetail(timers, details, deviceStartT, _mainViewModel.SelectedPlc.Id, ref timerCount);
                    _timerDeviceService.SaveWithOperation(timers, operations, deviceStartT, _mainViewModel.SelectedPlc.Id, ref timerCount);
                    _timerDeviceService.SaveWithCY(timers, cylinders, deviceStartT, _mainViewModel.SelectedPlc.Id, ref timerCount);
                    
                    MessageBox.Show($"MnemonicTimerDeviceを生成しました。\n生成数: {timerCount}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"生成中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ViewMnemonicTimerDevices()
        {
            // MnemonicTimerDevice表示画面を開く
            var dialog = new MnemonicTimerDeviceView(_repository, _mainViewModel)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        [RelayCommand]
        private void Refresh()
        {
            if (HasChanges)
            {
                var result = MessageBox.Show(
                    "保存されていない変更があります。\n破棄してもよろしいですか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                    
                if (result != MessageBoxResult.Yes)
                    return;
            }
            LoadTimers();
        }
        
        [RelayCommand]
        private void SaveAllTimers()
        {
            var dirtyTimers = _allTimers.Where(t => t.IsDirty).ToList();
            if (!dirtyTimers.Any())
            {
                MessageBox.Show("変更されたタイマーはありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            try
            {
                foreach (var timer in dirtyTimers)
                {
                    var timerModel = timer.GetModel();
                    _repository.UpdateTimer(timerModel);
                    
                    // RecordIdsを中間テーブルに保存（RecordIdsが変更された場合のみ）
                    _repository.DeleteAllTimerRecordIds(timer.ID);
                    foreach (var recordId in timer.RecordIds)
                    {
                        _repository.AddTimerRecordId(timer.ID, recordId);
                    }
                    
                    // カテゴリ名を更新
                    if (timer.TimerCategoryId.HasValue)
                    {
                        var category = TimerCategories.FirstOrDefault(c => c.Id == timer.TimerCategoryId.Value);
                        timer.CategoryName = category?.CategoryName ?? $"ID: {timer.TimerCategoryId}";
                    }
                    else
                    {
                        timer.CategoryName = null;
                    }
                    
                    // サイクル名を更新
                    if (timer.CycleId.HasValue)
                    {
                        var cycle = Cycles.FirstOrDefault(c => c.Id == timer.CycleId.Value);
                        timer.CycleName = cycle?.CycleName ?? $"ID: {timer.CycleId}";
                    }
                    
                    // MnemonicType名を更新
                    if (timer.MnemonicId.HasValue)
                    {
                        var mnemonicType = MnemonicTypes.FirstOrDefault(m => m.Id == timer.MnemonicId.Value);
                        timer.MnemonicTypeName = mnemonicType?.TableName ?? $"ID: {timer.MnemonicId}";
                    }
                    
                    timer.ResetDirty();
                }
                
                UpdateCanSave();
                FilteredTimers.Refresh(); // グリッドを更新（再読み込みではなく表示のみ更新）
                MessageBox.Show($"{dirtyTimers.Count}件のタイマーを保存しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        [RelayCommand]
        private void EditRecordIds()
        {
            if (SelectedTimer == null)
                return;

            // RecordIds編集ダイアログを開く
            var dialog = new RecordIdsEditorDialog(SelectedTimer, _repository)
            {
                Owner = Application.Current.MainWindow
            };
            
            if (dialog.ShowDialog() == true)
            {
                // RecordIdsが更新されたので表示を更新
                SelectedTimer.RecordIdsDisplay = string.Join(", ", SelectedTimer.RecordIds);
            }
        }
        
        partial void OnShowUnassignedOnlyChanged(bool value)
        {
            FilteredTimers.Refresh();
        }
    }
}
