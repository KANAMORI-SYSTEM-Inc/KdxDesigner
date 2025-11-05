using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Core.Application;
using KdxDesigner.Utils;
using KdxDesigner.Utils.Cylinder;
using KdxDesigner.Utils.Operation;
using KdxDesigner.Utils.ProcessDetail;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// ラダープログラム出力ウィンドウのViewModel
    /// </summary>
    public partial class OutputViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        /// <summary>
        /// 出力エラーリスト
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<OutputError> _outputErrors = new();

        /// <summary>
        /// 処理中フラグ
        /// </summary>
        [ObservableProperty]
        private bool _isProcessing = false;

        /// <summary>
        /// ステータスメッセージ
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "準備完了";

        /// <summary>
        /// 進捗率 (0-100)
        /// </summary>
        [ObservableProperty]
        private int _progressPercentage = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="mainViewModel">MainViewModelへの参照</param>
        public OutputViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        /// <summary>
        /// 出力処理を実行
        /// </summary>
        [RelayCommand]
        private async Task ProcessOutput()
        {
            var errorMessages = ValidateProcessOutput();

            if (errorMessages.Any())
            {
                MessageBox.Show(
                    string.Join("\n", errorMessages),
                    "入力エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_mainViewModel.Repository == null || _mainViewModel.SelectedPlc == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            try
            {
                IsProcessing = true;
                ProgressPercentage = 0;
                OutputErrors.Clear();
                StatusMessage = "出力処理を開始しています...";

                var allGeneratedErrors = new List<OutputError>();
                var allOutputRows = new List<LadderCsvRow>();

                // --- 1. データ準備 ---
                StatusMessage = "データを準備中...";
                ProgressPercentage = 10;
                var (data, prepErrors) = await PrepareDataForOutput();
                allGeneratedErrors.AddRange(prepErrors);

                // --- 2. 各ビルダーによるラダー生成 ---

                // ProcessBuilder
                StatusMessage = "工程ラダーを生成中...";
                ProgressPercentage = 20;
                var pErrorAggregator = new ErrorAggregator((int)MnemonicType.Process);

                var processRows = await ProcessBuilder.GenerateAllLadderCsvRows(
                    _mainViewModel.SelectedCycle!,
                    _mainViewModel.ProcessDeviceStartL,
                    _mainViewModel.DetailDeviceStartL,
                    data.JoinedProcessList,
                    data.JoinedProcessDetailList,
                    data.IoList,
                    _mainViewModel.Repository!);
                allOutputRows.AddRange(processRows);
                allGeneratedErrors.AddRange(pErrorAggregator.GetAllErrors());

                // ProcessDetailBuilder
                StatusMessage = "工程詳細ラダーを生成中...";
                ProgressPercentage = 40;
                var pdErrorAggregator = new ErrorAggregator((int)MnemonicType.ProcessDetail);

                var pdIoAddressService = new IOAddressService(
                    pdErrorAggregator,
                    _mainViewModel.Repository,
                    _mainViewModel.SelectedPlc.Id,
                    _mainViewModel._ioSelectorService);
                var detailBuilder = new ProcessDetailBuilder(
                    _mainViewModel,
                    pdErrorAggregator,
                    pdIoAddressService,
                    _mainViewModel.Repository);
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
                StatusMessage = "操作ラダーを生成中...";
                ProgressPercentage = 60;
                var opErrorAggregator = new ErrorAggregator((int)MnemonicType.Operation);
                var opIoAddressService = new IOAddressService(
                    opErrorAggregator,
                    _mainViewModel.Repository,
                    _mainViewModel.SelectedPlc.Id,
                    _mainViewModel._ioSelectorService);
                var operationBuilder = new OperationBuilder(
                    _mainViewModel,
                    opErrorAggregator,
                    opIoAddressService);
                var operationRows = operationBuilder.GenerateLadder(
                    data.JoinedProcessDetailList,
                    data.JoinedOperationList,
                    data.JoinedCylinderList,
                    data.JoinedOperationWithTimerList,
                    data.SpeedDevice,
                    data.MnemonicErrors,
                    data.ProsTime,
                    data.IoList);
                allOutputRows.AddRange(operationRows);
                allGeneratedErrors.AddRange(opErrorAggregator.GetAllErrors());

                // CylinderBuilder
                StatusMessage = "シリンダーラダーを生成中...";
                ProgressPercentage = 80;
                var cyErrorAggregator = new ErrorAggregator((int)MnemonicType.CY);
                var cyIoAddressService = new IOAddressService(
                    cyErrorAggregator,
                    _mainViewModel.Repository,
                    _mainViewModel.SelectedPlc.Id,
                    _mainViewModel._ioSelectorService);
                var cylinderBuilder = new CylinderBuilder(
                    _mainViewModel,
                    cyErrorAggregator,
                    cyIoAddressService,
                    _mainViewModel.Repository);
                var cylinderRows = await cylinderBuilder.GenerateLadder(
                    data.JoinedProcessDetailList,
                    data.JoinedOperationList,
                    data.JoinedCylinderList,
                    data.JoinedOperationWithTimerList,
                    data.JoinedCylinderWithTimerList,
                    data.SpeedDevice,
                    data.MnemonicErrors,
                    data.ProsTime,
                    data.IoList);
                allOutputRows.AddRange(cylinderRows);
                allGeneratedErrors.AddRange(cyErrorAggregator.GetAllErrors());

                // --- 3. 全てのエラーをUIに反映 ---
                StatusMessage = "エラーチェック中...";
                ProgressPercentage = 90;
                foreach (var error in allGeneratedErrors.Distinct())
                {
                    OutputErrors.Add(error);
                }

                if (OutputErrors.Any())
                {
                    MessageBox.Show(
                        "ラダー生成中にエラーが検出されました。エラーリストを確認してください。",
                        "生成エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                // --- 4. CSVエクスポート ---
                if (OutputErrors.Any(e => e.IsCritical))
                {
                    StatusMessage = "致命的なエラーのため、処理を中止しました。";
                    ProgressPercentage = 0;
                    MessageBox.Show(
                        "致命的なエラーのため、CSV出力を中止しました。",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                StatusMessage = "CSVファイルを出力中...";
                ExportLadderCsvFile(processRows, "Process.csv");
                ExportLadderCsvFile(detailRows, "Detail.csv");
                ExportLadderCsvFile(operationRows, "Operation.csv");
                ExportLadderCsvFile(cylinderRows, "Cylinder.csv");
                ExportLadderCsvFile(allOutputRows, "KdxLadder_All.csv");

                ProgressPercentage = 100;
                StatusMessage = "出力処理が完了しました。";
                MessageBox.Show(
                    "出力処理が完了しました。",
                    "完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var errorMessage = $"出力処理中に致命的なエラーが発生しました: {ex.Message}";
                StatusMessage = "エラーが発生しました。";
                ProgressPercentage = 0;

                MessageBox.Show(errorMessage, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"{errorMessage}\n{ex.StackTrace}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// CSVファイルのエクスポート処理
        /// </summary>
        private void ExportLadderCsvFile(List<LadderCsvRow> rows, string fileName)
        {
            if (!rows.Any()) return;

            try
            {
                string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                LadderCsvExporter.ExportLadderCsv(rows, csvPath);
                Debug.WriteLine($"CSVファイルを出力しました: {csvPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CSVファイル出力エラー ({fileName}): {ex.Message}");
            }
        }

        /// <summary>
        /// 出力処理のバリデーション
        /// </summary>
        private List<string> ValidateProcessOutput()
        {
            var errors = new List<string>();
            if (_mainViewModel.SelectedCycle == null) errors.Add("Cycleが選択されていません。");
            if (_mainViewModel.SelectedPlc == null) errors.Add("PLCが選択されていません。");
            if (_mainViewModel.Processes.Count == 0) errors.Add("Processが選択されていません。");
            return errors;
        }

        /// <summary>
        /// 出力用データの準備
        /// </summary>
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
            if (_mainViewModel.Repository == null ||
                _mainViewModel.SelectedPlc == null ||
                _mainViewModel._ioSelectorService == null ||
                _mainViewModel._mnemonicMemoryStore == null ||
                _mainViewModel._timerService == null ||
                _mainViewModel._speedService == null ||
                _mainViewModel._errorService == null ||
                _mainViewModel._prosTimeService == null)
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

            var plcId = _mainViewModel.SelectedPlc!.Id;
            var cycleId = _mainViewModel.SelectedCycle!.Id;
            var devices = _mainViewModel._mnemonicMemoryStore.GetMnemonicDevices(plcId);
            var operations = await _mainViewModel.Repository.GetOperationsAsync();

            // CylinderCycleなどのデータを取得
            _mainViewModel._selectedCylinderCycles = await _mainViewModel.Repository.GetCylinderCyclesByPlcIdAsync(plcId);
            _mainViewModel._selectedControlBoxes = await _mainViewModel.Repository.GetControlBoxesByPlcIdAsync(plcId);
            _mainViewModel._selectedCylinderControlBoxes = await _mainViewModel.Repository.GetCylinderControlBoxesByPlcIdAsync(plcId);

            if (_mainViewModel._selectedCylinderCycles == null || _mainViewModel._selectedCylinderCycles.Count == 0)
            {
                MessageBox.Show(
                    "CylinderCycleのデータが存在しません。CylinderCycleの設定を確認してください。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

            var cylindersForPlc = (await _mainViewModel.Repository.GetCYsAsync())
                .Where(c => c.PlcId == plcId)
                .OrderBy(c => c.SortNumber);
            var cylinders = cylindersForPlc.Join(
                _mainViewModel._selectedCylinderCycles,
                c => c.Id,
                cc => cc.CylinderId,
                (c, cc) => new { Cylinder = c, CylinderCycle = cc }
            ).Where(cc => cc.CylinderCycle.CycleId == cycleId)
             .Select(cc => cc.Cylinder);

            var details = (await _mainViewModel.Repository.GetProcessDetailsAsync())
                .Where(d => d.CycleId == cycleId)
                .ToList();
            var ioList = await _mainViewModel.Repository.GetIoListAsync();
            _mainViewModel._selectedServo = await _mainViewModel.Repository.GetServosAsync(null, null);

            var devicesP = devices.Where(m => m.MnemonicId == (int)MnemonicType.Process).ToList().OrderBy(p => p.StartNum);
            var devicesD = devices.Where(m => m.MnemonicId == (int)MnemonicType.ProcessDetail).ToList().OrderBy(d => d.StartNum);
            var devicesO = devices.Where(m => m.MnemonicId == (int)MnemonicType.Operation).ToList().OrderBy(o => o.StartNum);
            var devicesC = devices.Where(m => m.MnemonicId == (int)MnemonicType.CY).ToList().OrderBy(c => c.StartNum);

            var timerDevices = await _mainViewModel._timerService!.GetMnemonicTimerDevice(plcId, cycleId);
            var prosTime = _mainViewModel._prosTimeService!.GetProsTimeByMnemonicId(plcId, (int)MnemonicType.Operation);

            var speedDeviceModels = _mainViewModel._speedService!.GetMnemonicSpeedDevice(plcId);
            var speedDevice = speedDeviceModels.Select(s => new MnemonicSpeedDevice
            {
                ID = s.ID,
                CylinderId = s.CylinderId,
                Device = s.Device,
                PlcId = s.PlcId
            }).ToList();
            var mnemonicErrors = await _mainViewModel._errorService!.GetErrors(plcId, cycleId, (int)MnemonicType.Operation);

            // JOIN処理
            var joinedProcessList = devicesP
                .Join(_mainViewModel.Processes, m => m.RecordId, p => p.Id, (m, p)
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
                .Join(cylindersForPlc, m => m.RecordId, c => c.Id, (m, c)
                => new MnemonicDeviceWithCylinder { Mnemonic = m, Cylinder = c })
                .OrderBy(x => x.Mnemonic.StartNum).ToList();

            var timerDevicesOperation = timerDevices.Where(t => t.MnemonicId == (int)MnemonicType.Operation).ToList();
            var joinedOperationWithTimerList = timerDevicesOperation
                .Join(operations, m => m.RecordId, o => o.Id, (m, o)
                => new MnemonicTimerDeviceWithOperation { Timer = m, Operation = o })
                .OrderBy(x => x.Operation.SortNumber).ToList();

            var timerDevicesCY = timerDevices.Where(t => t.MnemonicId == (int)MnemonicType.CY).ToList();
            var joinedCylinderWithTimerList = timerDevicesCY
                .Join(cylindersForPlc, m => m.RecordId, o => o.Id, (m, o)
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
            return (dataTuple, new List<OutputError>());
        }
    }
}
