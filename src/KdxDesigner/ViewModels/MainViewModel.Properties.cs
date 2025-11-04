using CommunityToolkit.Mvvm.ComponentModel;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Services;
using KdxDesigner.Services.Authentication;
using KdxDesigner.Services.ErrorService;
using KdxDesigner.Services.IOSelector;
using KdxDesigner.Services.MemonicTimerDevice;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.Services.MnemonicSpeedDevice;
using KdxDesigner.ViewModels.Managers;
using System.Collections.ObjectModel;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class MainViewModel
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
    }
}
