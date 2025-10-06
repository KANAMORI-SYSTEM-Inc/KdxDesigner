using CommunityToolkit.Mvvm.ComponentModel;
using KdxDesigner.Models;

namespace KdxDesigner.ViewModels.Managers
{
    /// <summary>
    /// デバイス設定を管理するマネージャークラス
    /// デバイスの開始番号やメモリ/出力の有効化フラグを管理
    /// </summary>
    public partial class DeviceConfigurationManager : ObservableObject
    {
        // デバイス開始番号（フィールド）
        private int _processDeviceStartL = 14000;
        private int _detailDeviceStartL = 15000;
        private int _operationDeviceStartM = 20000;
        private int _cylinderDeviceStartM = 30000;
        private int _cylinderDeviceStartD = 5000;
        private int _errorDeviceStartM = 120000;
        private int _errorDeviceStartT = 2000;
        private int _deviceStartT = 0;
        private int _timerStartZR = 3000;
        private int _prosTimeStartZR = 12000;
        private int _prosTimePreviousStartZR = 24000;
        private int _cyTimeStartZR = 30000;

        // メモリ設定フラグ
        private bool _isProcessMemory = false;
        private bool _isDetailMemory = false;
        private bool _isOperationMemory = false;
        private bool _isCylinderMemory = false;
        private bool _isErrorMemory = false;
        private bool _isTimerMemory = false;
        private bool _isProsTimeMemory = false;
        private bool _isCyTimeMemory = false;

        // 出力設定フラグ
        private bool _isProcessOutput = false;
        private bool _isDetailOutput = false;
        private bool _isOperationOutput = false;
        private bool _isCylinderOutput = false;
        private bool _isDebug = false;

        /// <summary>
        /// メモリプロファイルから設定を読み込む
        /// </summary>
        public void LoadFromProfile(MemoryProfile? profile)
        {
            if (profile == null) return;
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
        /// 現在の設定からメモリプロファイルを作成
        /// </summary>
        public MemoryProfile ToProfile(string profileName)
        {
            return new MemoryProfile
            {
                Name = profileName,
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
        }

        /// <summary>
        /// すべてのメモリフラグをリセット
        /// </summary>
        public void ResetMemoryFlags()
        {
            IsProcessMemory = false;
            IsDetailMemory = false;
            IsOperationMemory = false;
            IsCylinderMemory = false;
            IsErrorMemory = false;
            IsTimerMemory = false;
            IsProsTimeMemory = false;
            IsCyTimeMemory = false;
        }

        /// <summary>
        /// すべての出力フラグをリセット
        /// </summary>
        public void ResetOutputFlags()
        {
            IsProcessOutput = false;
            IsDetailOutput = false;
            IsOperationOutput = false;
            IsCylinderOutput = false;
            IsDebug = false;
        }

        // プロパティ名を正しく修正
        public int ProcessDeviceStartL
        {
            get => _processDeviceStartL;
            set => SetProperty(ref _processDeviceStartL, value);
        }

        public int DetailDeviceStartL
        {
            get => _detailDeviceStartL;
            set => SetProperty(ref _detailDeviceStartL, value);
        }

        public int OperationDeviceStartM
        {
            get => _operationDeviceStartM;
            set => SetProperty(ref _operationDeviceStartM, value);
        }

        public int CylinderDeviceStartM
        {
            get => _cylinderDeviceStartM;
            set => SetProperty(ref _cylinderDeviceStartM, value);
        }

        public int CylinderDeviceStartD
        {
            get => _cylinderDeviceStartD;
            set => SetProperty(ref _cylinderDeviceStartD, value);
        }

        public int ErrorDeviceStartM
        {
            get => _errorDeviceStartM;
            set => SetProperty(ref _errorDeviceStartM, value);
        }

        public int ErrorDeviceStartT
        {
            get => _errorDeviceStartT;
            set => SetProperty(ref _errorDeviceStartT, value);
        }

        public int DeviceStartT
        {
            get => _deviceStartT;
            set => SetProperty(ref _deviceStartT, value);
        }

        public int TimerStartZR
        {
            get => _timerStartZR;
            set => SetProperty(ref _timerStartZR, value);
        }

        public int ProsTimeStartZR
        {
            get => _prosTimeStartZR;
            set => SetProperty(ref _prosTimeStartZR, value);
        }

        public int ProsTimePreviousStartZR
        {
            get => _prosTimePreviousStartZR;
            set => SetProperty(ref _prosTimePreviousStartZR, value);
        }

        public int CyTimeStartZR
        {
            get => _cyTimeStartZR;
            set => SetProperty(ref _cyTimeStartZR, value);
        }

        // メモリ設定フラグプロパティ
        public bool IsProcessMemory
        {
            get => _isProcessMemory;
            set => SetProperty(ref _isProcessMemory, value);
        }

        public bool IsDetailMemory
        {
            get => _isDetailMemory;
            set => SetProperty(ref _isDetailMemory, value);
        }

        public bool IsOperationMemory
        {
            get => _isOperationMemory;
            set => SetProperty(ref _isOperationMemory, value);
        }

        public bool IsCylinderMemory
        {
            get => _isCylinderMemory;
            set => SetProperty(ref _isCylinderMemory, value);
        }

        public bool IsErrorMemory
        {
            get => _isErrorMemory;
            set => SetProperty(ref _isErrorMemory, value);
        }

        public bool IsTimerMemory
        {
            get => _isTimerMemory;
            set => SetProperty(ref _isTimerMemory, value);
        }

        public bool IsProsTimeMemory
        {
            get => _isProsTimeMemory;
            set => SetProperty(ref _isProsTimeMemory, value);
        }

        public bool IsCyTimeMemory
        {
            get => _isCyTimeMemory;
            set => SetProperty(ref _isCyTimeMemory, value);
        }

        // 出力設定フラグプロパティ
        public bool IsProcessOutput
        {
            get => _isProcessOutput;
            set => SetProperty(ref _isProcessOutput, value);
        }

        public bool IsDetailOutput
        {
            get => _isDetailOutput;
            set => SetProperty(ref _isDetailOutput, value);
        }

        public bool IsOperationOutput
        {
            get => _isOperationOutput;
            set => SetProperty(ref _isOperationOutput, value);
        }

        public bool IsCylinderOutput
        {
            get => _isCylinderOutput;
            set => SetProperty(ref _isCylinderOutput, value);
        }

        public bool IsDebug
        {
            get => _isDebug;
            set => SetProperty(ref _isDebug, value);
        }
    }
}
