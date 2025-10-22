using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Kdx.Contracts.DTOs;
using KdxDesigner.Models;
using Kdx.Infrastructure.Supabase.Repositories;
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
        private readonly ISupabaseRepository _repository;
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

        public TimerEditorViewModel(ISupabaseRepository repository, MainViewModel mainViewModel)
        {
            _repository = repository;
            _mainViewModel = mainViewModel;
            _timerDeviceService = new MnemonicTimerDeviceService(repository, mainViewModel);
            _allTimers = new List<TimerViewModel>();

            // MnemonicTypesの初期化
            _mnemonicTypes = new ObservableCollection<MnemonicTypeInfo>(MnemonicTypeInfo.GetAll());

            // タイマーカテゴリとサイクルの初期化
            _timerCategories = new ObservableCollection<TimerCategory>(Task.Run(async () => await _repository.GetTimerCategoryAsync()).GetAwaiter().GetResult());

            // 選択されているPLCに紐づくCycleのみを取得
            var allCycles = Task.Run(async () => await _repository.GetCyclesAsync()).GetAwaiter().GetResult();
            var filteredCycles = _mainViewModel.SelectedPlc != null
                ? allCycles.Where(c => c.PlcId == _mainViewModel.SelectedPlc.Id).ToList()
                : allCycles.ToList();
            _cycles = new ObservableCollection<Cycle>(filteredCycles);

            // nullを含むカテゴリリストを作成
            UpdateTimerCategoriesWithNull();

            // フィルタリング用のCollectionView
            FilteredTimers = CollectionViewSource.GetDefaultView(_allTimers);
            FilteredTimers.Filter = FilterTimers;

            // タイマーを非同期で読み込み（コンストラクタではawaitできないため）
            _ = LoadTimers();
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
            {
                System.Diagnostics.Debug.WriteLine($"[FilterTimers] item is not TimerViewModel");
                return false;
            }

            // MnemonicType未設定のみ表示フィルタ
            if (ShowUnassignedOnly && vm.MnemonicId != null)
            {
                System.Diagnostics.Debug.WriteLine($"[FilterTimers] タイマーID={vm.ID}: 未設定のみフィルタで除外（MnemonicId={vm.MnemonicId}）");
                return false;
            }

            // MnemonicTypeでフィルタ
            if (SelectedMnemonicType != null)
            {
                // Id = 0 は「未設定」を意味し、MnemonicIdがnullまたは0のものを表示
                if (SelectedMnemonicType.Id == 0)
                {
                    if (vm.MnemonicId.HasValue && vm.MnemonicId.Value != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FilterTimers] タイマーID={vm.ID}: 未設定フィルタで除外（MnemonicId={vm.MnemonicId}）");
                        return false;
                    }
                }
                else
                {
                    // それ以外の場合は完全一致
                    if (vm.MnemonicId != SelectedMnemonicType.Id)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FilterTimers] タイマーID={vm.ID}: MnemonicTypeフィルタで除外（SelectedMnemonicType={SelectedMnemonicType.Id}, タイマーのMnemonicId={vm.MnemonicId}）");
                        return false;
                    }
                }
            }

            // テキストフィルタ
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var searchTerm = FilterText.ToLower();
                var result = (vm.TimerName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.CategoryName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.CycleName?.ToLower().Contains(searchTerm) ?? false) ||
                       (vm.RecordIdsDisplay?.ToLower().Contains(searchTerm) ?? false);

                if (!result)
                {
                    System.Diagnostics.Debug.WriteLine($"[FilterTimers] タイマーID={vm.ID}: テキストフィルタで除外（FilterText={FilterText}, TimerName={vm.TimerName}）");
                }

                return result;
            }

            System.Diagnostics.Debug.WriteLine($"[FilterTimers] タイマーID={vm.ID}: フィルタ通過");
            return true;
        }

        private async Task LoadTimers()
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
            var timers = await _repository.GetTimersAsync();
            var cycles = await _repository.GetCyclesAsync();
            var timerCategories = await _repository.GetTimerCategoryAsync();

            // 選択されているPLCに紐づくCycleのIDリストを作成
            var selectedPlcCycleIds = _mainViewModel.SelectedPlc != null
                ? cycles.Where(c => c.PlcId == _mainViewModel.SelectedPlc.Id).Select(c => c.Id).ToHashSet()
                : cycles.Select(c => c.Id).ToHashSet();

            System.Diagnostics.Debug.WriteLine($"[LoadTimers] 全タイマー数: {timers.Count}");
            System.Diagnostics.Debug.WriteLine($"[LoadTimers] 選択されたPLC: {_mainViewModel.SelectedPlc?.Id}");
            System.Diagnostics.Debug.WriteLine($"[LoadTimers] 選択されたPLCのCycle数: {selectedPlcCycleIds.Count}");
            System.Diagnostics.Debug.WriteLine($"[LoadTimers] 選択されたPLCのCycleIds: {string.Join(", ", selectedPlcCycleIds)}");

            // 選択されているPLCに紐づくCycleのタイマーのみをフィルタリング
            var filteredTimers = timers.Where(t => !t.CycleId.HasValue || selectedPlcCycleIds.Contains(t.CycleId.Value)).ToList();

            System.Diagnostics.Debug.WriteLine($"[LoadTimers] フィルタリング後のタイマー数: {filteredTimers.Count}");

            // 新規追加されたタイマーがフィルタリングで除外されていないか確認
            foreach (var timer in timers.OrderByDescending(t => t.ID).Take(5))
            {
                var isFiltered = !timer.CycleId.HasValue || selectedPlcCycleIds.Contains(timer.CycleId.Value);
                System.Diagnostics.Debug.WriteLine($"  タイマーID={timer.ID}, CycleId={timer.CycleId}, フィルタ通過={isFiltered}");
            }

            foreach (var timer in filteredTimers)
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
                else
                {
                    // MnemonicIdがnullの場合は「未設定」と表示
                    vm.MnemonicTypeName = "未設定";
                }

                // 中間テーブルからRecordIdsを読み込む
                var recordIds = await _repository.GetTimerRecordIdsAsync(timer.ID);
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
        
        private async void OnTimerMnemonicTypeChanged(object? sender, EventArgs e)
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
                    await _repository.DeleteAllTimerRecordIdsAsync(timer.ID);
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
        private async Task AddTimer()
        {
            // 現在表示されているタイマーリストから次のTimerNumを計算
            var maxTimerNum = _allTimers
                .Where(t => t.TimerNum.HasValue)
                .Select(t => t.TimerNum!.Value)
                .DefaultIfEmpty(0)
                .Max();

            // 新しいタイマーを作成（IDは0のままでデータベース側で自動採番される）
            var newTimer = new Timer
            {
                ID = 0, // データベース側のIDENTITYで自動採番
                CycleId = _mainViewModel.SelectedCycle?.Id,
                TimerName = null,
                TimerNum = maxTimerNum + 1, // 現在表示されているタイマーリストから+1
                TimerCategoryId = null,
                MnemonicId = 0, // デフォルトのMnemonicType
                Example = null
            };

            System.Diagnostics.Debug.WriteLine($"[AddTimer] 追加するタイマー:");
            System.Diagnostics.Debug.WriteLine($"  CycleId: {newTimer.CycleId}");
            System.Diagnostics.Debug.WriteLine($"  TimerName: {newTimer.TimerName}");
            System.Diagnostics.Debug.WriteLine($"  SelectedPlc: {_mainViewModel.SelectedPlc?.Id}");
            System.Diagnostics.Debug.WriteLine($"  SelectedCycle: {_mainViewModel.SelectedCycle?.Id}");

            var newId = await _repository.AddTimerAsync(newTimer);

            System.Diagnostics.Debug.WriteLine($"[AddTimer] 新規タイマーID: {newId}");

            // 追加されたタイマーのデータを更新
            newTimer.ID = newId;

            // 新規追加されたタイマーを表示するため、MnemonicTypeフィルタを一時的にクリア
            SelectedMnemonicType = null;
            ShowUnassignedOnly = false;

            // LoadTimersを呼ぶ代わりに、追加したタイマーを直接リストに追加
            // （データベースのコミットタイミングやキャッシュの問題で、LoadTimersで取得できないことがあるため）
            var vm = new TimerViewModel(newTimer);
            vm.LoadFromModel();

            // サイクル名を設定
            if (newTimer.CycleId.HasValue)
            {
                var cycles = await _repository.GetCyclesAsync();
                var cycle = cycles.FirstOrDefault(c => c.Id == newTimer.CycleId.Value);
                vm.CycleName = cycle?.CycleName ?? $"ID: {newTimer.CycleId}";
            }

            // MnemonicType名を設定
            if (newTimer.MnemonicId.HasValue)
            {
                var mnemonicType = MnemonicTypes.FirstOrDefault(m => m.Id == newTimer.MnemonicId.Value);
                vm.MnemonicTypeName = mnemonicType?.TableName ?? $"ID: {newTimer.MnemonicId}";
            }
            else
            {
                vm.MnemonicTypeName = "未設定";
            }

            // RecordIdsは空のリスト
            vm.RecordIds = new List<int>();

            // PropertyChangedイベントを監視
            vm.PropertyChanged += OnTimerPropertyChanged;

            // MnemonicType変更イベントを監視
            vm.OnMnemonicTypeChanged += OnTimerMnemonicTypeChanged;

            // リストに追加
            _allTimers.Add(vm);
            FilteredTimers.Refresh();

            System.Diagnostics.Debug.WriteLine($"[AddTimer] タイマーをリストに追加。タイマー数: {_allTimers.Count}");

            // 追加されたタイマーを選択状態にする
            SelectedTimer = vm;
            System.Diagnostics.Debug.WriteLine($"[AddTimer] 新規タイマーを選択: ID={newId}");
        }

        private async Task<int> GetNextTimerIdAsync()
        {
            // データベースから直接最大IDを取得
            var timers = await _repository.GetTimersAsync();
            var maxId = timers.Any() ? timers.Max(t => t.ID) : 0;
            return maxId + 1;
        }

        private async Task SyncRecordIdsToTableAsync(int timerId, string recordIdsText)
        {
            // 既存のRecordIdsを削除
            await _repository.DeleteAllTimerRecordIdsAsync(timerId);

            // RecordIdsをパースして中間テーブルに追加
            var idStrings = recordIdsText.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var idStr in idStrings)
            {
                if (int.TryParse(idStr.Trim(), out int recordId))
                {
                    await _repository.AddTimerRecordIdAsync(timerId, recordId);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteTimer()
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
                await _repository.DeleteAllTimerRecordIdsAsync(SelectedTimer.ID);

                // タイマー本体を削除
                await _repository.DeleteTimerAsync(SelectedTimer.ID);

                await LoadTimers();
            }
        }

        [RelayCommand]
        private async Task SaveSelectedTimer()
        {
            if (SelectedTimer == null || !SelectedTimer.IsDirty)
                return;

            try
            {
                var timer = SelectedTimer.GetModel();

                // タイマー本体を保存
                await _repository.UpdateTimerAsync(timer);

                // RecordIdsを中間テーブルに保存（既存のデータを削除してから追加）
                await _repository.DeleteAllTimerRecordIdsAsync(SelectedTimer.ID);
                foreach (var recordId in SelectedTimer.RecordIds)
                {
                    await _repository.AddTimerRecordIdAsync(SelectedTimer.ID, recordId);
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
                else
                {
                    SelectedTimer.MnemonicTypeName = "未設定";
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
        private async Task GenerateMnemonicTimerDevices()
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
                    await _repository.DeleteAllMnemonicTimerDeviceAsync();
                    // 必要なデータを取得
                    var timers = await _repository.GetTimersAsync();
                    var details = await _repository.GetProcessDetailsAsync();
                    var operations = await _repository.GetOperationsAsync();
                    var cylinders = await _repository.GetCYsAsync();

                    int timerCount = 0;
                    int deviceStartT = 0; // TODO: 実際の値を取得する必要がある

                    // MnemonicTimerDeviceを生成
                    timerCount = await _timerDeviceService.SaveWithDetail(timers, details, deviceStartT, _mainViewModel.SelectedPlc.Id, timerCount);
                    timerCount = await _timerDeviceService.SaveWithOperation(timers, operations, deviceStartT, _mainViewModel.SelectedPlc.Id, timerCount);
                    timerCount = await _timerDeviceService.SaveWithCY(timers, cylinders, deviceStartT, _mainViewModel.SelectedPlc.Id, timerCount);

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
        private async Task Refresh()
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
            await LoadTimers();
        }
        
        [RelayCommand]
        private async Task SaveAllTimers()
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
                    await _repository.UpdateTimerAsync(timerModel);

                    // RecordIdsを中間テーブルに保存（RecordIdsが変更された場合のみ）
                    await _repository.DeleteAllTimerRecordIdsAsync(timer.ID);
                    foreach (var recordId in timer.RecordIds)
                    {
                        await _repository.AddTimerRecordIdAsync(timer.ID, recordId);
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
                    else
                    {
                        timer.MnemonicTypeName = "未設定";
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

            // 選択されているPLCのIDを取得
            var plcId = _mainViewModel.SelectedPlc?.Id ?? 0;

            // RecordIds編集ダイアログを開く
            var dialog = new RecordIdsEditorDialog(SelectedTimer, _repository, plcId)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                // RecordIdsが更新されたので表示を更新
                SelectedTimer.RecordIdsDisplay = string.Join(", ", SelectedTimer.RecordIds);

                // DataGridの表示を更新
                FilteredTimers.Refresh();

                // 変更をダーティとしてマーク
                SelectedTimer.IsDirty = true;
                UpdateCanSave();
            }
        }
        
        partial void OnShowUnassignedOnlyChanged(bool value)
        {
            FilteredTimers.Refresh();
        }
    }
}
