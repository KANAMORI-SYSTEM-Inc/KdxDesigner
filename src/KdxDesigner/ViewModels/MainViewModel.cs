// ViewModel: PlcSelectionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Core.Application;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Services;
using KdxDesigner.Services.Authentication;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.Utils;
using KdxDesigner.Utils.Cylinder;
using KdxDesigner.Utils.Operation;
using KdxDesigner.Utils.ProcessDetail;
using KdxDesigner.ViewModels.Managers;
using KdxDesigner.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        /// <summary>
        /// DIコンテナ用コンストラクタ（推奨）
        /// </summary>
        public MainViewModel(ISupabaseRepository repository, IAuthenticationService authService, SupabaseConnectionHelper? supabaseHelper = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _supabaseHelper = supabaseHelper;

            Initialize();
        }

        /// <summary>
        /// パラメータなしコンストラクタ（デザイナー/レガシーサポート用）
        /// </summary>
        public MainViewModel()
        {
            var repository = App.Services?.GetService<ISupabaseRepository>();
            var authService = App.Services?.GetService<IAuthenticationService>();
            var supabaseHelper = App.Services?.GetService<SupabaseConnectionHelper>();

            if (repository == null || authService == null)
            {
                throw new InvalidOperationException(
                    "MainViewModelはDIコンテナから取得してください。" +
                    "App.Services.GetRequiredService<MainViewModel>()を使用してください。");
            }

            _repository = repository;
            _authService = authService;
            _supabaseHelper = supabaseHelper;

            Initialize();
        }

        /// <summary>
        /// 共通初期化処理
        /// </summary>
        private async void Initialize()
        {
            await InitializeManagers();
            _ = LoadInitialDataAsync();

            if (_supabaseHelper != null)
            {
                _ = InitializeSupabaseAsync();
            }
        }

        /// <summary>
        /// マネージャークラスの初期化
        /// </summary>
        private async Task InitializeManagers()
        {
            // SelectionStateManager の初期化
            _selectionManager = new SelectionStateManager(_repository);

            // SelectionStateManagerのプロパティ変更をMainViewModelに転送
            _selectionManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    OnPropertyChanged(e.PropertyName);

                    // 特定のプロパティ変更時に追加処理を実行
                    switch (e.PropertyName)
                    {
                        case nameof(SelectedCompany):
                            if (_selectionManager.SelectedCompany != null)
                            {
                                SettingsManager.Settings.LastSelectedCompanyId = _selectionManager.SelectedCompany.Id;
                                SettingsManager.Save();
                            }
                            break;
                        case nameof(SelectedModel):
                            if (_selectionManager.SelectedModel != null)
                            {
                                SettingsManager.Settings.LastSelectedModelId = _selectionManager.SelectedModel.Id;
                                SettingsManager.Save();
                            }
                            break;
                        case nameof(SelectedPlc):
                            UpdateMemoryConfigurationStatus();
                            break;
                        case nameof(SelectedCycle):
                            if (_selectionManager.SelectedCycle != null)
                            {
                                SettingsManager.Settings.LastSelectedCycleId = _selectionManager.SelectedCycle.Id;
                                SettingsManager.Save();
                            }
                            UpdateMemoryConfigurationStatus();
                            break;
                    }
                }
            };

            // ServiceInitializer の初期化とサービス生成
            _serviceInitializer = new ServiceInitializer(_repository, this);
            _serviceInitializer.InitializeAll();

            // DeviceConfigurationManager の初期化
            _deviceConfig = new DeviceConfigurationManager();

            // DeviceConfigurationManagerのプロパティ変更をMainViewModelに転送
            _deviceConfig.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    OnPropertyChanged(e.PropertyName);
                }
            };

            // MemoryConfigurationManager の初期化
            if (_serviceInitializer.MemoryStore != null)
            {
                _memoryConfig = new MemoryConfigurationManager(_serviceInitializer.MemoryStore);

                // MemoryConfigurationManagerのプロパティ変更をMainViewModelに転送
                _memoryConfig.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName != null)
                    {
                        OnPropertyChanged(e.PropertyName);
                    }
                };
            }
            else
            {
                throw new InvalidOperationException("MemoryStore が初期化されていません");
            }
        }

        private async Task InitializeSupabaseAsync()
        {
            try
            {
                if (_supabaseHelper == null) return;

                Debug.WriteLine("Starting Supabase initialization from MainViewModel...");
                var success = await _supabaseHelper.InitializeAsync();

                if (success)
                {
                    Debug.WriteLine("Supabase initialized successfully from MainViewModel");
                    // 接続テストを実行
                    var testResult = await _supabaseHelper.TestConnectionAsync();
                    Debug.WriteLine($"Supabase connection test result: {testResult}");
                }
                else
                {
                    Debug.WriteLine("Failed to initialize Supabase from MainViewModel");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Supabase from MainViewModel: {ex.Message}");
            }
        }


        private bool CanExecute()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 初期データの読み込み
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            if (!CanExecute() || _ioSelectorService == null)
            {
                Debug.WriteLine("システムの初期化が不完全なため、初期データの読み込みをスキップします。");
                return;
            }

            try
            {
                // 現在のユーザー情報を設定
                if (_authService?.CurrentSession != null)
                {
                    CurrentUserEmail = _authService.CurrentSession.User?.Email ?? "Unknown User";
                }

                // 初期データの読み込み
                await LoadMasterDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初期データの読み込み中にエラーが発生しました: {ex.Message}");
                MessageBox.Show($"データの読み込みに失敗しました: {ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// マスターデータの読み込み
        /// </summary>
        private async Task LoadMasterDataAsync()
        {
            // SelectionStateManagerを使ってマスターデータを読み込む
            await _selectionManager.LoadMasterDataAsync();

            // 設定とプロファイルの読み込み
            LoadSettings();

            // 前回の選択状態を復元
            RestoreLastSelection();
        }

        /// <summary>
        /// 設定とプロファイルの読み込み
        /// </summary>
        private void LoadSettings()
        {
            SettingsManager.Load();
            LoadMemoryProfile();
        }

        /// <summary>
        /// 前回の選択状態を復元
        /// </summary>
        private void RestoreLastSelection()
        {
            _selectionManager.RestoreSelection(
                SettingsManager.Settings.LastSelectedCompanyId,
                SettingsManager.Settings.LastSelectedModelId,
                SettingsManager.Settings.LastSelectedCycleId);
        }

        public void OnProcessDetailSelected(ProcessDetail selected)
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // ProcessDetailが選択されてもSelectedOperationsは変更しない
            // SelectedOperationsはCycleの全Operationを保持する
            if (selected?.OperationId != null)
            {
                // 必要に応じて選択されたOperationの詳細を別途処理
                // ただしSelectedOperationsコレクションは変更しない
            }
        }


        // その他ボタン処理
        #region Properties for Process Details

        private void LoadMemoryProfile()
        {
            var profileManager = new MemoryProfileManager();
            KdxDesigner.Models.MemoryProfile? profileToLoad = null;

            // 前回使用したプロファイルを取得
            if (!string.IsNullOrEmpty(SettingsManager.Settings.LastUsedMemoryProfileId))
            {
                profileToLoad = profileManager.GetProfile(SettingsManager.Settings.LastUsedMemoryProfileId);
            }

            // 前回のプロファイルがない場合はデフォルトプロファイルを使用
            if (profileToLoad == null)
            {
                profileToLoad = profileManager.GetDefaultProfile();
            }

            // プロファイルを適用
            if (profileToLoad != null)
            {
                profileManager.ApplyProfileToViewModel(profileToLoad, this);
            }
        }

        public void SaveLastUsedProfile(string profileId)
        {
            SettingsManager.Settings.LastUsedMemoryProfileId = profileId;
            SettingsManager.Save();
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

        #endregion

        // 出力処理
        #region ProcessOutput
        /// <summary>
        /// 出力ウィンドウを表示
        /// </summary>
        [RelayCommand]
        private void ProcessOutput()
        {
            var outputWindow = new OutputWindow(this)
            {
                Owner = Application.Current.MainWindow
            };
            outputWindow.ShowDialog();
        }
        #endregion


        // メモリ設定
        #region MemorySetting

        // MemorySettingに必要なデータを準備するヘルパー
        private async Task<(
            List<ProcessDetail> details,
            List<Cylinder> cylinders,
            List<Operation> operations,
            List<IO> ioList,
            List<Timer> timers)?> PrepareDataForMemorySetting()
        {
            if (SelectedCycle == null || _repository == null || SelectedPlc == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
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

        // Mnemonic* と Timer* テーブルへのデータ保存をまとめたヘルパー
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

            // prepDataから選択されたCycleに紐づくデータを使用
            var timer = prepData.timers;
            var details = prepData.details;
            var operations = prepData.operations;
            var cylinders = prepData.cylinders;

            int timerCount = 0;

            // Timerテーブルの保存
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

        // Memoryテーブルへの保存処理
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
            //if (!await ProcessAndSaveMemoryAsync(true, timerDevices, _memoryService.SaveMnemonicTimerMemoriesT, "Timer (T)", progressViewModel)) return;
            //if (!await ProcessAndSaveMemoryAsync(true, timerDevices, _memoryService.SaveMnemonicTimerMemoriesZR, "Timer (ZR)", progressViewModel)) return;

            progressViewModel?.UpdateStatus("メモリ設定状態を更新中...");

            // メモリ設定状態を更新
            await UpdateMemoryConfigurationStatus();

            MessageBox.Show("すべてのメモリ保存が完了しました。");
        }

        // Memory保存の繰り返し処理を共通化するヘルパー
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

        #endregion

        // Memory Status
        #region Memory Status Methods

        /// <summary>
        /// メモリ設定状態を更新する
        /// </summary>
        private async Task UpdateMemoryConfigurationStatus()
        {
            if (SelectedPlc == null || SelectedCycle == null || _mnemonicService == null || _timerService == null || _speedService == null)
            {
                MemoryConfigurationStatus = "未設定";
                IsMemoryConfigured = false;
                TotalMemoryDeviceCount = 0;
                return;
            }

            try
            {
                // メモリに保存されたデバイスの数をカウント
                var devices = await _mnemonicService.GetMnemonicDevice(SelectedPlc.Id) ?? new List<MnemonicDevice>();
                var timerDevices = await _timerService.GetMnemonicTimerDevice(SelectedPlc.Id, SelectedCycle.Id) ?? new List<MnemonicTimerDevice>();
                var speedDevices = _speedService.GetMnemonicSpeedDevice(SelectedPlc.Id) ?? new List<MnemonicSpeedDevice>();

                int totalCount = devices.Count + timerDevices.Count + speedDevices.Count;
                TotalMemoryDeviceCount = totalCount;

                if (totalCount > 0)
                {
                    IsMemoryConfigured = true;
                    LastMemoryConfigTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                    // 設定状態の詳細を作成
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

                    MemoryConfigurationStatus = statusBuilder.ToString().TrimEnd();
                }
                else
                {
                    MemoryConfigurationStatus = "未設定";
                    IsMemoryConfigured = false;
                }
            }
            catch (Exception ex)
            {
                MemoryConfigurationStatus = $"エラー: {ex.Message}";
                IsMemoryConfigured = false;
                TotalMemoryDeviceCount = 0;
            }
        }

        #endregion
    }
}
