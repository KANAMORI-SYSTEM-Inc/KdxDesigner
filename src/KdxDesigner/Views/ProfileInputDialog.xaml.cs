using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Views
{
    /// <summary>
    /// プロファイル入力ダイアログ
    /// プロファイル名と説明を入力するための汎用ダイアログ
    /// </summary>
    public partial class ProfileInputDialog : Window, INotifyPropertyChanged
    {
        private readonly ISupabaseRepository? _repository;
        private readonly int? _plcId;

        private string _dialogTitle = "プロファイル作成";
        private string _profileName = string.Empty;
        private int _profileCycleId = 0;
        private string _profileDescription = string.Empty;
        private ObservableCollection<Cycle> _availableCycles = new();
        private Cycle? _selectedCycle = null;
        private bool _showCycleSelector = false;
        private bool _showDeviceSettings = false;

        // デバイス設定値
        private int _processDeviceStartL = 14000;
        private int _detailDeviceStartL = 15000;
        private int _operationDeviceStartM = 20000;

        /// <summary>
        /// ダイアログのタイトル
        /// </summary>
        public string DialogTitle
        {
            get => _dialogTitle;
            set
            {
                _dialogTitle = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// プロファイル名
        /// </summary>
        public string ProfileName
        {
            get => _profileName;
            set
            {
                _profileName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// サイクルId（数値として取得）
        /// </summary>
        public int ProfileCycleIdValue
        {
            get => _selectedCycle?.Id ?? _profileCycleId;
        }

        /// <summary>
        /// サイクルId（TextBox用の文字列）
        /// </summary>
        public string ProfileCycleId
        {
            get => _profileCycleId.ToString();
            set
            {
                if (int.TryParse(value, out var cycleId))
                {
                    _profileCycleId = cycleId;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// プロファイルの説明
        /// </summary>
        public string ProfileDescription
        {
            get => _profileDescription;
            set
            {
                _profileDescription = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 利用可能なCycle一覧
        /// </summary>
        public ObservableCollection<Cycle> AvailableCycles
        {
            get => _availableCycles;
            set
            {
                _availableCycles = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 選択されたCycle
        /// </summary>
        public Cycle? SelectedCycle
        {
            get => _selectedCycle;
            set
            {
                _selectedCycle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProfileCycleIdValue));
            }
        }

        /// <summary>
        /// Cycleセレクターを表示するかどうか
        /// </summary>
        public bool ShowCycleSelector
        {
            get => _showCycleSelector;
            set
            {
                _showCycleSelector = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// デバイス設定を表示するかどうか
        /// </summary>
        public bool ShowDeviceSettings
        {
            get => _showDeviceSettings;
            set
            {
                _showDeviceSettings = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 工程デバイス開始番地 (L)
        /// </summary>
        public int ProcessDeviceStartL
        {
            get => _processDeviceStartL;
            set
            {
                _processDeviceStartL = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 工程詳細デバイス開始番地 (L)
        /// </summary>
        public int DetailDeviceStartL
        {
            get => _detailDeviceStartL;
            set
            {
                _detailDeviceStartL = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 操作デバイス開始番地 (M)
        /// </summary>
        public int OperationDeviceStartM
        {
            get => _operationDeviceStartM;
            set
            {
                _operationDeviceStartM = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// デフォルトコンストラクタ（PLC用プロファイルなど、Cycle選択が不要な場合に使用）
        /// </summary>
        public ProfileInputDialog()
        {
            InitializeComponent();
            DataContext = this;
            ShowCycleSelector = false;
        }

        /// <summary>
        /// Cycle選択対応コンストラクタ（Cycle用プロファイルで使用）
        /// </summary>
        /// <param name="repository">Supabaseリポジトリ</param>
        /// <param name="plcId">PLCのID</param>
        /// <param name="existingCycleId">既存のCycleID（編集時に使用、オプション）</param>
        /// <param name="processDeviceStartL">工程デバイス開始番地（オプション）</param>
        /// <param name="detailDeviceStartL">工程詳細デバイス開始番地（オプション）</param>
        /// <param name="operationDeviceStartM">操作デバイス開始番地（オプション）</param>
        public ProfileInputDialog(ISupabaseRepository repository, int plcId, int existingCycleId = 0,
            int processDeviceStartL = 14000, int detailDeviceStartL = 15000, int operationDeviceStartM = 20000) : this()
        {
            _repository = repository;
            _plcId = plcId;
            _profileCycleId = existingCycleId; // 既存のCycleIdを設定
            ShowCycleSelector = true;
            ShowDeviceSettings = true; // デバイス設定を表示

            // デバイス設定値の初期化
            ProcessDeviceStartL = processDeviceStartL;
            DetailDeviceStartL = detailDeviceStartL;
            OperationDeviceStartM = operationDeviceStartM;

            LoadCyclesAsync();
        }

        /// <summary>
        /// Cycle一覧を非同期で読み込み
        /// </summary>
        private async void LoadCyclesAsync()
        {
            if (_repository == null || _plcId == null)
            {
                return;
            }

            try
            {
                var allCycles = await _repository.GetCyclesAsync();
                var filteredCycles = allCycles.Where(c => c.PlcId == _plcId.Value).ToList();

                AvailableCycles = new ObservableCollection<Cycle>(filteredCycles);

                // 既存のCycleIdが設定されている場合は、そのCycleを選択
                if (_profileCycleId > 0)
                {
                    SelectedCycle = AvailableCycles.FirstOrDefault(c => c.Id == _profileCycleId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cycle一覧の取得に失敗しました: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // プロファイル名の検証
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                MessageBox.Show("プロファイル名を入力してください。", "入力エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProfileNameTextBox.Focus();
                return;
            }

            // Cycleセレクターが表示されている場合、Cycleが選択されているか確認
            if (ShowCycleSelector && SelectedCycle == null)
            {
                MessageBox.Show("サイクルを選択してください。", "入力エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// キャンセルボタンクリック時の処理
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
