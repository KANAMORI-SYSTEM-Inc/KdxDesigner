// ViewModel: PlcSelectionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using Kdx.Core.Application;
using KdxDesigner.Models;
using KdxDesigner.Services;
using KdxDesigner.Services.IOSelector;
using KdxDesigner.Services.Authentication;
using KdxDesigner.Services.ErrorService;
using KdxDesigner.Services.MemonicTimerDevice;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.Services.MnemonicSpeedDevice;
using KdxDesigner.Utils;
using KdxDesigner.Utils.Cylinder;
using KdxDesigner.Utils.Operation;
using KdxDesigner.Utils.ProcessDetail;
using KdxDesigner.Views;
using KdxDesigner.ViewModels.Managers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Process = Kdx.Contracts.DTOs.Process;
using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // リポジトリと認証サービス
        protected private readonly ISupabaseRepository _repository = null!;
        private readonly IAuthenticationService _authService = null!;

        // Repositoryを公開（ViewからOperationPropertiesWindowにアクセスするため）
        public ISupabaseRepository? Repository => _repository;

        // Operationsを再読み込みするメソッド（Operation編集後に呼び出す）
        public async Task ReloadOperationsAsync()
        {
            if (SelectedCycle != null && _repository != null)
            {
                var operations = await _repository.GetOperationsAsync();
                var cycleOperations = operations
                    .Where(o => o.CycleId == SelectedCycle.Id)
                    .OrderBy(o => o.SortNumber)
                    .ToList();

                // DataGridの更新を確実にするため、ObservableCollectionを新しく作成
                _selectionManager.SelectedOperations = new ObservableCollection<Operation>(cycleOperations);

                // プロパティ変更通知を発火
                OnPropertyChanged(nameof(SelectedOperations));
            }
        }

        private readonly SupabaseConnectionHelper? _supabaseHelper;

        // マネージャークラス
        private SelectionStateManager _selectionManager = null!;
        private DeviceConfigurationManager _deviceConfig = null!;
        private MemoryConfigurationManager _memoryConfig = null!;
        private ServiceInitializer _serviceInitializer = null!;

        // 開いているProcessFlowDetailWindowのリスト
        private readonly List<Window> _openProcessFlowWindows = new();

        // その他の選択リスト
        public List<Servo> _selectedServo = new();
        public List<CylinderCycle>? _selectedCylinderCycles = new();
        public List<ControlBox> _selectedControlBoxes = new();
        public List<CylinderControlBox> _selectedCylinderControlBoxes = new();

        // 出力エラー
        [ObservableProperty] private List<OutputError> _outputErrors = new();

        // 認証関連
        [ObservableProperty] private string _currentUserEmail = string.Empty;

        // マネージャーからのプロパティ公開（バインディング用）
        public ObservableCollection<Company> Companies
        {
            get => _selectionManager?.Companies ?? new();
            set { if (_selectionManager != null) _selectionManager.Companies = value; }
        }
        public ObservableCollection<Model> Models
        {
            get => _selectionManager?.Models ?? new();
            set { if (_selectionManager != null) _selectionManager.Models = value; }
        }
        public ObservableCollection<PLC> Plcs
        {
            get => _selectionManager?.Plcs ?? new();
            set { if (_selectionManager != null) _selectionManager.Plcs = value; }
        }
        public ObservableCollection<Cycle> Cycles
        {
            get => _selectionManager?.Cycles ?? new();
            set { if (_selectionManager != null) _selectionManager.Cycles = value; }
        }
        public ObservableCollection<Process> Processes
        {
            get => _selectionManager?.Processes ?? new();
            set { if (_selectionManager != null) _selectionManager.Processes = value; }
        }
        public ObservableCollection<ProcessDetail> ProcessDetails
        {
            get => _selectionManager?.ProcessDetails ?? new();
            set { if (_selectionManager != null) _selectionManager.ProcessDetails = value; }
        }
        public ObservableCollection<Operation> SelectedOperations
        {
            get => _selectionManager?.SelectedOperations ?? new();
            set { if (_selectionManager != null) _selectionManager.SelectedOperations = value; }
        }

        public ObservableCollection<ProcessCategory> ProcessCategories
        {
            get => _selectionManager?.ProcessCategories ?? new();
            set { if (_selectionManager != null) _selectionManager.ProcessCategories = value; }
        }
        public ObservableCollection<ProcessDetailCategory> ProcessDetailCategories
        {
            get => _selectionManager?.ProcessDetailCategories ?? new();
            set { if (_selectionManager != null) _selectionManager.ProcessDetailCategories = value; }
        }
        public ObservableCollection<OperationCategory> OperationCategories
        {
            get => _selectionManager?.OperationCategories ?? new();
            set { if (_selectionManager != null) _selectionManager.OperationCategories = value; }
        }

        public Company? SelectedCompany
        {
            get => _selectionManager?.SelectedCompany;
            set { if (_selectionManager != null) _selectionManager.SelectedCompany = value; }
        }
        public Model? SelectedModel
        {
            get => _selectionManager?.SelectedModel;
            set { if (_selectionManager != null) _selectionManager.SelectedModel = value; }
        }
        public PLC? SelectedPlc
        {
            get => _selectionManager?.SelectedPlc;
            set { if (_selectionManager != null) _selectionManager.SelectedPlc = value; }
        }
        public Cycle? SelectedCycle
        {
            get => _selectionManager?.SelectedCycle;
            set { if (_selectionManager != null) _selectionManager.SelectedCycle = value; }
        }
        public Process? SelectedProcess
        {
            get => _selectionManager?.SelectedProcess;
            set { if (_selectionManager != null) _selectionManager.SelectedProcess = value; }
        }
        public ProcessDetail? SelectedProcessDetail
        {
            get => _selectionManager?.SelectedProcessDetail;
            set { if (_selectionManager != null) _selectionManager.SelectedProcessDetail = value; }
        }

        // デバイス設定プロパティ（DeviceConfigurationManagerから公開）
        public int ProcessDeviceStartL
        {
            get => _deviceConfig?.ProcessDeviceStartL ?? 14000;
            set { if (_deviceConfig != null) _deviceConfig.ProcessDeviceStartL = value; }
        }
        public int DetailDeviceStartL
        {
            get => _deviceConfig?.DetailDeviceStartL ?? 15000;
            set { if (_deviceConfig != null) _deviceConfig.DetailDeviceStartL = value; }
        }
        public int OperationDeviceStartM
        {
            get => _deviceConfig?.OperationDeviceStartM ?? 20000;
            set { if (_deviceConfig != null) _deviceConfig.OperationDeviceStartM = value; }
        }
        public int CylinderDeviceStartM
        {
            get => _deviceConfig?.CylinderDeviceStartM ?? 30000;
            set { if (_deviceConfig != null) _deviceConfig.CylinderDeviceStartM = value; }
        }
        public int CylinderDeviceStartD
        {
            get => _deviceConfig?.CylinderDeviceStartD ?? 5000;
            set { if (_deviceConfig != null) _deviceConfig.CylinderDeviceStartD = value; }
        }
        public int ErrorDeviceStartM
        {
            get => _deviceConfig?.ErrorDeviceStartM ?? 120000;
            set { if (_deviceConfig != null) _deviceConfig.ErrorDeviceStartM = value; }
        }
        public int ErrorDeviceStartT
        {
            get => _deviceConfig?.ErrorDeviceStartT ?? 2000;
            set { if (_deviceConfig != null) _deviceConfig.ErrorDeviceStartT = value; }
        }
        public int DeviceStartT
        {
            get => _deviceConfig?.DeviceStartT ?? 0;
            set { if (_deviceConfig != null) _deviceConfig.DeviceStartT = value; }
        }
        public int TimerStartZR
        {
            get => _deviceConfig?.TimerStartZR ?? 3000;
            set { if (_deviceConfig != null) _deviceConfig.TimerStartZR = value; }
        }
        public int ProsTimeStartZR
        {
            get => _deviceConfig?.ProsTimeStartZR ?? 12000;
            set { if (_deviceConfig != null) _deviceConfig.ProsTimeStartZR = value; }
        }
        public int ProsTimePreviousStartZR
        {
            get => _deviceConfig?.ProsTimePreviousStartZR ?? 24000;
            set { if (_deviceConfig != null) _deviceConfig.ProsTimePreviousStartZR = value; }
        }
        public int CyTimeStartZR
        {
            get => _deviceConfig?.CyTimeStartZR ?? 30000;
            set { if (_deviceConfig != null) _deviceConfig.CyTimeStartZR = value; }
        }

        // メモリ/出力フラグ
        public bool IsProcessMemory
        {
            get => _deviceConfig?.IsProcessMemory ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsProcessMemory = value; }
        }
        public bool IsDetailMemory
        {
            get => _deviceConfig?.IsDetailMemory ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsDetailMemory = value; }
        }
        public bool IsOperationMemory
        {
            get => _deviceConfig?.IsOperationMemory ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsOperationMemory = value; }
        }
        public bool IsCylinderMemory
        {
            get => _deviceConfig?.IsCylinderMemory ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsCylinderMemory = value; }
        }
        public bool IsErrorMemory
        {
            get => _deviceConfig?.IsErrorMemory ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsErrorMemory = value; }
        }
        public bool IsTimerMemory
        {
            get => _deviceConfig?.IsTimerMemory ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsTimerMemory = value; }
        }
        public bool IsProsTimeMemory
        {
            get => _deviceConfig?.IsProsTimeMemory ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsProsTimeMemory = value; }
        }
        public bool IsCyTimeMemory
        {
            get => _deviceConfig?.IsCyTimeMemory ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsCyTimeMemory = value; }
        }

        public bool IsProcessOutput
        {
            get => _deviceConfig?.IsProcessOutput ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsProcessOutput = value; }
        }
        public bool IsDetailOutput
        {
            get => _deviceConfig?.IsDetailOutput ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsDetailOutput = value; }
        }
        public bool IsOperationOutput
        {
            get => _deviceConfig?.IsOperationOutput ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsOperationOutput = value; }
        }
        public bool IsCylinderOutput
        {
            get => _deviceConfig?.IsCylinderOutput ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsCylinderOutput = value; }
        }
        public bool IsDebug
        {
            get => _deviceConfig?.IsDebug ?? false;
            set { if (_deviceConfig != null) _deviceConfig.IsDebug = value; }
        }

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

        // サービスへのアクセス（後方互換性のため）
        protected private IMnemonicDeviceService? _mnemonicService => _serviceInitializer?.MnemonicService;
        protected private IMnemonicTimerDeviceService? _timerService => _serviceInitializer?.TimerService;
        protected private IProsTimeDeviceService? _prosTimeService => _serviceInitializer?.ProsTimeService;
        protected private IMnemonicSpeedDeviceService? _speedService => _serviceInitializer?.SpeedService;
        protected private IMemoryService? _memoryService => _serviceInitializer?.MemoryService;
        protected private IMnemonicDeviceMemoryStore? _mnemonicMemoryStore => _serviceInitializer?.MemoryStore;
        protected private ErrorService? _errorService => _serviceInitializer?.ErrorService;
        protected private WpfIOSelectorService? _ioSelectorService => _serviceInitializer?.IOSelectorService;

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
        private void Initialize()
        {
            InitializeManagers();
            _ = LoadInitialDataAsync();

            if (_supabaseHelper != null)
            {
                _ = InitializeSupabaseAsync();
            }
        }

        /// <summary>
        /// マネージャークラスの初期化
        /// </summary>
        private void InitializeManagers()
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

        #region Selection Change Handlers

        /// <summary>
        /// 会社が選択されたときのハンドラ（互換性のため残す、実際の処理はSelectionStateManagerで実行）
        /// </summary>
        public async Task OnCompanyChangedAsync(Company? value)
        {
            if (!CanExecute() || value == null) return;
            SelectedCompany = value;
            await Task.CompletedTask;
        }

        /// <summary>
        /// モデルが選択されたときのハンドラ（互換性のため残す、実際の処理はSelectionStateManagerで実行）
        /// </summary>
        public async Task OnModelChangedAsync(Model? value)
        {
            if (!CanExecute() || value == null) return;
            SelectedModel = value;
            await Task.CompletedTask;
        }

        /// <summary>
        /// PLCが選択されたときのハンドラ（互換性のため残す、実際の処理はSelectionStateManagerで実行）
        /// </summary>
        public async Task OnPlcChangedAsync(PLC? value)
        {
            if (!CanExecute() || value == null) return;
            SelectedPlc = value;
            await Task.CompletedTask;
        }

        /// <summary>
        /// サイクルが選択されたときのハンドラ（互換性のため残す、実際の処理はSelectionStateManagerで実行）
        /// </summary>
        public async Task OnCycleChangedAsync(Cycle? value)
        {
            if (!CanExecute() || value == null) return;
            SelectedCycle = value;
            await Task.CompletedTask;
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

        #endregion

        // その他ボタン処理
        #region Properties for Process Details
        [RelayCommand]
        public void OpenControllBoxView()
        {
            if (SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var window = new KdxDesigner.Views.ControlBoxViews(_repository, SelectedPlc.Id)
            {
                Owner = Application.Current.MainWindow // 親ウィンドウ設定
            };
            window.ShowDialog();
        }

        [RelayCommand]
        public void UpdateSelectedProcesses(List<Process> selectedProcesses)
        {
            var selectedIds = selectedProcesses.Select(p => p.Id).ToHashSet();
            var filtered = _selectionManager.GetAllDetails()
                .Where(d => selectedIds.Contains(d.ProcessId))
                .ToList();

            ProcessDetails = new ObservableCollection<ProcessDetail>(filtered);
        }

        [RelayCommand]
        private void OpenIoEditor()
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // Viewにリポジトリのインスタンスを渡して生成
            var view = new IoEditorView(_repository, this);
            view.Show(); // モードレスダイアログとして表示
        }

        [RelayCommand]
        private void OpenIOConversionWindow()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            var window = new IOConversionWindow();
            var viewModel = new IOConversionViewModel(_repository);
            window.DataContext = viewModel;
            window.ShowDialog(); // モーダルダイアログとして表示
        }

        [RelayCommand]
        private async Task AddNewProcess()
        {
            if (!CanExecute() || SelectedCycle == null)
            {
                MessageBox.Show("サイクルを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 新しいProcessオブジェクトを作成
                var newProcess = new Process
                {
                    ProcessName = "新規工程",
                    CycleId = SelectedCycle.Id,
                    SortNumber = Processes.Count > 0 ? Processes.Max(p => p.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository!.AddProcessAsync(newProcess);
                newProcess.Id = newId;

                // コレクションとローカルリストに追加
                Processes.Add(newProcess);
                _selectionManager.AddProcessToCache(newProcess);

                MessageBox.Show("新しい工程を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task AddNewProcessDetail()
        {
            if (!CanExecute() || SelectedProcess == null)
            {
                MessageBox.Show("工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 新しいProcessDetailオブジェクトを作成
                var newDetail = new ProcessDetail
                {
                    ProcessId = SelectedProcess.Id,
                    DetailName = "新規詳細",
                    CycleId = SelectedCycle?.Id,
                    SortNumber = ProcessDetails.Count > 0 ? ProcessDetails.Max(d => d.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository!.AddProcessDetailAsync(newDetail);
                newDetail.Id = newId;

                // コレクションとローカルリストに追加
                ProcessDetails.Add(newDetail);
                _selectionManager.AddDetailToCache(newDetail);

                MessageBox.Show("新しい工程詳細を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程詳細の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 工程プロパティを編集
        /// </summary>
        [RelayCommand]
        private void EditProcess()
        {
            if (!CanExecute() || SelectedProcess == null)
            {
                MessageBox.Show("編集する工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ProcessPropertiesWindowを開く
                var window = new ProcessPropertiesWindow(_repository, SelectedProcess)
                {
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    // プロセスの更新をUIに反映
                    var index = Processes.IndexOf(SelectedProcess);
                    if (index >= 0)
                    {
                        Processes[index] = SelectedProcess;
                        OnPropertyChanged(nameof(Processes));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の編集中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task AddNewOperation()
        {
            if (!CanExecute() || SelectedCycle == null)
            {
                MessageBox.Show("サイクルを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 新しいOperationオブジェクトを作成
                var newOperation = new Operation
                {
                    OperationName = "新規操作",
                    CycleId = SelectedCycle.Id,
                    SortNumber = SelectedOperations.Count > 0 ? SelectedOperations.Max(o => o.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository!.AddOperationAsync(newOperation);
                newOperation.Id = newId;

                // コレクションに追加
                SelectedOperations.Add(newOperation);

                MessageBox.Show("新しい操作を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenCylinderManagement()
        {
            if (!CanExecute() || SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var window = new CylinderManagementWindow(_repository!, SelectedPlc.Id)
                {
                    Owner = Application.Current.MainWindow
                };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シリンダー管理ウィンドウを開く際にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SaveAllChanges()
        {
            if (!CanExecute()) return;

            try
            {
                // Processの保存
                foreach (var process in Processes)
                {
                    _repository!.UpdateProcessAsync(process);
                }

                // ProcessDetailの保存
                foreach (var detail in ProcessDetails)
                {
                    _repository!.UpdateProcessDetailAsync(detail);
                }

                // Operationの保存
                foreach (var operation in SelectedOperations)
                {
                    _repository!.UpdateOperationAsync(operation);
                }

                MessageBox.Show("すべての変更を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteSelectedProcess()
        {
            if (SelectedProcess == null) return;

            var result = MessageBox.Show($"工程 '{SelectedProcess.ProcessName}' を削除しますか？\n関連する工程詳細も削除されます。",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository!.DeleteProcessAsync(SelectedProcess.Id);
                    Processes.Remove(SelectedProcess);
                    _selectionManager.RemoveProcessFromCache(SelectedProcess);

                    // 関連するProcessDetailも削除
                    var detailsToRemove = ProcessDetails.Where(d => d.ProcessId == SelectedProcess.Id).ToList();
                    foreach (var detail in detailsToRemove)
                    {
                        ProcessDetails.Remove(detail);
                        _selectionManager.RemoveDetailFromCache(detail);
                    }

                    SelectedProcess = null;
                    MessageBox.Show("工程を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteSelectedProcessDetail()
        {
            if (SelectedProcessDetail == null) return;

            var result = MessageBox.Show($"工程詳細 '{SelectedProcessDetail.DetailName}' を削除しますか？",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository!.DeleteProcessDetailAsync(SelectedProcessDetail.Id);
                    ProcessDetails.Remove(SelectedProcessDetail);
                    _selectionManager.RemoveDetailFromCache(SelectedProcessDetail);
                    SelectedProcessDetail = null;
                    MessageBox.Show("工程詳細を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteSelectedOperation()
        {
            if (SelectedOperations.Count == 0) return;

            var operation = SelectedOperations.FirstOrDefault();
            if (operation == null) return;

            var result = MessageBox.Show($"操作 '{operation.OperationName}' を削除しますか？",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository!.DeleteOperationAsync(operation.Id);
                    SelectedOperations.Remove(operation);
                    MessageBox.Show("操作を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void SaveOperation()
        {
            if (!CanExecute()) return;

            foreach (var op in SelectedOperations)
            {
                _repository!.UpdateOperationAsync(op);
            }
            MessageBox.Show("保存しました。");
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var view = new SettingsView();
            view.ShowDialog();
        }

        [RelayCommand]
        private void OpenProcessFlowDetail()
        {
            if (SelectedCycle == null)
            {
                MessageBox.Show("サイクルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // MainViewModelで既に取得しているデータを渡してパフォーマンスを改善
            var allProcesses = _selectionManager.GetAllProcesses();
            var allProcessDetails = _selectionManager.GetAllDetails();
            var categories = ProcessDetailCategories.ToList();
            var plcId = SelectedPlc?.Id;

            // 新しいウィンドウを作成（既存データを渡す）
            var window = new ProcessFlowDetailWindow(
                _repository,
                SelectedCycle.Id,
                SelectedCycle.CycleName ?? $"サイクル{SelectedCycle.Id}",
                allProcesses,
                allProcessDetails,
                categories,
                plcId);

            // ウィンドウが閉じられたときにリストから削除
            window.Closed += (s, e) =>
            {
                if (s is Window w)
                {
                    _openProcessFlowWindows.Remove(w);
                }
            };

            // リストに追加して表示
            _openProcessFlowWindows.Add(window);
            window.Show();
        }

        [RelayCommand]
        private void CloseAllProcessFlowWindows()
        {
            // すべてのProcessFlowDetailWindowを閉じる
            var windowsToClose = _openProcessFlowWindows.ToList();
            foreach (var window in windowsToClose)
            {
                window.Close();
            }
            _openProcessFlowWindows.Clear();
        }

        [RelayCommand]
        private void OpenInterlockSettings()
        {
            // Supabaseリポジトリを取得
            Kdx.Infrastructure.Supabase.Repositories.SupabaseRepository? supabaseRepo = null;
            try
            {
                if (App.Services != null)
                {
                    var supabaseClient = App.Services.GetService<Supabase.Client>();
                    if (supabaseClient != null)
                    {
                        supabaseRepo = new Kdx.Infrastructure.Supabase.Repositories.SupabaseRepository(supabaseClient);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get SupabaseRepository: {ex.Message}");
                MessageBox.Show("Supabase接続が利用できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (supabaseRepo == null)
            {
                MessageBox.Show("Supabase接続が利用できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 選択されたサイクルを確認
            if (SelectedCycle == null)
            {
                MessageBox.Show("サイクルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // インターロック設定ウィンドウを表示（シリンダー選択を含む）
            var window = new InterlockSettingsWindow();
            var viewModel = new InterlockSettingsViewModel(supabaseRepo, _repository!, SelectedPlc.Id, SelectedCycle.Id, window);
            window.DataContext = viewModel;
            window.ShowDialog();
        }

        [RelayCommand]
        private void OpenMemoryEditor()
        {
            if (SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。");
                return;
            }

            var view = new MemoryEditorView(SelectedPlc.Id, _repository);
            view.ShowDialog();
        }

        [RelayCommand]
        private void OpenLinkDeviceManager()
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // Viewにリポジトリのインスタンスを渡す
            var view = new LinkDeviceView(_repository);
            view.ShowDialog(); // モーダルダイアログとして表示
        }

        [RelayCommand]
        private void OpenTimerEditor()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            var view = new TimerEditorView(_repository, this);
            view.ShowDialog();
        }

        [RelayCommand]
        private void OpenMemoryProfileManager()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }
            var view = new MemoryProfileView(this, _repository);
            view.ShowDialog();
        }

        private void LoadMemoryProfile()
        {
            var profileManager = new MemoryProfileManager();
            MemoryProfile? profileToLoad = null;

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
        [RelayCommand]
        private async Task ProcessOutput()
        {
            var errorMessages = ValidateProcessOutput();

            if (_repository == null || SelectedPlc == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            try
            {
                OutputErrors.Clear();
                var allGeneratedErrors = new List<OutputError>();
                var allOutputRows = new List<LadderCsvRow>();

                // --- 1. データ準備 ---
                var (data, prepErrors) = await PrepareDataForOutput();
                allGeneratedErrors.AddRange(prepErrors);

                // --- 2. 各ビルダーによるラダー生成 ---

                // ProcessBuilder (out パラメータ方式を維持、または新しい方式に修正)
                var pErrorAggregator = new ErrorAggregator((int)MnemonicType.Process);

                var processRows = await ProcessBuilder.GenerateAllLadderCsvRows(
                    SelectedCycle!,
                    ProcessDeviceStartL,
                    DetailDeviceStartL,
                    data.JoinedProcessList,
                    data.JoinedProcessDetailList,
                    data.IoList,
                    _repository!);
                allOutputRows.AddRange(processRows);
                allGeneratedErrors.AddRange(pErrorAggregator.GetAllErrors());

                // ProcessDetailBuilder
                var pdErrorAggregator = new ErrorAggregator((int)MnemonicType.ProcessDetail);

                var pdIoAddressService = new IOAddressService(pdErrorAggregator, _repository, SelectedPlc.Id, _ioSelectorService);
                var detailBuilder = new ProcessDetailBuilder(this, pdErrorAggregator, pdIoAddressService, _repository);
                var detailRows = await detailBuilder.GenerateAllLadderCsvRows(
                    data.JoinedProcessList,
                    data.JoinedProcessDetailList,
                    data.JoinedOperationList,
                    data.JoinedCylinderList,
                    data.IoList,
                    data.JoinedProcessDetailWithTimerList);
                allOutputRows.AddRange(detailRows);
                allGeneratedErrors.AddRange(pdErrorAggregator.GetAllErrors());

                // OperationBuilder
                var opErrorAggregator = new ErrorAggregator((int)MnemonicType.Operation);
                var opIoAddressService = new IOAddressService(opErrorAggregator, _repository, SelectedPlc.Id, _ioSelectorService);
                var operationBuilder = new OperationBuilder(this, opErrorAggregator, opIoAddressService);
                var operationRows = operationBuilder.GenerateLadder(
                    data.JoinedProcessDetailList,
                    data.JoinedOperationList,
                    data.JoinedCylinderList,
                    data.JoinedOperationWithTimerList,
                    data.SpeedDevice,
                    data.MnemonicErrors,
                    data.ProsTime, data.IoList);
                allOutputRows.AddRange(operationRows);
                allGeneratedErrors.AddRange(opErrorAggregator.GetAllErrors());

                // CylinderBuilder
                var cyErrorAggregator = new ErrorAggregator((int)MnemonicType.CY);
                var cyIoAddressService = new IOAddressService(cyErrorAggregator, _repository, SelectedPlc.Id, _ioSelectorService);
                var cylinderBuilder = new CylinderBuilder(this, cyErrorAggregator, cyIoAddressService, _repository);
                var cylinderRows = await cylinderBuilder.GenerateLadder(
                    data.JoinedProcessDetailList,
                    data.JoinedOperationList,
                    data.JoinedCylinderList,
                    data.JoinedOperationWithTimerList,
                    data.JoinedCylinderWithTimerList,
                    data.SpeedDevice,
                    data.MnemonicErrors,
                    data.ProsTime, data.IoList);
                allOutputRows.AddRange(cylinderRows);
                allGeneratedErrors.AddRange(cyErrorAggregator.GetAllErrors());

                // --- 3. 全てのエラーをUIに一度に反映 ---
                OutputErrors = allGeneratedErrors.Distinct().ToList(); // 重複するエラーを除く場合


                if (OutputErrors.Any())
                {
                    MessageBox.Show("ラダー生成中にエラーが検出されました。エラーリストを確認してください。", "生成エラー");
                }

                // --- 4. CSVエクスポート ---
                if (OutputErrors.Any(e => e.IsCritical))
                {
                    MessageBox.Show("致命的なエラーのため、CSV出力を中止しました。", "エラー");
                    return;
                }
                ExportLadderCsvFile(processRows, "Process.csv", "全ラダー");
                ExportLadderCsvFile(detailRows, "Detail.csv", "全ラダー");
                ExportLadderCsvFile(operationRows, "Operation.csv", "全ラダー");
                ExportLadderCsvFile(cylinderRows, "Cylinder.csv", "全ラダー");
                ExportLadderCsvFile(allOutputRows, "KdxLadder_All.csv", "全ラダー");
                MessageBox.Show("出力処理が完了しました。", "完了");
            }
            catch (Exception ex)
            {
                var errorMessage = $"出力処理中に致命的なエラーが発生しました: {ex.Message}";
                var stackMessage = $"出力処理中に致命的なエラーが発生しました: {ex.StackTrace}";

                MessageBox.Show(errorMessage, "エラー");
                MessageBox.Show(stackMessage, "エラー");

                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// CSVファイルのエクスポート処理を共通化するヘルパーメソッド
        /// </summary>
        public void ExportLadderCsvFile(List<LadderCsvRow> rows, string fileName, string categoryName)
        {
            if (!rows.Any()) return; // 出力する行がなければ何もしない

            try
            {
                string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                LadderCsvExporter.ExportLadderCsv(rows, csvPath);
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
            }
        }


        private List<string> ValidateProcessOutput()
        {
            var errors = new List<string>();
            if (SelectedCycle == null) errors.Add("Cycleが選択されていません。");
            if (SelectedPlc == null) errors.Add("PLCが選択されていません。");
            if (Processes.Count == 0) errors.Add("Processが選択されていません。");
            return errors;
        }

        private async Task<((List<MnemonicDeviceWithProcess> JoinedProcessList,
                  List<MnemonicDeviceWithProcessDetail> JoinedProcessDetailList,
                  List<MnemonicTimerDeviceWithDetail> JoinedProcessDetailWithTimerList,
                  List<MnemonicDeviceWithOperation> JoinedOperationList,
                  List<MnemonicDeviceWithCylinder> JoinedCylinderList,
                  List<MnemonicTimerDeviceWithOperation> JoinedOperationWithTimerList,
                  List<MnemonicTimerDeviceWithCylinder> JoinedCylinderWithTimerList,
                  List<MnemonicSpeedDevice> SpeedDevice,
                  List<ProcessError> MnemonicErrors,
                  List<ProsTime> ProsTime,
                  List<IO> IoList) Data, List<OutputError> Errors)> PrepareDataForOutput()
        {

            if (_repository == null || SelectedPlc == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return (
                    (new List<MnemonicDeviceWithProcess>(),
                     new List<MnemonicDeviceWithProcessDetail>(),
                     new List<MnemonicTimerDeviceWithDetail>(),
                     new List<MnemonicDeviceWithOperation>(),
                     new List<MnemonicDeviceWithCylinder>(),
                     new List<MnemonicTimerDeviceWithOperation>(),
                     new List<MnemonicTimerDeviceWithCylinder>(),
                     new List<MnemonicSpeedDevice>(),
                     new List<ProcessError>(),
                     new List<ProsTime>(),
                     new List<IO>()),
                    new List<OutputError>()
                );
            }

            var plcId = SelectedPlc!.Id;
            var cycleId = SelectedCycle!.Id;
            var devices = _mnemonicMemoryStore.GetMnemonicDevices(plcId);
            var operations = await _repository.GetOperationsAsync();

            // mainViewModelのフィールドを使用
            _selectedCylinderCycles = await _repository.GetCylinderCyclesByPlcIdAsync(plcId);
            _selectedControlBoxes = await _repository.GetControlBoxesByPlcIdAsync(plcId);
            _selectedCylinderControlBoxes = await _repository.GetCylinderControlBoxesByPlcIdAsync(plcId);


            if (_selectedCylinderCycles == null || _selectedCylinderCycles.Count == 0)
            {
                MessageBox.Show("CylinderCycleのデータが存在しません。CylinderCycleの設定を確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return (
                    (new List<MnemonicDeviceWithProcess>(),
                     new List<MnemonicDeviceWithProcessDetail>(),
                     new List<MnemonicTimerDeviceWithDetail>(),
                     new List<MnemonicDeviceWithOperation>(),
                     new List<MnemonicDeviceWithCylinder>(),
                     new List<MnemonicTimerDeviceWithOperation>(),
                     new List<MnemonicTimerDeviceWithCylinder>(),
                     new List<MnemonicSpeedDevice>(),
                     new List<ProcessError>(),
                     new List<ProsTime>(),
                     new List<IO>()),
                    new List<OutputError>()
                );
            }

            var cylindersForPlc = (await _repository.GetCYsAsync()).Where(c => c.PlcId == plcId).OrderBy(c => c.SortNumber);
            var cylinders = cylindersForPlc.Join(
                _selectedCylinderCycles,
                c => c.Id,
                cc => cc.CylinderId,
                (c, cc) => new { Cylinder = c, CylinderCycle = cc }
            ).Where(cc => cc.CylinderCycle.CycleId == cycleId).Select(cc => cc.Cylinder);

            var details = (await _repository.GetProcessDetailsAsync()).Where(d => d.CycleId == cycleId).ToList();
            var ioList = await _repository.GetIoListAsync();
            _selectedServo = await _repository.GetServosAsync(null, null);

            var devicesP = devices.Where(m => m.MnemonicId == (int)MnemonicType.Process).ToList().OrderBy(p => p.StartNum);
            var devicesD = devices.Where(m => m.MnemonicId == (int)MnemonicType.ProcessDetail).ToList().OrderBy(d => d.StartNum);
            var devicesO = devices.Where(m => m.MnemonicId == (int)MnemonicType.Operation).ToList().OrderBy(o => o.StartNum);
            var devicesC = devices.Where(m => m.MnemonicId == (int)MnemonicType.CY).ToList().OrderBy(c => c.StartNum);

            var timerDevices = await _timerService!.GetMnemonicTimerDevice(plcId, cycleId);
            var prosTime = _prosTimeService!.GetProsTimeByMnemonicId(plcId, (int)MnemonicType.Operation);

            var speedDeviceModels = _speedService!.GetMnemonicSpeedDevice(plcId);
            // KdxDesigner.Models から Kdx.Contracts.DTOs へマッピング
            var speedDevice = speedDeviceModels.Select(s => new Kdx.Contracts.DTOs.MnemonicSpeedDevice
            {
                ID = s.ID,
                CylinderId = s.CylinderId,
                Device = s.Device,
                PlcId = s.PlcId
            }).ToList();
            var mnemonicErrors = await _errorService!.GetErrors(plcId, cycleId, (int)MnemonicType.Operation);

            // JOIN処理
            var joinedProcessList = devicesP
                .Join(Processes, m => m.RecordId, p => p.Id, (m, p)
                => new MnemonicDeviceWithProcess { Mnemonic = m, Process = p }).ToList();
            var joinedProcessDetailList = devicesD
                .Join(details, m => m.RecordId, d => d.Id, (m, d)
                => new MnemonicDeviceWithProcessDetail { Mnemonic = m, Detail = d }).ToList();

            var timerDevicesDetail = timerDevices.Where(t => t.MnemonicId == (int)MnemonicType.ProcessDetail).ToList();

            var joinedProcessDetailWithTimerList = timerDevicesDetail.Join(
                details, m => m.RecordId, o => o.Id, (m, o) =>
                new MnemonicTimerDeviceWithDetail { Timer = m, Detail = o }).OrderBy(x => x.Detail.Id).ToList();

            var joinedOperationList = devicesO
                .Join(operations, m => m.RecordId, o => o.Id, (m, o)
                => new MnemonicDeviceWithOperation { Mnemonic = m, Operation = o })
                .OrderBy(x => x.Mnemonic.StartNum).ToList();
            var joinedCylinderList = devicesC
                .Join(cylinders, m => m.RecordId, c => c.Id, (m, c)
                => new MnemonicDeviceWithCylinder { Mnemonic = m, Cylinder = c })
                .OrderBy(x => x.Mnemonic.StartNum).ToList();

            var timerDevicesOperation = timerDevices.Where(t => t.MnemonicId == (int)MnemonicType.Operation).ToList();
            var joinedOperationWithTimerList = timerDevicesOperation
                .Join(operations, m => m.RecordId, o => o.Id, (m, o)
                => new MnemonicTimerDeviceWithOperation { Timer = m, Operation = o })
                .OrderBy(x => x.Operation.SortNumber).ToList();

            var timerDevicesCY = timerDevices.Where(t => t.MnemonicId == (int)MnemonicType.CY).ToList();
            var joinedCylinderWithTimerList = timerDevicesCY
                .Join(cylinders, m => m.RecordId, o => o.Id, (m, o)
                => new MnemonicTimerDeviceWithCylinder { Timer = m, Cylinder = o })
                .OrderBy(x => x.Cylinder.Id).ToList();

            var dataTuple = (
                joinedProcessList,
                joinedProcessDetailList,
                joinedProcessDetailWithTimerList,
                joinedOperationList,
                joinedCylinderList,
                joinedOperationWithTimerList,
                joinedCylinderWithTimerList,
                speedDevice,
                mnemonicErrors,
                prosTime,
                ioList);
            return (dataTuple, new List<OutputError>()); // 初期エラーリスト
        }

        #endregion


        // メモリ設定
        #region MemorySetting

        [RelayCommand]
        private async Task MemorySetting()
        {
            // メモリ設定状態を「設定中」に更新
            MemoryConfigurationStatus = "設定中...";
            IsMemoryConfigured = false;
            if (!ValidateMemorySettings()) return;

            // 進捗ウィンドウを作成
            var progressViewModel = new MemoryProgressViewModel();
            var progressWindow = new Views.MemoryProgressWindow
            {
                DataContext = progressViewModel,
                Owner = Application.Current.MainWindow
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

                    // 3. データ準備
                    progressViewModel.UpdateStatus("データを準備しています...");
                    var prepData = await PrepareDataForMemorySetting();

                    // 4. Mnemonic/Timerテーブルへの事前保存
                    if (prepData == null)
                    {
                        // データ準備に失敗した場合、ユーザーに通知して処理を中断
                        progressViewModel.MarkError("データ準備に失敗しました");
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show("データ準備に失敗しました。CycleまたはPLCが選択されているか確認してください。", "エラー"));
                        return;
                    }

                    await SaveMnemonicAndTimerDevices(prepData.Value, progressViewModel);
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
                filteredCylinders = cylinders.Join(
                    _selectedCylinderCycles,
                    c => c.Id,
                    cc => cc.CylinderId,
                    (c, cc) => new { Cylinder = c, CylinderCycle = cc }
                ).Where(cc => cc.CylinderCycle.CycleId == SelectedCycle.Id).Select(cc => cc.Cylinder).ToList();
            }

            var operationIds = details.Select(c => c.OperationId).ToHashSet();
            List<Operation> operations = (await _repository.GetOperationsAsync()).ToList();

            var op = operations
                .Where(o => o.CycleId == SelectedCycle.Id)
                .OrderBy(o => o.SortNumber).ToList();
            var ioList = await _repository.GetIoListAsync();
            var timers = await _repository.GetTimersByCycleIdAsync(SelectedCycle.Id);

            return (details, filteredCylinders, op, ioList, timers);
        }

        // Mnemonic* と Timer* テーブルへのデータ保存をまとめたヘルパー
        private async Task SaveMnemonicAndTimerDevices(
            (List<ProcessDetail> details,
            List<Cylinder> cylinders,
            List<Operation> operations, List<IO> ioList, List<Timer> timers) prepData,
            MemoryProgressViewModel? progressViewModel = null)
        {
            progressViewModel?.UpdateStatus("既存のニーモニックデバイスを削除中...");
            _mnemonicService!.DeleteAllMnemonicDevices();

            progressViewModel?.UpdateStatus($"工程デバイスを保存中... ({Processes.Count}件)");
            _mnemonicService!.SaveMnemonicDeviceProcess(Processes.ToList(), ProcessDeviceStartL, SelectedPlc!.Id);

            progressViewModel?.UpdateStatus($"工程詳細デバイスを保存中... ({prepData.details.Count}件)");
            _mnemonicService!.SaveMnemonicDeviceProcessDetail(prepData.details, DetailDeviceStartL, SelectedPlc!.Id);

            progressViewModel?.UpdateStatus($"操作デバイスを保存中... ({prepData.operations.Count}件)");
            _mnemonicService!.SaveMnemonicDeviceOperation(prepData.operations, OperationDeviceStartM, SelectedPlc!.Id);

            progressViewModel?.UpdateStatus($"シリンダーデバイスを保存中... ({prepData.cylinders.Count}件)");
            _mnemonicService!.SaveMnemonicDeviceCY(prepData.cylinders, CylinderDeviceStartM, SelectedPlc!.Id);

            if (_repository == null || _timerService == null || _errorService == null || _prosTimeService == null || _speedService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }
            var timer = await _repository.GetTimersAsync();
            var details = await _repository.GetProcessDetailsAsync();
            var operations = await _repository.GetOperationsAsync();
            var cylinders = await _repository.GetCYsAsync();

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
            UpdateMemoryConfigurationStatus();

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
            if (SelectedPlc == null || SelectedCycle == null)
            {
                MemoryConfigurationStatus = "未設定";
                IsMemoryConfigured = false;
                TotalMemoryDeviceCount = 0;
                return;
            }

            try
            {
                // メモリに保存されたデバイスの数をカウント
                var devices = await _mnemonicService?.GetMnemonicDevice(SelectedPlc.Id) ?? new List<Kdx.Contracts.DTOs.MnemonicDevice>();
                var timerDevices = await _timerService?.GetMnemonicTimerDevice(SelectedPlc.Id, SelectedCycle.Id) ?? new List<MnemonicTimerDevice>();
                var speedDevices = _speedService?.GetMnemonicSpeedDevice(SelectedPlc.Id) ?? new List<Kdx.Contracts.DTOs.MnemonicSpeedDevice>();

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

        // Authentication
        #region Authentication Commands

        [RelayCommand]
        private async Task SignOutAsync()
        {
            try
            {
                await _authService.SignOutAsync();

                // ログイン画面を表示
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var loginWindow = new Views.LoginView();
                    loginWindow.Show();

                    // メインウィンドウを閉じる
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is Views.MainView)
                        {
                            window.Close();
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"サインアウトに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

    }
}
