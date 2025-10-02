// ViewModel: PlcSelectionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
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
        protected private readonly IAccessRepository _repository = null!; // コンストラクタで初期化される
        protected private IMnemonicDeviceService? _mnemonicService;
        protected private IMnemonicTimerDeviceService? _timerService;
        protected private IProsTimeDeviceService? _prosTimeService;
        protected private IMnemonicSpeedDeviceService? _speedService;
        protected private IMemoryService? _memoryService;
        protected private IMnemonicDeviceMemoryStore _mnemonicMemoryStore = null!;
        protected private ErrorService? _errorService;
        protected private WpfIOSelectorService? _ioSelectorService;

        // 開いているProcessFlowDetailWindowのリスト
        private readonly List<Window> _openProcessFlowWindows = new();

        [ObservableProperty] private ObservableCollection<Company> _companies = new();
        [ObservableProperty] private ObservableCollection<Model> _models = new();
        [ObservableProperty] private ObservableCollection<PLC> _plcs = new();
        [ObservableProperty] private ObservableCollection<Cycle> _cycles = new();
        [ObservableProperty] private ObservableCollection<Process> _processes = new();
        [ObservableProperty] private ObservableCollection<ProcessDetail> _processDetails = new();
        [ObservableProperty] private ObservableCollection<Operation> _selectedOperations = new();

        [ObservableProperty] private ObservableCollection<ProcessCategory> _processCategories = new();
        [ObservableProperty] private ObservableCollection<ProcessDetailCategory> _processDetailCategories = new();
        [ObservableProperty] private ObservableCollection<OperationCategory> _operationCategories = new();

        [ObservableProperty] private Company? _selectedCompany;
        [ObservableProperty] private Model? _selectedModel;
        [ObservableProperty] private PLC? _selectedPlc;
        [ObservableProperty] private Cycle? _selectedCycle;
        [ObservableProperty] private Process? _selectedProcess;
        [ObservableProperty] private ProcessDetail? _selectedProcessDetail;

        [ObservableProperty] private int _processDeviceStartL = 14000;
        [ObservableProperty] private int _detailDeviceStartL = 15000;
        [ObservableProperty] private int _operationDeviceStartM = 20000;
        [ObservableProperty] private int _cylinderDeviceStartM = 30000;
        [ObservableProperty] private int _cylinderDeviceStartD = 5000;
        [ObservableProperty] private int _errorDeviceStartM = 120000;
        [ObservableProperty] private int _errorDeviceStartT = 2000;
        [ObservableProperty] private int _deviceStartT = 0;
        [ObservableProperty] private int _timerStartZR = 3000;
        [ObservableProperty] private int _prosTimeStartZR = 12000;
        [ObservableProperty] private int _prosTimePreviousStartZR = 24000;
        [ObservableProperty] private int _cyTimeStartZR = 30000;

        [ObservableProperty] private bool _isProcessMemory = false;
        [ObservableProperty] private bool _isDetailMemory = false;
        [ObservableProperty] private bool _isOperationMemory = false;
        [ObservableProperty] private bool _isCylinderMemory = false;
        [ObservableProperty] private bool _isErrorMemory = false;
        [ObservableProperty] private bool _isTimerMemory = false;
        [ObservableProperty] private bool _isProsTimeMemory = false;
        [ObservableProperty] private bool _isCyTimeMemory = false;

        [ObservableProperty] private bool _isProcessOutput = false;
        [ObservableProperty] private bool _isDetailOutput = false;
        [ObservableProperty] private bool _isOperationOutput = false;
        [ObservableProperty] private bool _isCylinderOutput = false;
        [ObservableProperty] private bool _isDebug = false;

        [ObservableProperty] private int _memoryProgressMax;
        [ObservableProperty] private int _memoryProgressValue;
        [ObservableProperty] private string _memoryStatusMessage = string.Empty;
        [ObservableProperty] private List<OutputError> _outputErrors = new();

        // メモリ設定状態の表示用プロパティ
        [ObservableProperty] private int _totalMemoryDeviceCount = 0;
        [ObservableProperty] private string _memoryConfigurationStatus = "未設定";
        [ObservableProperty] private bool _isMemoryConfigured = false;
        [ObservableProperty] private string _lastMemoryConfigTime = string.Empty;

        // 認証関連
        [ObservableProperty] private string _currentUserEmail = string.Empty;

        private List<ProcessDetail> _allDetails = new();
        private List<Process> _allProcesses = new();
        public List<Servo> _selectedServo = new(); // 選択されたサーボのリスト
        public List<CylinderCycle>? _selectedCylinderCycles = new(); // 選択されたシリンダーサイクルのリスト
        public List<ControlBox> _selectedControlBoxes = new(); // 選択されたコントロールボックスのリスト
        public List<CylinderControlBox> _selectedCylinderControlBoxes = new(); // 選択されたコントロールボックスのリスト


        // DIコンストラクタ（推奨）
        public MainViewModel(IAccessRepository repository, IAuthenticationService authService, SupabaseConnectionHelper? supabaseHelper = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _supabaseHelper = supabaseHelper;
            InitializeServices();
            LoadInitialData();

            // Supabase接続を非同期で初期化
            if (_supabaseHelper != null)
            {
                _ = InitializeSupabaseAsync();
            }
        }

        private readonly IAuthenticationService _authService = null!; // コンストラクタで初期化される
        private readonly SupabaseConnectionHelper? _supabaseHelper;

        // パラメータなしコンストラクタ（削除または例外をスロー）
        // XAML デザイナーやその他の場所でパラメータなしコンストラクタが必要な場合のみ
        public MainViewModel()
        {
            // DIコンテナからの取得を試みる
            var repository = App.Services?.GetService<IAccessRepository>();
            var authService = App.Services?.GetService<IAuthenticationService>();
            var supabaseHelper = App.Services?.GetService<SupabaseConnectionHelper>();

            if (repository != null && authService != null)
            {
                _repository = repository;
                _authService = authService;
                _supabaseHelper = supabaseHelper;
                InitializeServices();
                LoadInitialData();

                // Supabase接続を非同期で初期化
                if (_supabaseHelper != null)
                {
                    _ = InitializeSupabaseAsync();
                }
            }
            else
            {
                // デザイナーモードか、DIコンテナが設定されていない
                throw new InvalidOperationException("MainViewModelはDIコンテナから取得してください。App.Services.GetRequiredService<MainViewModel>()を使用してください。");
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

        private void InitializeServices()
        {
            // サービスの初期化
            _prosTimeService = App.Services?.GetService<IProsTimeDeviceService>()
                ?? new Kdx.Infrastructure.Services.ProsTimeDeviceService(_repository);
            _memoryService = App.Services?.GetService<IMemoryService>()
                ?? new Kdx.Infrastructure.Services.MemoryService(_repository);
            _ioSelectorService = new WpfIOSelectorService();
            _errorService = new ErrorService(_repository);

            // メモリストアを取得（App.xaml.csで登録済み）
            var memoryStore = App.Services?.GetService<IMnemonicDeviceMemoryStore>()
                ?? new MnemonicDeviceMemoryStore();
            _mnemonicMemoryStore = memoryStore;

            // ハイブリッドサービスを作成（メモリオンリーモード）
            var hybridService = new MnemonicDeviceHybridService(_repository, _memoryService, memoryStore);
            hybridService.SetMemoryOnlyMode(true); // データベースアクセスを無効化
            _mnemonicService = hybridService;

            // タイマーサービスもメモリストアを使用
            var timerAdapter = new MnemonicTimerDeviceMemoryAdapter(_repository, this, memoryStore, _memoryService);
            timerAdapter.SetMemoryOnlyMode(true); // データベースアクセスを無効化
            _timerService = timerAdapter;

            // スピードサービスもメモリストアを使用
            var speedAdapter = new MnemonicSpeedDeviceMemoryAdapter(_repository, memoryStore);
            speedAdapter.SetMemoryOnlyMode(true); // データベースアクセスを無効化
            _speedService = speedAdapter;
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

        // データの更新
        #region Properties for Selected Operations
        private void LoadInitialData()
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // 現在のユーザー情報を設定
            if (_authService?.CurrentSession != null)
            {
                CurrentUserEmail = _authService.CurrentSession.User?.Email ?? "Unknown User";
            }

            Companies = new ObservableCollection<Company>(_repository.GetCompanies());
            _allProcesses = _repository.GetProcesses();
            _allDetails = _repository.GetProcessDetails();

            // カテゴリデータの読み込み
            ProcessCategories = new ObservableCollection<ProcessCategory>(_repository.GetProcessCategories());
            ProcessDetailCategories = new ObservableCollection<ProcessDetailCategory>(_repository.GetProcessDetailCategories());
            OperationCategories = new ObservableCollection<OperationCategory>(_repository.GetOperationCategories());

            // 設定ファイルを読み込む
            SettingsManager.Load();

            // メモリプロファイルを読み込む
            LoadMemoryProfile();

            // 前回の選択を復元
            if (SettingsManager.Settings.LastSelectedCompanyId.HasValue)
            {
                var savedCompany = Companies.FirstOrDefault(c => c.Id == SettingsManager.Settings.LastSelectedCompanyId.Value);
                if (savedCompany != null)
                {
                    SelectedCompany = savedCompany;

                    // モデルも復元
                    if (SettingsManager.Settings.LastSelectedModelId.HasValue)
                    {
                        var savedModel = Models.FirstOrDefault(m => m.Id == SettingsManager.Settings.LastSelectedModelId.Value);
                        if (savedModel != null)
                        {
                            SelectedModel = savedModel;

                            // サイクルも復元
                            if (SettingsManager.Settings.LastSelectedCycleId.HasValue)
                            {
                                var savedCycle = Cycles.FirstOrDefault(c => c.Id == SettingsManager.Settings.LastSelectedCycleId.Value);
                                if (savedCycle != null)
                                {
                                    SelectedCycle = savedCycle;
                                }
                            }
                        }
                    }
                }
            }
        }

        partial void OnSelectedCompanyChanged(Company? value)
        {
            if (!CanExecute()) return;

            if (value == null) return;
            Models = new ObservableCollection<Model>(_repository!.GetModels().Where(m => m.CompanyId == value.Id));
            SelectedModel = null;

            // 選択した会社IDを保存
            SettingsManager.Settings.LastSelectedCompanyId = value.Id;
            SettingsManager.Save();
        }

        partial void OnSelectedModelChanged(Model? value)
        {
            if (!CanExecute()) return;

            if (value == null) return;
            Plcs = new ObservableCollection<PLC>(_repository!.GetPLCs().Where(p => p.ModelId == value.Id));
            SelectedPlc = null;

            // 選択したモデルIDを保存
            SettingsManager.Settings.LastSelectedModelId = value.Id;
            SettingsManager.Save();
        }

        partial void OnSelectedPlcChanged(PLC? value)
        {
            if (!CanExecute()) return;

            if (value == null) return;
            Cycles = new ObservableCollection<Cycle>(_repository!.GetCycles().Where(c => c.PlcId == value.Id));
            SelectedCycle = null;

            // PLCが変更されたらメモリ設定状態を更新
            UpdateMemoryConfigurationStatus();
        }

        partial void OnSelectedCycleChanged(Cycle? value)
        {
            if (!CanExecute()) return;

            if (value == null) return;

            // Processesをフィルタリング
            Processes = new ObservableCollection<Process>(
                _allProcesses.Where(p => p.CycleId == value.Id).OrderBy(p => p.SortNumber));

            // 選択されたCycleのOperationを読み込み
            var operations = _repository!.GetOperationsByCycleId(value.Id);
            SelectedOperations = new ObservableCollection<Operation>(operations.OrderBy(o => o.SortNumber));

            // 選択したサイクルIDを保存
            SettingsManager.Settings.LastSelectedCycleId = value.Id;
            SettingsManager.Save();

            // サイクルが変更されたらメモリ設定状態を更新
            UpdateMemoryConfigurationStatus();
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
            var filtered = _allDetails
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
        private void AddNewProcess()
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
                int newId = _repository!.AddProcess(newProcess);
                newProcess.Id = newId;

                // コレクションとローカルリストに追加
                Processes.Add(newProcess);
                _allProcesses.Add(newProcess);

                MessageBox.Show("新しい工程を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddNewProcessDetail()
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
                int newId = _repository!.AddProcessDetail(newDetail);
                newDetail.Id = newId;

                // コレクションとローカルリストに追加
                ProcessDetails.Add(newDetail);
                _allDetails.Add(newDetail);

                MessageBox.Show("新しい工程詳細を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程詳細の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddNewOperation()
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
                int newId = _repository!.AddOperation(newOperation);
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
        private void SaveAllChanges()
        {
            if (!CanExecute()) return;

            try
            {
                // Processの保存
                foreach (var process in Processes)
                {
                    _repository!.UpdateProcess(process);
                }

                // ProcessDetailの保存
                foreach (var detail in ProcessDetails)
                {
                    _repository!.UpdateProcessDetail(detail);
                }

                // Operationの保存
                foreach (var operation in SelectedOperations)
                {
                    _repository!.UpdateOperation(operation);
                }

                MessageBox.Show("すべての変更を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void DeleteSelectedProcess()
        {
            if (SelectedProcess == null) return;

            var result = MessageBox.Show($"工程 '{SelectedProcess.ProcessName}' を削除しますか？\n関連する工程詳細も削除されます。",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _repository!.DeleteProcess(SelectedProcess.Id);
                    Processes.Remove(SelectedProcess);
                    _allProcesses.Remove(SelectedProcess);

                    // 関連するProcessDetailも削除
                    var detailsToRemove = ProcessDetails.Where(d => d.ProcessId == SelectedProcess.Id).ToList();
                    foreach (var detail in detailsToRemove)
                    {
                        ProcessDetails.Remove(detail);
                        _allDetails.Remove(detail);
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
        private void DeleteSelectedProcessDetail()
        {
            if (SelectedProcessDetail == null) return;

            var result = MessageBox.Show($"工程詳細 '{SelectedProcessDetail.DetailName}' を削除しますか？",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _repository!.DeleteProcessDetail(SelectedProcessDetail.Id);
                    ProcessDetails.Remove(SelectedProcessDetail);
                    _allDetails.Remove(SelectedProcessDetail);
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
        private void DeleteSelectedOperation()
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
                    _repository!.DeleteOperation(operation.Id);
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
                _repository!.UpdateOperation(op);
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

            // 新しいウィンドウを作成
            var window = new ProcessFlowDetailWindow(_repository, SelectedCycle.Id, SelectedCycle.CycleName ?? $"サイクル{SelectedCycle.Id}");

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

            // インターロック設定ウィンドウを表示（シリンダー選択を含む）
            var window = new InterlockSettingsWindow();
            var viewModel = new InterlockSettingsViewModel(supabaseRepo, _repository!, SelectedPlc?.Id ?? 1, window);
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
        private void ProcessOutput()
        {
            var errorMessages = ValidateProcessOutput();

            if (_repository == null || SelectedPlc == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            try
            {
                MemoryStatusMessage = "処理を開始します...";
                OutputErrors.Clear();
                var allGeneratedErrors = new List<OutputError>();
                var allOutputRows = new List<LadderCsvRow>();

                // --- 1. データ準備 ---
                MemoryStatusMessage = "データ準備中...";
                var (data, prepErrors) = PrepareDataForOutput();
                allGeneratedErrors.AddRange(prepErrors);

                // --- 2. 各ビルダーによるラダー生成 ---
                MemoryStatusMessage = "ラダー生成中...";

                // ProcessBuilder (out パラメータ方式を維持、または新しい方式に修正)
                var processRows = ProcessBuilder.GenerateAllLadderCsvRows(
                    SelectedCycle!,
                    ProcessDeviceStartL,
                    DetailDeviceStartL,
                    data.JoinedProcessList,
                    data.JoinedProcessDetailList,
                    data.IoList,
                    _repository!,
                    out var processErrors);
                allOutputRows.AddRange(processRows);
                allGeneratedErrors.AddRange(processErrors);

                // ProcessDetailBuilder
                var pdErrorAggregator = new ErrorAggregator((int)MnemonicType.ProcessDetail);

                var pdIoAddressService = new IOAddressService(pdErrorAggregator, _repository, SelectedPlc.Id, _ioSelectorService);
                var detailBuilder = new ProcessDetailBuilder(this, pdErrorAggregator, pdIoAddressService, _repository);
                var detailRows = detailBuilder.GenerateAllLadderCsvRows(
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
                var cylinderRows = cylinderBuilder.GenerateLadder(
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
                    MemoryStatusMessage = "致命的なエラーのため、CSV出力を中止しました。";
                    MessageBox.Show(MemoryStatusMessage, "エラー");
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

                MemoryStatusMessage = errorMessage;
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
                MemoryStatusMessage = $"{categoryName} CSVファイル出力中...";
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

        private ((List<MnemonicDeviceWithProcess> JoinedProcessList,
                  List<MnemonicDeviceWithProcessDetail> JoinedProcessDetailList,
                  List<MnemonicTimerDeviceWithDetail> JoinedProcessDetailWithTimerList,
                  List<MnemonicDeviceWithOperation> JoinedOperationList,
                  List<MnemonicDeviceWithCylinder> JoinedCylinderList,
                  List<MnemonicTimerDeviceWithOperation> JoinedOperationWithTimerList,
                  List<MnemonicTimerDeviceWithCylinder> JoinedCylinderWithTimerList,
                  List<MnemonicSpeedDevice> SpeedDevice,
                  List<ProcessError> MnemonicErrors,
                  List<ProsTime> ProsTime,
                  List<IO> IoList) Data, List<OutputError> Errors) PrepareDataForOutput()
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
            var operations = _repository.GetOperations();

            // mainViewModelのフィールドを使用
            _selectedCylinderCycles = _repository.GetCylinderCyclesByPlcId(plcId);
            _selectedControlBoxes = _repository.GetControlBoxesByPlcId(plcId);
            _selectedCylinderControlBoxes = _repository.GetCylinderControlBoxesByPlcId(plcId);


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

            var cylindersForPlc = _repository.GetCYs().Where(c => c.PlcId == plcId).OrderBy(c => c.SortNumber);
            var cylinders = cylindersForPlc.Join(
                _selectedCylinderCycles,
                c => c.Id,
                cc => cc.CylinderId,
                (c, cc) => new { Cylinder = c, CylinderCycle = cc }
            ).Where(cc => cc.CylinderCycle.CycleId == cycleId).Select(cc => cc.Cylinder);

            var details = _repository.GetProcessDetails().Where(d => d.CycleId == cycleId).ToList();
            var ioList = _repository.GetIoList();
            _selectedServo = _repository.GetServos(null, null);

            var devicesP = devices.Where(m => m.MnemonicId == (int)MnemonicType.Process).ToList().OrderBy(p => p.StartNum);
            var devicesD = devices.Where(m => m.MnemonicId == (int)MnemonicType.ProcessDetail).ToList().OrderBy(d => d.StartNum);
            var devicesO = devices.Where(m => m.MnemonicId == (int)MnemonicType.Operation).ToList().OrderBy(o => o.StartNum);
            var devicesC = devices.Where(m => m.MnemonicId == (int)MnemonicType.CY).ToList().OrderBy(c => c.StartNum);

            var timerDevices = _timerService!.GetMnemonicTimerDevice(plcId, cycleId);
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
            var mnemonicErrors = _errorService!.GetErrors(plcId, cycleId, (int)MnemonicType.Operation);

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

            // 3. データ準備
            var prepData = PrepareDataForMemorySetting();

            // 4. Mnemonic/Timerテーブルへの事前保存
            if (prepData == null)
            {
                // データ準備に失敗した場合、ユーザーに通知して処理を中断
                MessageBox.Show("データ準備に失敗しました。CycleまたはPLCが選択されているか確認してください。", "エラー");
                return;
            }

            SaveMnemonicAndTimerDevices(prepData.Value);
            await SaveMemoriesToMemoryTableAsync(prepData.Value);
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
        private (
            List<ProcessDetail> details,
            List<Cylinder> cylinders,
            List<Operation> operations,
            List<IO> ioList,
            List<Timer> timers)? PrepareDataForMemorySetting()
        {
            if (SelectedCycle == null || _repository == null || SelectedPlc == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return null;
            }

            List<ProcessDetail> details = _repository
                .GetProcessDetails()
                .Where(d => d.CycleId == SelectedCycle.Id).OrderBy(d => d.SortNumber).ToList();
            List<Cylinder> cylinders = _repository.GetCYs()
                .Where(o => o.PlcId == SelectedPlc.Id).OrderBy(c => c.SortNumber).ToList();
            _selectedCylinderCycles = _repository.GetCylinderCyclesByPlcId(SelectedPlc.Id);

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
            List<Operation> operations = _repository.GetOperations().ToList();

            var op = operations
                .Where(o => o.CycleId == SelectedCycle.Id)
                .OrderBy(o => o.SortNumber).ToList();
            var ioList = _repository.GetIoList();
            var timers = _repository.GetTimersByCycleId(SelectedCycle.Id);

            return (details, filteredCylinders, op, ioList, timers);
        }

        // Mnemonic* と Timer* テーブルへのデータ保存をまとめたヘルパー
        private void SaveMnemonicAndTimerDevices(
            (List<ProcessDetail> details,
            List<Cylinder> cylinders,
            List<Operation> operations, List<IO> ioList, List<Timer> timers) prepData)
        {
            MemoryStatusMessage = "ニーモニックデバイス情報を保存中...";
            _mnemonicService!.DeleteAllMnemonicDevices();
            _mnemonicService!.SaveMnemonicDeviceProcess(Processes.ToList(), ProcessDeviceStartL, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceProcessDetail(prepData.details, DetailDeviceStartL, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceOperation(prepData.operations, OperationDeviceStartM, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceCY(prepData.cylinders, CylinderDeviceStartM, SelectedPlc!.Id);

            if (_repository == null || _timerService == null || _errorService == null || _prosTimeService == null || _speedService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }
            var timer = _repository.GetTimers();
            var details = _repository.GetProcessDetails();
            var operations = _repository.GetOperations();
            var cylinders = _repository.GetCYs();

            int timerCount = 0;

            // Timerテーブルの保存
            if (_timerService == null)
            {
                MessageBox.Show("TimerServiceが初期化されていません。", "エラー");
                return;
            }

            _repository.DeleteAllMnemonicTimerDevices();
            _timerService.SaveWithDetail(timer, details, DeviceStartT, SelectedPlc!.Id, ref timerCount);
            _timerService.SaveWithOperation(timer, operations, DeviceStartT, SelectedPlc!.Id, ref timerCount);
            _timerService.SaveWithCY(timer, cylinders, DeviceStartT, SelectedPlc!.Id, ref timerCount);

            // Errorテーブルの保存
            _errorService!.DeleteErrorTable();
            _errorService!.SaveMnemonicDeviceOperation(prepData.operations, prepData.ioList, ErrorDeviceStartM, ErrorDeviceStartT, SelectedPlc!.Id, SelectedCycle!.Id);

            // ProsTimeテーブルの保存
            _prosTimeService!.DeleteProsTimeTable();
            _prosTimeService!.SaveProsTime(prepData.operations, ProsTimeStartZR, ProsTimePreviousStartZR, CyTimeStartZR, SelectedPlc!.Id);

            // Speedテーブルの保存
            _speedService!.DeleteSpeedTable();
            _speedService!.Save(prepData.cylinders, CylinderDeviceStartD, SelectedPlc!.Id);
        }

        // Memoryテーブルへの保存処理
        private async Task SaveMemoriesToMemoryTableAsync(
            (List<ProcessDetail> details,
            List<Cylinder> cylinders,
            List<Operation> operations, List<IO> ioList, List<Timer> timers) prepData)
        {
            if (_memoryService == null)
            {
                MessageBox.Show("MemoryServiceが初期化されていません。", "エラー");
                return;
            }

            var devices = _mnemonicService!.GetMnemonicDevice(SelectedPlc!.Id);
            var timerDevices = _timerService!.GetMnemonicTimerDevice(SelectedPlc!.Id, SelectedCycle!.Id);

            var devicesP = devices.Where(m => m.MnemonicId == (int)MnemonicType.Process).ToList();
            var devicesD = devices.Where(m => m.MnemonicId == (int)MnemonicType.ProcessDetail).ToList();
            var devicesO = devices.Where(m => m.MnemonicId == (int)MnemonicType.Operation).ToList();
            var devicesC = devices.Where(m => m.MnemonicId == (int)MnemonicType.CY).ToList();

            MemoryProgressMax = (IsProcessMemory ? devicesP.Count : 0) +
                                (IsDetailMemory ? devicesD.Count : 0) +
                                (IsOperationMemory ? devicesO.Count : 0) +
                                (IsCylinderMemory ? devicesC.Count : 0) +
                                (IsErrorMemory ? devicesC.Count : 0) +
                                (IsTimerMemory ? timerDevices.Count * 2 : 0);
            MemoryProgressValue = 0;

            if (!await ProcessAndSaveMemoryAsync(IsErrorMemory, devicesC, device => _memoryService.SaveMnemonicMemories(_repository, device), "エラー")) return;
            //if (!await ProcessAndSaveMemoryAsync(true, timerDevices, _memoryService.SaveMnemonicTimerMemoriesT, "Timer (T)")) return;
            //if (!await ProcessAndSaveMemoryAsync(true, timerDevices, _memoryService.SaveMnemonicTimerMemoriesZR, "Timer (ZR)")) return;

            MemoryStatusMessage = "保存完了！";

            // メモリ設定状態を更新
            UpdateMemoryConfigurationStatus();

            MessageBox.Show("すべてのメモリ保存が完了しました。");
        }

        // Memory保存の繰り返し処理を共通化するヘルパー
        private async Task<bool> ProcessAndSaveMemoryAsync<T>(bool shouldProcess, IEnumerable<T> devices, Func<T, bool> saveAction, string categoryName)
        {
            if (!shouldProcess) return true;

            MessageBox.Show($"{categoryName}情報をMemoryテーブルにデータを保存します。", "確認");
            MemoryStatusMessage = $"{categoryName}情報を保存中...";

            foreach (var device in devices)
            {
                bool result = await Task.Run(() => saveAction(device));
                if (!result)
                {
                    MemoryStatusMessage = $"Memoryテーブル（{categoryName}）の保存に失敗しました。";
                    MessageBox.Show(MemoryStatusMessage, "エラー");
                    return false;
                }
                MemoryProgressValue++;
            }
            return true;
        }

        #endregion

        // Memory Status
        #region Memory Status Methods

        /// <summary>
        /// メモリ設定状態を更新する
        /// </summary>
        private void UpdateMemoryConfigurationStatus()
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
                var devices = _mnemonicService?.GetMnemonicDevice(SelectedPlc.Id) ?? new List<Kdx.Contracts.DTOs.MnemonicDevice>();
                var timerDevices = _timerService?.GetMnemonicTimerDevice(SelectedPlc.Id, SelectedCycle.Id) ?? new List<MnemonicTimerDevice>();
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
