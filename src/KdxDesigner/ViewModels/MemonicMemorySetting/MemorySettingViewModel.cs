using CommunityToolkit.Mvvm.ComponentModel;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Models;
using KdxDesigner.Services;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows;
using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.ViewModels.Settings
{
    /// <summary>
    /// メモリ設定ウィンドウのViewModel
    /// プロファイル管理とメモリ設定実行機能を提供
    /// </summary>
    public partial class MemorySettingViewModel : ObservableObject
    {
        public MemorySettingViewModel(ISupabaseRepository repository, MainViewModel mainViewModel)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _plcProfileManager = new PlcMemoryProfileManager();
            _cycleProfileManager = new CycleMemoryProfileManager();

            // サービスの取得（MainViewModelから）
            _mnemonicService = mainViewModel._mnemonicService;
            _timerService = mainViewModel._timerService;
            _prosTimeService = mainViewModel._prosTimeService;
            _speedService = mainViewModel._speedService;
            _memoryService = mainViewModel._memoryService;
            _errorService = mainViewModel._errorService;
            _memoryConfig = mainViewModel._memoryConfig;

            // プロファイルの読み込み
            LoadPlcProfiles();
            LoadCycleProfiles();

            // MainViewModelの現在の設定を読み込み
            LoadCurrentSettingsFromMainViewModel();
        }

        /// <summary>
        /// PLC用プロファイル一覧を読み込み
        /// プロファイルが存在しない場合は空のコレクションを表示
        /// </summary>
        private void LoadPlcProfiles()
        {
            var profiles = _plcProfileManager.LoadProfiles();
            PlcProfiles = new ObservableCollection<PlcMemoryProfile>(profiles);

            if (SelectedPlc != null)
                PlcProfiles = ToObservableCollection(PlcProfiles.Where(p => p.PlcId == SelectedPlc.Id));

            // プロファイルが存在する場合のみデフォルトを選択
            if (PlcProfiles.Any())
            {
                SelectedPlcProfile = PlcProfiles.FirstOrDefault(p => p.IsDefault) ?? PlcProfiles.First();
            }
            else
            {
                SelectedPlcProfile = null;
            }
        }

        /// <summary>
        /// Cycle用プロファイル一覧を読み込み
        /// プロファイルが存在しない場合は空のコレクションを表示
        /// </summary>
        private void LoadCycleProfiles()
        {
            var profiles = _cycleProfileManager.LoadProfiles();
            CycleProfiles = new ObservableCollection<CycleMemoryProfile>(profiles);

            if (SelectedPlc != null)
                CycleProfiles = ToObservableCollection(CycleProfiles.Where(p => p.PlcId == SelectedPlc.Id));

            // 既存の選択をクリア
            SelectedCycleProfiles.Clear();

            // プロファイルが存在する場合のみデフォルトを選択（単一選択）
            if (CycleProfiles.Any())
            {
                var defaultProfile = CycleProfiles.FirstOrDefault(p => p.IsDefault);
                if (defaultProfile != null)
                {
                    SelectedCycleProfiles.Add(defaultProfile);
                }
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
        /// PLC用プロファイルを適用
        /// Cylinder/Error/Timer/工程時間デバイスの設定をViewModelに反映
        /// </summary>
        private void ApplyPlcProfile(PlcMemoryProfile profile)
        {
            CylinderDeviceStartM = profile.CylinderDeviceStartM;
            CylinderDeviceStartD = profile.CylinderDeviceStartD;
            ErrorDeviceStartM = profile.ErrorDeviceStartM;
            ErrorDeviceStartT = profile.ErrorDeviceStartT;
            DeviceStartT = profile.DeviceStartT;
            TimerStartZR = profile.TimerStartZR;
            ProsTimeStartZR = profile.ProsTimeStartZR;
            ProsTimePreviousStartZR = profile.ProsTimePreviousStartZR;
            CyTimeStartZR = profile.CyTimeStartZR;

            // MainViewModelにも反映
            _mainViewModel.CylinderDeviceStartM = profile.CylinderDeviceStartM;
            _mainViewModel.CylinderDeviceStartD = profile.CylinderDeviceStartD;
            _mainViewModel.ErrorDeviceStartM = profile.ErrorDeviceStartM;
            _mainViewModel.ErrorDeviceStartT = profile.ErrorDeviceStartT;
            _mainViewModel.DeviceStartT = profile.DeviceStartT;
            _mainViewModel.TimerStartZR = profile.TimerStartZR;
            _mainViewModel.ProsTimeStartZR = profile.ProsTimeStartZR;
            _mainViewModel.ProsTimePreviousStartZR = profile.ProsTimePreviousStartZR;
            _mainViewModel.CyTimeStartZR = profile.CyTimeStartZR;
        }

        /// <summary>
        /// Cycle用プロファイルを適用
        /// Process/ProcessDetail/Operationデバイスの設定をViewModelに反映
        /// </summary>
        private void ApplyCycleProfile(CycleMemoryProfile profile)
        {
            ProcessDeviceStartL = profile.ProcessDeviceStartL;
            DetailDeviceStartL = profile.DetailDeviceStartL;
            OperationDeviceStartM = profile.OperationDeviceStartM;

            // MainViewModelにも反映
            _mainViewModel.ProcessDeviceStartL = profile.ProcessDeviceStartL;
            _mainViewModel.DetailDeviceStartL = profile.DetailDeviceStartL;
            _mainViewModel.OperationDeviceStartM = profile.OperationDeviceStartM;
        }

        /// <summary>
        /// 新プロファイルシステムの検証
        /// </summary>
        private bool ValidateNewProfileSystem()
        {
            var errorMessages = new List<string>();

            if (SelectedPlc == null)
            {
                errorMessages.Add("PLCが選択されていません。");
            }

            if (SelectedPlcProfile == null)
            {
                errorMessages.Add("PLC用プロファイルが選択されていません。");
            }

            if (SelectedCycleProfiles == null || !SelectedCycleProfiles.Any())
            {
                errorMessages.Add("Cycle用プロファイルが1つ以上選択されていません。");
            }

            if (errorMessages.Any())
            {
                MessageBox.Show(string.Join("\n", errorMessages), "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
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
        /// PLC全体のデバイスを保存（Cylinder, Error, Timer, ProsTime, Speed）
        /// ※PLC全体で1回のみ実行
        /// </summary>
        /// <param name="progressViewModel">進捗ViewModel</param>
        /// <param name="cycleTimerCount">Cycle処理で使用されたタイマー数（CylinderのTimerはこの後から開始）</param>
        private async Task SavePlcDevices(MemoryProgressViewModel? progressViewModel = null, int cycleTimerCount = 0)
        {
            if (SelectedPlc == null || _repository == null)
            {
                throw new InvalidOperationException("PLCまたはリポジトリが初期化されていません。");
            }

            // 既存のCYデバイスのみを削除（ProcessDetail/Operationデバイスは保持）
            // 注: DeleteAllMnemonicDevices()を呼び出すと、Cycleで保存したProcessDetail/Operationデバイスも
            // すべて削除されてしまうため、CYデバイスのみを削除する
            progressViewModel?.UpdateStatus("既存のシリンダーデバイスを削除中...");
            await _mnemonicService!.DeleteMnemonicDevice(SelectedPlc.Id, (int)MnemonicType.CY);

            // Cylinderデータの取得
            progressViewModel?.UpdateStatus("シリンダーデータを取得中...");
            var cylinders = (await _repository.GetCYsAsync())
                .Where(c => c.PlcId == SelectedPlc.Id)
                .OrderBy(c => c.SortNumber)
                .ToList();

            var cylinderCycles = await _repository.GetCylinderCyclesByPlcIdAsync(SelectedPlc.Id);
            if (cylinderCycles == null || cylinderCycles.Count == 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show("CylinderCycleのデータが存在しません。CylinderCycleの設定を確認してください。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning));
            }

            // Cylinderデバイスの保存
            progressViewModel?.UpdateStatus($"シリンダーデバイスを保存中... ({cylinders.Count}件)");
            await _mnemonicService!.SaveMnemonicDeviceCY(cylinders, CylinderDeviceStartM, SelectedPlc.Id);

            // Timerデータの取得
            progressViewModel?.UpdateStatus("タイマーデータを取得中...");
            var allTimers = await _repository.GetTimersAsync();
            var cycles = (await _repository.GetCyclesAsync()).Where(c => c.PlcId == SelectedPlc.Id).ToList();
            var timers = new List<Timer>();
            foreach (var cycle in cycles)
            {
                timers.AddRange(allTimers.Where(t => t.CycleId == cycle.Id));
            }

            // Timerデバイスの保存（Cylinderに関連するもののみ）
            // Cycle処理で使用されたタイマー数の後から開始
            progressViewModel?.UpdateStatus($"シリンダーのタイマーを保存中... (開始カウント: {cycleTimerCount})");
            if (_timerService != null)
            {
                await _repository.DeleteAllMnemonicTimerDeviceAsync();
                await _timerService.SaveWithCY(timers, cylinders, DeviceStartT, SelectedPlc.Id, cycleTimerCount);
                progressViewModel?.AddLog($"シリンダータイマー保存完了 (開始位置: {cycleTimerCount})");
            }

            // Errorテーブルは各Cycleで保存済み（SaveCycleDevicesで処理）
            // ここでは削除しない

            // Speedテーブルの保存
            progressViewModel?.UpdateStatus("速度テーブルを保存中...");
            if (_speedService != null)
            {
                _speedService.DeleteSpeedTable();
                _speedService.Save(cylinders, CylinderDeviceStartD, SelectedPlc.Id);
            }

            progressViewModel?.AddLog("PLC全体のデバイス保存完了");
        }

        /// <summary>
        /// Cycleごとのデバイスを保存（Process, ProcessDetail, Operation）
        /// ※Cycleごとに複数回実行可能
        /// </summary>
        /// <param name="cycleProfile">Cycle用プロファイル</param>
        /// <param name="progressViewModel">進捗ViewModel</param>
        /// <param name="isFirstCycle">最初のCycleかどうか（ProsTimeテーブルの削除判定に使用）</param>
        /// <param name="currentTimerCount">現在のタイマーカウント（前のCycleから引き継ぐ）</param>
        /// <param name="currentErrorCount">現在のエラーカウント（前のCycleから引き継ぐ）</param>
        /// <returns>Tuple&lt;更新されたタイマーカウント, 更新されたエラーカウント&gt;</returns>
        private async Task<(int timerCount, int errorCount)> SaveCycleDevices(CycleMemoryProfile cycleProfile, MemoryProgressViewModel? progressViewModel = null, bool isFirstCycle = false, int currentTimerCount = 0, int currentErrorCount = 0)
        {
            if (SelectedPlc == null || _repository == null)
            {
                throw new InvalidOperationException("PLCまたはリポジトリが初期化されていません。");
            }

            // CycleIdに基づいてCycleオブジェクトを取得
            var cycles = await _repository.GetCyclesAsync();
            var targetCycle = cycles.FirstOrDefault(c => c.Id == cycleProfile.CycleId && c.PlcId == SelectedPlc.Id);

            if (targetCycle == null)
            {
                progressViewModel?.AddLog($"警告: CycleId={cycleProfile.CycleId} のCycleが見つかりません。スキップします。");
                return (currentTimerCount, currentErrorCount);
            }

            progressViewModel?.UpdateStatus($"Cycle '{targetCycle.CycleName}' のデータを取得中...");

            // 既存のProcess/ProcessDetail/Operationデバイスを削除（重複を防ぐため）
            // 注: 最初のCycleの場合のみ削除（2回目以降のCycleでは前のCycleのデータを保持）
            if (isFirstCycle)
            {
                progressViewModel?.UpdateStatus("既存の工程・工程詳細・操作デバイスを削除中...");
                await _mnemonicService!.DeleteMnemonicDevice(SelectedPlc.Id, (int)MnemonicType.Process);
                await _mnemonicService!.DeleteMnemonicDevice(SelectedPlc.Id, (int)MnemonicType.ProcessDetail);
                await _mnemonicService!.DeleteMnemonicDevice(SelectedPlc.Id, (int)MnemonicType.Operation);

                // Errorテーブルの初期化（最初のCycleの前に削除）
                progressViewModel?.UpdateStatus("エラーテーブルを初期化中...");
                if (_errorService != null)
                {
                    await _errorService.DeleteErrorTable();
                }
            }

            // ProcessDetailデータの取得
            var details = (await _repository.GetProcessDetailsAsync())
                .Where(d => d.CycleId == targetCycle.Id)
                .OrderBy(d => d.SortNumber)
                .ToList();

            // Operationデータの取得
            var operations = (await _repository.GetOperationsAsync())
                .Where(o => o.CycleId == targetCycle.Id)
                .OrderBy(o => o.SortNumber)
                .ToList();

            // Processデバイスの保存
            progressViewModel?.UpdateStatus($"工程デバイスを保存中... ({Processes.Count}件)");
            _mnemonicService!.SaveMnemonicDeviceProcess(Processes.ToList(), cycleProfile.ProcessDeviceStartL, SelectedPlc.Id);

            // ProcessDetailデバイスの保存
            progressViewModel?.UpdateStatus($"工程詳細デバイスを保存中... ({details.Count}件)");
            await _mnemonicService!.SaveMnemonicDeviceProcessDetail(details, cycleProfile.DetailDeviceStartL, SelectedPlc.Id);

            // Operationデバイスの保存
            progressViewModel?.UpdateStatus($"操作デバイスを保存中... ({operations.Count}件)");
            _mnemonicService!.SaveMnemonicDeviceOperation(operations, cycleProfile.OperationDeviceStartM, SelectedPlc.Id);

            // Timerデバイスの保存（ProcessDetailとOperationに関連するもの）
            int timerCount = currentTimerCount;
            if (_timerService != null)
            {
                progressViewModel?.UpdateStatus("タイマーデバイスを保存中...");
                var allTimers = await _repository.GetTimersAsync();
                var timers = allTimers.Where(t => t.CycleId == targetCycle.Id).ToList();

                timerCount += await _timerService.SaveWithDetail(timers, details, DeviceStartT, SelectedPlc.Id, timerCount);
                timerCount += await _timerService.SaveWithOperation(timers, operations, DeviceStartT, SelectedPlc.Id, timerCount);

                progressViewModel?.AddLog($"タイマーデバイス保存完了 (このCycle: {timerCount - currentTimerCount}件, 累計: {timerCount}件)");
            }

            // Errorテーブルの保存（Operationに関連するもの）
            int errorCount = currentErrorCount;
            if (_errorService != null)
            {
                progressViewModel?.UpdateStatus("エラーテーブルを保存中...");
                var ioList = await _repository.GetIoListAsync();
                errorCount = await _errorService.SaveMnemonicDeviceOperation(operations, ioList, ErrorDeviceStartM, ErrorDeviceStartT, SelectedPlc.Id, targetCycle.Id, errorCount);
                progressViewModel?.AddLog($"エラーテーブル保存完了 (このCycle: {errorCount - currentErrorCount}件, 累計: {errorCount}件)");
            }

            // ProsTimeテーブルの保存
            if (_prosTimeService != null)
            {
                progressViewModel?.UpdateStatus("工程時間テーブルを保存中...");
                // 最初のCycleの場合のみテーブルを削除
                if (isFirstCycle)
                {
                    _prosTimeService.DeleteProsTimeTable();
                }
                _prosTimeService.SaveProsTime(operations, ProsTimeStartZR, ProsTimePreviousStartZR, CyTimeStartZR, SelectedPlc.Id);
                progressViewModel?.AddLog("工程時間テーブル保存完了");
            }

            progressViewModel?.AddLog($"Cycle '{targetCycle.CycleName}' のデバイス保存完了");
            return (timerCount, errorCount);
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
