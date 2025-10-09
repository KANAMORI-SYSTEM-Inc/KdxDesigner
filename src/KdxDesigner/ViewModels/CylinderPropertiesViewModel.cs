using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;

namespace KdxDesigner.ViewModels
{
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
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"マスターデータの読み込み中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        }
    }
}
