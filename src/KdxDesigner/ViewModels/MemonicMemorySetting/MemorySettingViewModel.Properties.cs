using CommunityToolkit.Mvvm.ComponentModel;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Models;
using KdxDesigner.Services;
using KdxDesigner.Services.ErrorService;
using KdxDesigner.Services.MemonicTimerDevice;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.Services.MnemonicSpeedDevice;
using KdxDesigner.Services.ProsTimeData;
using KdxDesigner.ViewModels.Managers;
using System.Collections.ObjectModel;

namespace KdxDesigner.ViewModels.Settings
{
    /// <summary>
    /// メモリ設定ウィンドウのViewModel
    /// プロファイル管理とメモリ設定実行機能を提供
    /// </summary>
    public partial class MemorySettingViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private readonly MainViewModel _mainViewModel;
        private readonly PlcMemoryProfileManager _plcProfileManager;
        private readonly CycleMemoryProfileManager _cycleProfileManager;

        // サービス
        private readonly IMnemonicDeviceService? _mnemonicService;
        private readonly IMnemonicTimerDeviceService? _timerService;
        private readonly ProsTimeDataBuilder? _prosTimeDataBuilder;
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

        // PLC用プロファイル関連
        [ObservableProperty]
        private ObservableCollection<PlcMemoryProfile> _plcProfiles = new();

        [ObservableProperty]
        private PlcMemoryProfile? _selectedPlcProfile;

        // Cycle用プロファイル関連
        [ObservableProperty]
        private ObservableCollection<CycleMemoryProfile> _cycleProfiles = new();

        /// <summary>
        /// 複数選択されたCycleプロファイル（ListBox用）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CycleMemoryProfile> _selectedCycleProfiles = new();

        // 旧プロファイル関連（後方互換性のため保持）
        [ObservableProperty]
        private ObservableCollection<Models.MemoryProfile> _profiles = new();

        [ObservableProperty]
        private Models.MemoryProfile? _selectedProfile;

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
    }
}
