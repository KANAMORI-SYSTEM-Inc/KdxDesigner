using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// Cycle選択用のラッパークラス
    /// </summary>
    public partial class CycleSelectionModel : ObservableObject
    {
        public Cycle Cycle { get; set; } = new();

        [ObservableProperty]
        private bool _isSelected;
    }

    /// <summary>
    /// シリンダープロパティウィンドウのViewModel
    /// </summary>
    public partial class CylinderPropertiesViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private Cylinder _cylinder;

        [ObservableProperty] private int _id;
        [ObservableProperty] private int _plcId;
        [ObservableProperty] private string? _puco;
        [ObservableProperty] private string _cyNum = "";
        [ObservableProperty] private string? _go;
        [ObservableProperty] private string? _back;
        [ObservableProperty] private string? _oilNum;
        [ObservableProperty] private int? _machineNameId;
        [ObservableProperty] private int? _driveSubId;
        [ObservableProperty] private int? _placeId;
        [ObservableProperty] private string? _cYNameSub;
        [ObservableProperty] private string? _sensorId;
        [ObservableProperty] private string? _flowType;
        [ObservableProperty] private string? _goSensorCount;
        [ObservableProperty] private string? _backSensorCount;
        [ObservableProperty] private string? _retentionSensorGo;
        [ObservableProperty] private string? _retentionSensorBack;
        [ObservableProperty] private int? _sortNumber;
        [ObservableProperty] private string? _flowCount;
        [ObservableProperty] private string? _flowCYGo;
        [ObservableProperty] private string? _flowCYBack;

        [ObservableProperty] private ObservableCollection<MachineName> _machineNames = new();
        [ObservableProperty] private ObservableCollection<DriveSub> _driveSubs = new();
        [ObservableProperty] private ObservableCollection<CycleSelectionModel> _availableCycles = new();

        public bool DialogResult { get; private set; }

        public CylinderPropertiesViewModel(ISupabaseRepository repository, Cylinder cylinder)
        {
            _repository = repository;
            _cylinder = cylinder;

            // マスターデータを読み込み
            LoadMasterData();

            // Cylinderのプロパティを読み込み
            LoadCylinderProperties();
        }

        /// <summary>
        /// マスターデータを読み込み
        /// </summary>
        private async void LoadMasterData()
        {
            try
            {
                var machineNames = await _repository.GetMachineNamesAsync();
                MachineNames = new ObservableCollection<MachineName>(machineNames);

                var driveSubs = await _repository.GetDriveSubsAsync();
                DriveSubs = new ObservableCollection<DriveSub>(driveSubs);

                // Cycleリストを読み込み
                await LoadCycles();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"マスターデータの読み込み中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cycleリストを読み込み、既存の関連付けに基づいて選択状態を設定
        /// </summary>
        private async Task LoadCycles()
        {
            try
            {
                // PlcIdに紐づくすべてのCycleを取得
                var allCycles = await _repository.GetCyclesAsync();
                var cyclesForPlc = allCycles.Where(c => c.PlcId == _cylinder.PlcId).ToList();

                // このシリンダーに関連付けられているCycleIdを取得
                var cylinderCycles = await _repository.GetCylinderCyclesByCylinderIdAsync(_cylinder.Id);
                var selectedCycleIds = cylinderCycles.Select(cc => cc.CycleId).ToHashSet();

                // CycleSelectionModelリストを作成
                var cycleSelections = cyclesForPlc.Select(c => new CycleSelectionModel
                {
                    Cycle = c,
                    IsSelected = selectedCycleIds.Contains(c.Id)
                }).ToList();

                AvailableCycles = new ObservableCollection<CycleSelectionModel>(cycleSelections);

                // IsSelectedプロパティの変更を監視
                foreach (var cycleSelection in AvailableCycles)
                {
                    cycleSelection.PropertyChanged += CycleSelection_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"サイクルの読み込み中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cycleの選択状態が変更されたときの処理
        /// </summary>
        private async void CycleSelection_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CycleSelectionModel.IsSelected) && sender is CycleSelectionModel cycleSelection)
            {
                try
                {
                    if (cycleSelection.IsSelected)
                    {
                        // 関連を追加
                        var cylinderCycle = new CylinderCycle
                        {
                            CylinderId = _cylinder.Id,
                            PlcId = _cylinder.PlcId,
                            CycleId = cycleSelection.Cycle.Id
                        };
                        await _repository.AddCylinderCycleAsync(cylinderCycle);
                    }
                    else
                    {
                        // 関連を削除
                        await _repository.DeleteCylinderCycleAsync(_cylinder.Id, cycleSelection.Cycle.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"サイクルの関連付け更新中にエラーが発生しました: {ex.Message}", "エラー",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    // エラーが発生した場合、選択状態を元に戻す
                    cycleSelection.IsSelected = !cycleSelection.IsSelected;
                }
            }
        }

        /// <summary>
        /// Cylinderのプロパティをロード
        /// </summary>
        private void LoadCylinderProperties()
        {
            Id = _cylinder.Id;
            PlcId = _cylinder.PlcId;
            Puco = _cylinder.PUCO;
            CyNum = _cylinder.CYNum;
            Go = _cylinder.Go;
            Back = _cylinder.Back;
            OilNum = _cylinder.OilNum;
            MachineNameId = _cylinder.MachineNameId;
            DriveSubId = _cylinder.DriveSubId;
            PlaceId = _cylinder.PlaceId;
            CYNameSub = _cylinder.CYNameSub;
            SensorId = _cylinder.SensorId;
            FlowType = _cylinder.FlowType;
            GoSensorCount = _cylinder.GoSensorCount;
            BackSensorCount = _cylinder.BackSensorCount;
            RetentionSensorGo = _cylinder.RetentionSensorGo;
            RetentionSensorBack = _cylinder.RetentionSensorBack;
            SortNumber = _cylinder.SortNumber;
            FlowCount = _cylinder.FlowCount;
            FlowCYGo = _cylinder.FlowCYGo;
            FlowCYBack = _cylinder.FlowCYBack;
        }

        /// <summary>
        /// 保存コマンド
        /// </summary>
        [RelayCommand]
        private async void Save()
        {
            // Cylinderのプロパティを更新
            _cylinder.PUCO = Puco;
            _cylinder.CYNum = CyNum;
            _cylinder.Go = Go;
            _cylinder.Back = Back;
            _cylinder.OilNum = OilNum;
            _cylinder.MachineNameId = MachineNameId;
            _cylinder.DriveSubId = DriveSubId;
            _cylinder.PlaceId = PlaceId;
            _cylinder.CYNameSub = CYNameSub;
            _cylinder.SensorId = SensorId;
            _cylinder.FlowType = FlowType;
            _cylinder.GoSensorCount = GoSensorCount;
            _cylinder.BackSensorCount = BackSensorCount;
            _cylinder.RetentionSensorGo = RetentionSensorGo;
            _cylinder.RetentionSensorBack = RetentionSensorBack;
            _cylinder.SortNumber = SortNumber;
            _cylinder.FlowCount = FlowCount;
            _cylinder.FlowCYGo = FlowCYGo;
            _cylinder.FlowCYBack = FlowCYBack;

            // データベースに保存
            await _repository.UpdateCylinderAsync(_cylinder);

            DialogResult = true;
            RequestClose?.Invoke();
        }

        /// <summary>
        /// キャンセルコマンド
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke();
        }

        public event Action? RequestClose;

        /// <summary>
        /// Cylinderオブジェクトを更新
        /// </summary>
        public void UpdateCylinder(Cylinder cylinder)
        {
            _cylinder = cylinder;
            LoadCylinderProperties();
        }

        /// <summary>
        /// イベントハンドラをクリア
        /// </summary>
        public void ClearEventHandlers()
        {
            RequestClose = null;

            // CycleSelectionのイベントハンドラもクリア
            foreach (var cycleSelection in AvailableCycles)
            {
                cycleSelection.PropertyChanged -= CycleSelection_PropertyChanged;
            }
        }
    }
}
