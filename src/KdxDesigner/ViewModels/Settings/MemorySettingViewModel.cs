using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Services;
using KdxDesigner.Services.ErrorService;
using KdxDesigner.Services.MemonicTimerDevice;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.Services.MnemonicSpeedDevice;
using KdxDesigner.Utils;
using KdxDesigner.ViewModels;
using KdxDesigner.ViewModels.Managers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows;
using MemoryProfile = KdxDesigner.Models.MemoryProfile;
using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.Views
{
    /// <summary>
    /// メモリ設定ウィンドウのViewModel
    /// プロファイル管理とメモリ設定実行機能を提供
    /// </summary>
    public partial class MemorySettingViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private readonly MainViewModel _mainViewModel;
        private readonly MemoryProfileManager _profileManager;

        // サービス
        private readonly IMnemonicDeviceService? _mnemonicService;
        private readonly IMnemonicTimerDeviceService? _timerService;
        private readonly IProsTimeDeviceService? _prosTimeService;
        private readonly IMnemonicSpeedDeviceService? _speedService;
        private readonly IMemoryService? _memoryService;
        private readonly ErrorService? _errorService;
        private readonly MemoryConfigurationManager? _memoryConfig = null;

        // メモリ設定状態プロパティ
        public int TotalMemoryDeviceCount
        {
            get => _memoryConfig?.TotalMemoryDeviceCount ?? 0;
            set { if (_memoryConfig != null) _memoryConfig.TotalMemoryDeviceCount = value; }
        }
        public string MemoryConfigurationStatus
        {
            get => _memoryConfig?.MemoryConfigurationStatus ?? "未設定";
            set { if (_memoryConfig != null) _memoryConfig.MemoryConfigurationStatus = value; }
        }
        public bool IsMemoryConfigured
        {
            get => _memoryConfig?.IsMemoryConfigured ?? false;
            set { if (_memoryConfig != null) _memoryConfig.IsMemoryConfigured = value; }
        }
        public string LastMemoryConfigTime
        {
            get => _memoryConfig?.LastMemoryConfigTime ?? string.Empty;
            set { if (_memoryConfig != null) _memoryConfig.LastMemoryConfigTime = value; }
        }

        // プロファイル関連
        [ObservableProperty]
        private ObservableCollection<MemoryProfile> _profiles = new();

        [ObservableProperty]
        private MemoryProfile? _selectedProfile;

        [ObservableProperty]
        private bool _isNewProfile;

        [ObservableProperty]
        private string _newProfileName = string.Empty;

        [ObservableProperty]
        private string _newProfileDescription = string.Empty;

        // デバイス開始番号設定
        [ObservableProperty]
        private int _processDeviceStartL = 14000;

        [ObservableProperty]
        private int _detailDeviceStartL = 15000;

        [ObservableProperty]
        private int _operationDeviceStartM = 20000;

        [ObservableProperty]
        private int _cylinderDeviceStartM = 30000;

        [ObservableProperty]
        private int _cylinderDeviceStartD = 5000;

        [ObservableProperty]
        private int _errorDeviceStartM = 120000;

        [ObservableProperty]
        private int _errorDeviceStartT = 2000;

        [ObservableProperty]
        private int _deviceStartT = 0;

        [ObservableProperty]
        private int _timerStartZR = 3000;

        [ObservableProperty]
        private int _prosTimeStartZR = 12000;

        [ObservableProperty]
        private int _prosTimePreviousStartZR = 24000;

        [ObservableProperty]
        private int _cyTimeStartZR = 30000;

        // メモリ保存フラグ
        [ObservableProperty]
        private bool _isProcessMemory;

        [ObservableProperty]
        private bool _isDetailMemory;

        [ObservableProperty]
        private bool _isOperationMemory;

        [ObservableProperty]
        private bool _isCylinderMemory;

        [ObservableProperty]
        private bool _isErrorMemory;

        [ObservableProperty]
        private bool _isTimerMemory;

        [ObservableProperty]
        private bool _isProsTimeMemory;

        [ObservableProperty]
        private bool _isCyTimeMemory;

        // 選択されたPLCとCycle（MainViewModelから取得）
        private PLC? SelectedPlc => _mainViewModel.SelectedPlc;
        private Cycle? SelectedCycle => _mainViewModel.SelectedCycle;
        private ObservableCollection<Process> Processes => _mainViewModel.Processes;
        private List<CylinderCycle>? _selectedCylinderCycles;

        public MemorySettingViewModel(ISupabaseRepository repository, MainViewModel mainViewModel)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _profileManager = new MemoryProfileManager();

            // サービスの取得（MainViewModelから）
            _mnemonicService = mainViewModel._mnemonicService;
            _timerService = mainViewModel._timerService;
            _prosTimeService = mainViewModel._prosTimeService;
            _speedService = mainViewModel._speedService;
            _memoryService = mainViewModel._memoryService;
            _errorService = mainViewModel._errorService;
            _memoryConfig = mainViewModel._memoryConfig;

            // プロファイルの読み込み
            LoadProfiles();

            // MainViewModelの現在の設定を読み込み
            LoadCurrentSettingsFromMainViewModel();
        }

        /// <summary>
        /// プロファイル一覧を読み込み
        /// </summary>
        private void LoadProfiles()
        {
            var profiles = _profileManager.LoadProfiles();
            Profiles = new ObservableCollection<MemoryProfile>(profiles);

            if (SelectedPlc != null)
                Profiles = ToObservableCollection(Profiles.Where(p => p.PlcId == SelectedPlc.Id));

            // 前回使用したプロファイルまたはデフォルトプロファイルを選択
            var lastProfileId = SettingsManager.Settings.LastUsedMemoryProfileId;
            if (!string.IsNullOrEmpty(lastProfileId))
            {
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == lastProfileId);
            }

            if (SelectedProfile == null)
            {
                SelectedProfile = Profiles.FirstOrDefault(p => p.IsDefault);
            }
        }

        /// <summary>
        /// MainViewModelの現在の設定を読み込み
        /// </summary>
        private void LoadCurrentSettingsFromMainViewModel()
        {
            ProcessDeviceStartL = _mainViewModel.ProcessDeviceStartL;
            DetailDeviceStartL = _mainViewModel.DetailDeviceStartL;
            OperationDeviceStartM = _mainViewModel.OperationDeviceStartM;
            CylinderDeviceStartM = _mainViewModel.CylinderDeviceStartM;
            CylinderDeviceStartD = _mainViewModel.CylinderDeviceStartD;
            ErrorDeviceStartM = _mainViewModel.ErrorDeviceStartM;
            ErrorDeviceStartT = _mainViewModel.ErrorDeviceStartT;
            DeviceStartT = _mainViewModel.DeviceStartT;
            TimerStartZR = _mainViewModel.TimerStartZR;
            ProsTimeStartZR = _mainViewModel.ProsTimeStartZR;
            ProsTimePreviousStartZR = _mainViewModel.ProsTimePreviousStartZR;
            CyTimeStartZR = _mainViewModel.CyTimeStartZR;

            IsProcessMemory = _mainViewModel.IsProcessMemory;
            IsDetailMemory = _mainViewModel.IsDetailMemory;
            IsOperationMemory = _mainViewModel.IsOperationMemory;
            IsCylinderMemory = _mainViewModel.IsCylinderMemory;
            IsErrorMemory = _mainViewModel.IsErrorMemory;
            IsTimerMemory = _mainViewModel.IsTimerMemory;
            IsProsTimeMemory = _mainViewModel.IsProsTimeMemory;
            IsCyTimeMemory = _mainViewModel.IsCyTimeMemory;
        }

        /// <summary>
        /// 選択されたプロファイルが変更された時の処理
        /// </summary>
        partial void OnSelectedProfileChanged(MemoryProfile? value)
        {
            if (value != null && !IsNewProfile)
            {
                ApplyProfile(value);
            }
        }

        /// <summary>
        /// プロファイルを適用
        /// </summary>
        private void ApplyProfile(MemoryProfile profile)
        {
            ProcessDeviceStartL = profile.ProcessDeviceStartL;
            DetailDeviceStartL = profile.DetailDeviceStartL;
            OperationDeviceStartM = profile.OperationDeviceStartM;
            CylinderDeviceStartM = profile.CylinderDeviceStartM;
            CylinderDeviceStartD = profile.CylinderDeviceStartD;
            ErrorDeviceStartM = profile.ErrorDeviceStartM;
            ErrorDeviceStartT = profile.ErrorDeviceStartT;
            DeviceStartT = profile.DeviceStartT;
            TimerStartZR = profile.TimerStartZR;
            ProsTimeStartZR = profile.ProsTimeStartZR;
            ProsTimePreviousStartZR = profile.ProsTimePreviousStartZR;
            CyTimeStartZR = profile.CyTimeStartZR;
        }

        /// <summary>
        /// 新規プロファイル作成モードに切り替え
        /// </summary>
        [RelayCommand]
        private void CreateNewProfile()
        {
            IsNewProfile = true;
            NewProfileName = string.Empty;
            NewProfileDescription = string.Empty;
        }

        /// <summary>
        /// 新規プロファイルを保存
        /// </summary>
        [RelayCommand]
        private void SaveNewProfile()
        {
            if (string.IsNullOrWhiteSpace(NewProfileName))
            {
                MessageBox.Show("プロファイル名を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newProfile = new MemoryProfile
            {
                Name = NewProfileName,
                PlcId = SelectedPlc?.Id ?? 2,
                Description = NewProfileDescription,
                ProcessDeviceStartL = ProcessDeviceStartL,
                DetailDeviceStartL = DetailDeviceStartL,
                OperationDeviceStartM = OperationDeviceStartM,
                CylinderDeviceStartM = CylinderDeviceStartM,
                CylinderDeviceStartD = CylinderDeviceStartD,
                ErrorDeviceStartM = ErrorDeviceStartM,
                ErrorDeviceStartT = ErrorDeviceStartT,
                DeviceStartT = DeviceStartT,
                TimerStartZR = TimerStartZR,
                ProsTimeStartZR = ProsTimeStartZR,
                ProsTimePreviousStartZR = ProsTimePreviousStartZR,
                CyTimeStartZR = CyTimeStartZR
            };

            _profileManager.SaveProfile(newProfile);
            LoadProfiles();
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == newProfile.Id);
            IsNewProfile = false;

            MessageBox.Show($"プロファイル '{NewProfileName}' を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 新規プロファイル作成をキャンセル
        /// </summary>
        [RelayCommand]
        private void CancelNewProfile()
        {
            IsNewProfile = false;
            if (SelectedProfile != null)
            {
                ApplyProfile(SelectedProfile);
            }
        }

        /// <summary>
        /// メモリ設定を実行
        /// </summary>
        [RelayCommand]
        private async Task ExecuteMemorySetting()
        {
            if (!ValidateMemorySettings()) return;

            // 選択されたプロファイルを保存
            if (SelectedProfile != null)
            {
                SettingsManager.Settings.LastUsedMemoryProfileId = SelectedProfile.Id;
                SettingsManager.Save();
            }

            // MainViewModelに設定を反映
            ApplySettingsToMainViewModel();

            // メモリ設定状態を「設定中」に更新
            if (_memoryConfig == null) return;
            _memoryConfig.MemoryConfigurationStatus = "設定中...";
            _memoryConfig.IsMemoryConfigured = false;

            // 進捗ウィンドウを作成
            var progressViewModel = new MemoryProgressViewModel();
            var progressWindow = new MemoryProgressWindow
            {
                DataContext = progressViewModel,
                Owner = Application.Current.Windows.OfType<MemorySettingWindow>().FirstOrDefault()
            };

            // 進捗ウィンドウを非モーダルで表示
            progressWindow.Show();

            // UIスレッドをブロックしないようにTask.Runで実行
            await Task.Run(async () =>
            {
                try
                {
                    progressViewModel.UpdateStatus("メモリ設定を開始しています...");
                    await Task.Delay(100); // UIの更新を待つ

                    // データ準備
                    progressViewModel.UpdateStatus("データを準備しています...");
                    var prepData = await PrepareDataForMemorySetting();

                    if (prepData == null)
                    {
                        progressViewModel.MarkError("データ準備に失敗しました");
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show("データ準備に失敗しました。CycleまたはPLCが選択されているか確認してください。", "エラー"));
                        return;
                    }

                    // Mnemonic/Timerテーブルへの保存
                    await SaveMnemonicAndTimerDevices(prepData.Value, progressViewModel);

                    // Memoryテーブルへの保存
                    await SaveMemoriesToMemoryTableAsync(prepData.Value, progressViewModel);

                    progressViewModel.MarkCompleted();
                }
                catch (Exception ex)
                {
                    progressViewModel.MarkError(ex.Message);
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"メモリ設定中にエラーが発生しました: {ex.Message}", "エラー"));
                }
            });
        }

        [RelayCommand]
        private void ShowMemoryDeviceList()
        {
            try
            {
                // メモリストアを取得
                var memoryStore = App.Services?.GetService<IMnemonicDeviceMemoryStore>()
                    ?? new MnemonicDeviceMemoryStore();

                // 現在選択中のPLCとCycleを渡してウィンドウを開く
                var window = new MemoryDeviceListWindow(
                    memoryStore,
                    SelectedPlc?.Id,
                    SelectedCycle?.Id);

                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"メモリデバイス一覧の表示に失敗しました。\n{ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// MainViewModelに設定を反映
        /// </summary>
        private void ApplySettingsToMainViewModel()
        {
            _mainViewModel.ProcessDeviceStartL = ProcessDeviceStartL;
            _mainViewModel.DetailDeviceStartL = DetailDeviceStartL;
            _mainViewModel.OperationDeviceStartM = OperationDeviceStartM;
            _mainViewModel.CylinderDeviceStartM = CylinderDeviceStartM;
            _mainViewModel.CylinderDeviceStartD = CylinderDeviceStartD;
            _mainViewModel.ErrorDeviceStartM = ErrorDeviceStartM;
            _mainViewModel.ErrorDeviceStartT = ErrorDeviceStartT;
            _mainViewModel.DeviceStartT = DeviceStartT;
            _mainViewModel.TimerStartZR = TimerStartZR;
            _mainViewModel.ProsTimeStartZR = ProsTimeStartZR;
            _mainViewModel.ProsTimePreviousStartZR = ProsTimePreviousStartZR;
            _mainViewModel.CyTimeStartZR = CyTimeStartZR;

            _mainViewModel.IsProcessMemory = IsProcessMemory;
            _mainViewModel.IsDetailMemory = IsDetailMemory;
            _mainViewModel.IsOperationMemory = IsOperationMemory;
            _mainViewModel.IsCylinderMemory = IsCylinderMemory;
            _mainViewModel.IsErrorMemory = IsErrorMemory;
            _mainViewModel.IsTimerMemory = IsTimerMemory;
            _mainViewModel.IsProsTimeMemory = IsProsTimeMemory;
            _mainViewModel.IsCyTimeMemory = IsCyTimeMemory;
        }

        /// <summary>
        /// メモリ設定の検証
        /// </summary>
        private bool ValidateMemorySettings()
        {
            var errorMessages = new List<string>();
            if (SelectedCycle == null) errorMessages.Add("Cycleが選択されていません。");
            if (SelectedPlc == null) errorMessages.Add("PLCが選択されていません。");

            if (errorMessages.Any())
            {
                MessageBox.Show(string.Join("\n", errorMessages), "入力エラー");
                return false;
            }
            return true;
        }

        /// <summary>
        /// メモリ設定用のデータを準備
        /// </summary>
        private async Task<(
            List<ProcessDetail> details,
            List<Cylinder> cylinders,
            List<Operation> operations,
            List<IO> ioList,
            List<Timer> timers)?> PrepareDataForMemorySetting()
        {
            if (SelectedCycle == null || _repository == null || SelectedPlc == null)
            {
                return null;
            }

            List<ProcessDetail> details = (await _repository
                .GetProcessDetailsAsync())
                .Where(d => d.CycleId == SelectedCycle.Id).OrderBy(d => d.SortNumber).ToList();

            List<Cylinder> cylinders = (await _repository.GetCYsAsync())
                .Where(o => o.PlcId == SelectedPlc.Id).OrderBy(c => c.SortNumber).ToList();
            _selectedCylinderCycles = await _repository.GetCylinderCyclesByPlcIdAsync(SelectedPlc.Id);

            List<Cylinder> filteredCylinders = new List<Cylinder>();

            if (_selectedCylinderCycles == null || _selectedCylinderCycles.Count == 0)
            {
                MessageBox.Show("CylinderCycleのデータが存在しません。CylinderCycleの設定を確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                filteredCylinders = cylinders.ToList();
            }

            var operationIds = details.Select(c => c.OperationId).ToHashSet();
            List<Operation> operations = (await _repository.GetOperationsAsync()).ToList();

            var op = operations
                .Where(o => o.CycleId == SelectedCycle.Id)
                .OrderBy(o => o.SortNumber).ToList();
            var ioList = await _repository.GetIoListAsync();
            var timers = await _repository.GetTimersAsync();
            var Cycles = await _repository.GetCyclesAsync();

            Cycles = Cycles.Where(c => c.PlcId == SelectedPlc.Id).ToList();

            List<Timer> filteredTimers = new List<Timer>();

            foreach (var cycle in Cycles)
            {
                filteredTimers.AddRange(timers.Where(t => t.CycleId == cycle.Id).ToList());
            }

            return (details, filteredCylinders, op, ioList, filteredTimers);
        }

        /// <summary>
        /// Mnemonic* と Timer* テーブルへのデータ保存
        /// </summary>
        private async Task SaveMnemonicAndTimerDevices(
            (List<ProcessDetail> details,
            List<Cylinder> cylinders,
            List<Operation> operations, List<IO> ioList, List<Timer> timers) prepData,
            MemoryProgressViewModel? progressViewModel = null)
        {
            progressViewModel?.UpdateStatus("既存のニーモニックデバイスを削除中...");
            await _mnemonicService!.DeleteAllMnemonicDevices();

            progressViewModel?.UpdateStatus($"工程デバイスを保存中... ({Processes.Count}件)");
            _mnemonicService!.SaveMnemonicDeviceProcess(Processes.ToList(), ProcessDeviceStartL, SelectedPlc!.Id);

            progressViewModel?.UpdateStatus($"工程詳細デバイスを保存中... ({prepData.details.Count}件)");
            await _mnemonicService!.SaveMnemonicDeviceProcessDetail(prepData.details, DetailDeviceStartL, SelectedPlc!.Id);

            progressViewModel?.UpdateStatus($"操作デバイスを保存中... ({prepData.operations.Count}件)");
            _mnemonicService!.SaveMnemonicDeviceOperation(prepData.operations, OperationDeviceStartM, SelectedPlc!.Id);

            progressViewModel?.UpdateStatus($"シリンダーデバイスを保存中... ({prepData.cylinders.Count}件)");
            await _mnemonicService!.SaveMnemonicDeviceCY(prepData.cylinders, CylinderDeviceStartM, SelectedPlc!.Id);

            if (_repository == null || _timerService == null || _errorService == null || _prosTimeService == null || _speedService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            var timer = prepData.timers;
            var details = prepData.details;
            var operations = prepData.operations;
            var cylinders = prepData.cylinders;

            int timerCount = 0;

            if (_timerService == null)
            {
                MessageBox.Show("TimerServiceが初期化されていません。", "エラー");
                return;
            }

            progressViewModel?.UpdateStatus("既存のタイマーデバイスを削除中...");
            await _repository.DeleteAllMnemonicTimerDeviceAsync();

            progressViewModel?.UpdateStatus("工程詳細のタイマーを保存中...");
            timerCount += await _timerService.SaveWithDetail(timer, details, DeviceStartT, SelectedPlc!.Id, timerCount);

            progressViewModel?.UpdateStatus("操作のタイマーを保存中...");
            timerCount += await _timerService.SaveWithOperation(timer, operations, DeviceStartT, SelectedPlc!.Id, timerCount);

            progressViewModel?.UpdateStatus("シリンダーのタイマーを保存中...");
            timerCount += await _timerService.SaveWithCY(timer, cylinders, DeviceStartT, SelectedPlc!.Id, timerCount);
            progressViewModel?.AddLog($"タイマーデバイス保存完了 (合計: {timerCount}件)");

            // Errorテーブルの保存
            progressViewModel?.UpdateStatus("エラーテーブルを保存中...");
            await _errorService!.DeleteErrorTable();
            await _errorService!.SaveMnemonicDeviceOperation(prepData.operations, prepData.ioList, ErrorDeviceStartM, ErrorDeviceStartT, SelectedPlc!.Id, SelectedCycle!.Id);
            progressViewModel?.AddLog("エラーテーブル保存完了");

            // ProsTimeテーブルの保存
            progressViewModel?.UpdateStatus("工程時間テーブルを保存中...");
            _prosTimeService!.DeleteProsTimeTable();
            _prosTimeService!.SaveProsTime(prepData.operations, ProsTimeStartZR, ProsTimePreviousStartZR, CyTimeStartZR, SelectedPlc!.Id);
            progressViewModel?.AddLog("工程時間テーブル保存完了");

            // Speedテーブルの保存
            progressViewModel?.UpdateStatus("速度テーブルを保存中...");
            _speedService!.DeleteSpeedTable();
            _speedService!.Save(prepData.cylinders, CylinderDeviceStartD, SelectedPlc!.Id);
            progressViewModel?.AddLog("速度テーブル保存完了");
        }

        /// <summary>
        /// Memoryテーブルへの保存処理
        /// </summary>
        private async Task SaveMemoriesToMemoryTableAsync(
            (List<ProcessDetail> details,
            List<Cylinder> cylinders,
            List<Operation> operations, List<IO> ioList, List<Timer> timers) prepData,
            MemoryProgressViewModel? progressViewModel = null)
        {
            if (_memoryService == null)
            {
                MessageBox.Show("MemoryServiceが初期化されていません。", "エラー");
                return;
            }

            progressViewModel?.UpdateStatus("デバイス情報を取得中...");
            var devices = await _mnemonicService!.GetMnemonicDevice(SelectedPlc!.Id);
            var timerDevices = await _timerService!.GetMnemonicTimerDevice(SelectedPlc!.Id, SelectedCycle!.Id);

            var devicesP = devices.Where(m => m.MnemonicId == (int)MnemonicType.Process).ToList();
            var devicesD = devices.Where(m => m.MnemonicId == (int)MnemonicType.ProcessDetail).ToList();
            var devicesO = devices.Where(m => m.MnemonicId == (int)MnemonicType.Operation).ToList();
            var devicesC = devices.Where(m => m.MnemonicId == (int)MnemonicType.CY).ToList();

            progressViewModel?.AddLog($"取得したデバイス数 - 工程: {devicesP.Count}, 工程詳細: {devicesD.Count}, 操作: {devicesO.Count}, シリンダー: {devicesC.Count}, タイマー: {timerDevices.Count}");

            int totalProgress = (IsProcessMemory ? devicesP.Count : 0) +
                                (IsDetailMemory ? devicesD.Count : 0) +
                                (IsOperationMemory ? devicesO.Count : 0) +
                                (IsCylinderMemory ? devicesC.Count : 0) +
                                (IsErrorMemory ? devicesC.Count : 0) +
                                (IsTimerMemory ? timerDevices.Count * 2 : 0);

            progressViewModel?.SetProgressMax(totalProgress);

            if (!await ProcessAndSaveMemoryAsync(IsErrorMemory, devicesC, async device => await _memoryService.SaveMnemonicMemories(_repository, device), "エラー", progressViewModel)) return;

            progressViewModel?.UpdateStatus("メモリ設定状態を更新中...");

            // メモリ設定状態を更新
            await UpdateMemoryConfigurationStatus();

            MessageBox.Show("すべてのメモリ保存が完了しました。");
        }

        /// <summary>
        /// Memory保存の繰り返し処理を共通化
        /// </summary>
        private async Task<bool> ProcessAndSaveMemoryAsync<T>(bool shouldProcess, IEnumerable<T> devices, Func<T, Task<bool>> saveAction, string categoryName, MemoryProgressViewModel? progressViewModel = null)
        {
            if (!shouldProcess)
            {
                progressViewModel?.AddLog($"{categoryName}情報: スキップ（設定で無効）");
                return true;
            }

            var deviceList = devices.ToList();
            progressViewModel?.UpdateStatus($"{categoryName}情報をMemoryテーブルに保存中... ({deviceList.Count}件)");
            MessageBox.Show($"{categoryName}情報をMemoryテーブルにデータを保存します。", "確認");

            int index = 0;
            foreach (var device in deviceList)
            {
                index++;
                progressViewModel?.UpdateStatus($"{categoryName}情報を保存中... ({index}/{deviceList.Count})");

                bool result = await saveAction(device);
                if (!result)
                {
                    progressViewModel?.MarkError($"{categoryName}情報の保存に失敗 (デバイス {index}/{deviceList.Count})");
                    MessageBox.Show($"Memoryテーブル（{categoryName}）の保存に失敗しました。", "エラー");
                    return false;
                }
                progressViewModel?.IncrementProgress();
            }
            progressViewModel?.AddLog($"{categoryName}情報の保存完了 ({deviceList.Count}件)");
            return true;
        }

        /// <summary>
        /// メモリ設定状態を更新
        /// </summary>
        private async Task UpdateMemoryConfigurationStatus()
        {
            if (SelectedPlc == null || SelectedCycle == null || _mnemonicService == null || _timerService == null || _speedService == null || _memoryConfig == null)
            {
                if (_memoryConfig != null)
                {
                    _memoryConfig.MemoryConfigurationStatus = "未設定";
                    _memoryConfig.IsMemoryConfigured = false;
                    _memoryConfig.TotalMemoryDeviceCount = 0;
                }
                return;
            }

            try
            {
                var devices = await _mnemonicService.GetMnemonicDevice(SelectedPlc.Id) ?? new List<MnemonicDevice>();
                var timerDevices = await _timerService.GetMnemonicTimerDevice(SelectedPlc.Id, SelectedCycle.Id) ?? new List<MnemonicTimerDevice>();
                var speedDevices = _speedService.GetMnemonicSpeedDevice(SelectedPlc.Id) ?? new List<MnemonicSpeedDevice>();

                int totalCount = devices.Count + timerDevices.Count + speedDevices.Count;
                _memoryConfig.TotalMemoryDeviceCount = totalCount;

                if (totalCount > 0)
                {
                    _memoryConfig.IsMemoryConfigured = true;
                    _memoryConfig.LastMemoryConfigTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                    var statusBuilder = new System.Text.StringBuilder();
                    statusBuilder.AppendLine($"設定済み (合計: {totalCount} デバイス)");

                    if (devices.Count > 0)
                    {
                        var processCount = devices.Count(d => d.MnemonicId == (int)MnemonicType.Process);
                        var detailCount = devices.Count(d => d.MnemonicId == (int)MnemonicType.ProcessDetail);
                        var operationCount = devices.Count(d => d.MnemonicId == (int)MnemonicType.Operation);
                        var cylinderCount = devices.Count(d => d.MnemonicId == (int)MnemonicType.CY);

                        if (processCount > 0) statusBuilder.AppendLine($"  工程: {processCount}");
                        if (detailCount > 0) statusBuilder.AppendLine($"  工程詳細: {detailCount}");
                        if (operationCount > 0) statusBuilder.AppendLine($"  操作: {operationCount}");
                        if (cylinderCount > 0) statusBuilder.AppendLine($"  シリンダ: {cylinderCount}");
                    }

                    if (timerDevices.Count > 0)
                        statusBuilder.AppendLine($"  タイマー: {timerDevices.Count}");

                    if (speedDevices.Count > 0)
                        statusBuilder.AppendLine($"  速度: {speedDevices.Count}");

                    _memoryConfig.MemoryConfigurationStatus = statusBuilder.ToString().TrimEnd();
                }
                else
                {
                    _memoryConfig.MemoryConfigurationStatus = "未設定";
                    _memoryConfig.IsMemoryConfigured = false;
                }
            }
            catch (Exception ex)
            {
                _memoryConfig.MemoryConfigurationStatus = $"エラー: {ex.Message}";
                _memoryConfig.IsMemoryConfigured = false;
                _memoryConfig.TotalMemoryDeviceCount = 0;
            }
            finally
            {
                // MemorySettingViewModelのプロパティ変更を通知
                OnPropertyChanged(nameof(TotalMemoryDeviceCount));
                OnPropertyChanged(nameof(MemoryConfigurationStatus));
                OnPropertyChanged(nameof(IsMemoryConfigured));
                OnPropertyChanged(nameof(LastMemoryConfigTime));

                // 注: MainViewModelのプロパティ変更通知は、
                // _memoryConfigのPropertyChangedイベントを通じて自動的に転送されます
                // （MainViewModel.cs:113-119行目参照）
            }
        }

        // 追加: IEnumerable<T> → ObservableCollection<T> への変換メソッド
        private static ObservableCollection<T> ToObservableCollection<T>(IEnumerable<T> source)
        {
            return new ObservableCollection<T>(source);
        }
    }
}
