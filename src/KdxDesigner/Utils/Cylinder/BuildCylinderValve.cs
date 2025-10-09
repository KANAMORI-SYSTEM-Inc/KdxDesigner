using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.Cylinder
{
    internal class BuildCylinderValve
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        private readonly ISupabaseRepository _repository;
        public BuildCylinderValve(
            MainViewModel mainViewModel,
            IErrorAggregator errorAggregator,
            IIOAddressService ioAddressService,
            ISupabaseRepository accessRepository)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
            _repository = accessRepository;
        }

        public async Task<List<LadderCsvRow>> Valve1(
                MnemonicDeviceWithCylinder cylinder,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<ProcessError> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装  
            var result = new List<LadderCsvRow>();
            // 手動ボタンデバイスの取得
            var manualNumber = _mainViewModel._selectedCylinderControlBoxes
                .Where(cb => cb.CylinderId == cylinder.Cylinder.Id)
                .FirstOrDefault()!.ManualNumber;

            var manualButton = _mainViewModel._selectedControlBoxes
                .Where(cb => cb.BoxNumber == manualNumber)
                .FirstOrDefault()!.ManualButton;

            if (manualButton == null || manualButton == string.Empty)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}の手動操作盤が見つかりません。",
                });
                manualButton = SettingsManager.Settings.AlwaysOFF;
            }

            var functions = new CylinderFunction(_mainViewModel, _errorAggregator, cylinder, _ioAddressService, manualButton, null);


            // CYNumを含むIOの取得
            var sensors = ioList.Where(i => i.IOName != null
                                            && cylinder.Cylinder.CYNum != null
                                            && i.IOName.Contains(cylinder.Cylinder.CYNum)).ToList();

            // 行間ステートメント  
            string id = cylinder.Cylinder.Id.ToString();
            string cyNum = cylinder.Cylinder.CYNum ?? ""; // シリンダー名の取得  
            string cyNumSub = cylinder.Cylinder.CYNameSub.ToString() ?? ""; // シリンダー名の取得  
            string cyName = cyNum + cyNumSub; // シリンダー名の組み合わせ  

            result.Add(LadderRow.AddStatement(id + ":" + cyName + " シングルバルブ"));

            var label = cylinder.Mnemonic.DeviceLabel; // ラベルの取得  
            var startNum = cylinder.Mnemonic.StartNum; // ラベルの取得  

            // CYが一致するOperationの取得  
            var cylinderOperations = operations.Where(o => o.Operation.CYId == cylinder.Cylinder.Id).ToList();
            var goOperation = cylinderOperations.Where(o => o.Operation.GoBack == "G").ToList();        // 行きのOperationを取得  
            var backOperation = cylinderOperations.Where(o => o.Operation.GoBack == "B").ToList();      // 帰りのOperationを取得  
            var activeOperation = cylinderOperations.Where(o => o.Operation.GoBack == "A").ToList();    // 作動のOperationを取得  
            var offOperation = cylinderOperations.Where(o => o.Operation.GoBack == "O").ToList();       // 励磁切のOperationを取得  

            // 行き方向自動指令  
            if (goOperation.Count != 0 && activeOperation.Count == 0)
            {
                result.AddRange(functions.GoOperation(goOperation));
                // 帰り方向自動指令
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(goOperation));
                result.AddRange(functions.BackManualOperation(backOperation));

            }
            // 行き方向自動指令がない場合は、行き方向手動指令を使用
            else if (goOperation.Count == 0 && activeOperation.Count != 0)
            {

                result.AddRange(functions.GoOperation(activeOperation));
                // 帰り方向自動指令
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(activeOperation));
                result.AddRange(functions.BackManualOperation(backOperation));

            }

            if (offOperation.Count != 0)
            {
                // 励磁切指令
                result.AddRange(functions.OffOperation(offOperation));
            }

            result.AddRange(functions.ManualReset());


            // Cycleスタート時の方向自動指令
            result.AddRange(functions.CyclePulse());

            // 保持出力
            if (cylinder.Cylinder.MachineNameId == null || cylinder.Cylinder.DriveSubId == null) return result;

            Machine? machine = await _repository.GetMachineByIdAsync(cylinder.Cylinder.MachineNameId!.Value, cylinder.Cylinder.DriveSubId!.Value);

            if (machine == null) return result;

            if (machine.UseValveRetention == false)
            {
                result.AddRange(functions.Excitation(sensors));

            }
            else
            {
                result.AddRange(functions.OutputRetention());
                result.AddRange(functions.Retention(sensors, _mainViewModel.SelectedCycle!.StartDevice));
            }

            // マニュアル
            result.AddRange(functions.ManualButton());
            result.Add(LadderRow.AddNOP());

            // 出力OK
            result.AddRange(functions.ILOK());
            result.Add(LadderRow.AddNOP());

            // 出力検索
            int? cyNameSub = int.TryParse(cylinder.Cylinder.CYNameSub, out int cyNameSubValue) ? cyNameSubValue : (int?)null;
            if (cyNameSub != null)
            {
                result.AddRange(functions.SingleValve(sensors, cyNameSub));
            }
            else
            {
                result.AddRange(functions.SingleValve(sensors, null));
            }

            return result;

        }

        public List<LadderCsvRow> Valve2(
                MnemonicDeviceWithCylinder cylinder,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<ProcessError> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装  
            var result = new List<LadderCsvRow>();
            // 手動ボタンデバイスの取得
            var manualNumber = _mainViewModel._selectedCylinderControlBoxes
                .Where(cb => cb.CylinderId == cylinder.Cylinder.Id)
                .FirstOrDefault()!.ManualNumber;

            var manualButton = _mainViewModel._selectedControlBoxes
                .Where(cb => cb.BoxNumber == manualNumber)
                .FirstOrDefault()!.ManualButton;

            if (manualButton == null || manualButton == string.Empty)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}の手動操作盤が見つかりません。",
                });
                manualButton = SettingsManager.Settings.AlwaysOFF;
            }

            var functions = new CylinderFunction(_mainViewModel, _errorAggregator, cylinder, _ioAddressService, manualButton, null);


            // CYNumを含むIOの取得
            var sensors = ioList.Where(i => i.IOName != null
                                            && cylinder.Cylinder.CYNum != null
                                            && i.IOName.Contains(cylinder.Cylinder.CYNum)).ToList();

            // 行間ステートメント  
            string id = cylinder.Cylinder.Id.ToString();
            string cyNum = cylinder.Cylinder.CYNum ?? "";                   // シリンダー名の取得  
            string cyNumSub = cylinder.Cylinder.CYNameSub.ToString() ?? ""; // シリンダー名の取得  
            string cyName = cyNum + cyNumSub;                               // シリンダー名の組み合わせ  

            result.Add(LadderRow.AddStatement(id + ":" + cyName + " ダブルバルブ"));
            var label = cylinder.Mnemonic.DeviceLabel; // ラベルの取得  
            var startNum = cylinder.Mnemonic.StartNum; // ラベルの取得  

            // CYが一致するOperationの取得  
            var cylinderOperations = operations.Where(o => o.Operation.CYId == cylinder.Cylinder.Id).ToList();
            var goOperation = cylinderOperations.Where(o => o.Operation.GoBack == "G").ToList();        // 行きのOperationを取得  
            var backOperation = cylinderOperations.Where(o => o.Operation.GoBack == "B").ToList();      // 帰りのOperationを取得  
            var activeOperation = cylinderOperations.Where(o => o.Operation.GoBack == "A").ToList();    // 作動のOperationを取得  
            var offOperation = cylinderOperations.Where(o => o.Operation.GoBack == "O").ToList();    // 励磁切のOperationを取得  


            // 行き方向自動指令  
            if (goOperation.Count != 0 && activeOperation.Count == 0)
            {
                result.AddRange(functions.GoOperation(goOperation));
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(goOperation));
                result.AddRange(functions.BackManualOperation(backOperation));
            }
            // 行き方向自動指令がない場合は、行き方向手動指令を使用
            else if (goOperation.Count == 0 && activeOperation.Count != 0)
            {
                result.AddRange(functions.GoOperation(activeOperation));
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(activeOperation));
                result.AddRange(functions.BackManualOperation(backOperation));
            }

            // 励磁切指令
            if (offOperation.Count != 0)
            {
                result.AddRange(functions.OffOperation(offOperation));
            }

            result.AddRange(functions.ManualReset());

            // Cycleスタート時の方向自動指令
            result.AddRange(functions.CyclePulse());

            // 保持出力
            result.AddRange(functions.OutputRetention());
            result.AddRange(functions.Retention(sensors, _mainViewModel.SelectedCycle!.StartDevice));

            // マニュアル
            result.AddRange(functions.ManualButton());
            result.Add(LadderRow.AddNOP());

            // 出力OK
            result.AddRange(functions.ILOK());
            result.Add(LadderRow.AddNOP());

            // 出力検索
            int? cyNameSub = int.TryParse(cylinder.Cylinder.CYNameSub, out int cyNameSubValue) ? cyNameSubValue : (int?)null;
            if (cyNameSub != null)
            {
                result.AddRange(functions.DoubleValve(sensors, cyNameSub));

            }
            else
            {
                result.AddRange(functions.DoubleValve(sensors, null));

            }

            return result;

        }

        public async Task<List<LadderCsvRow>> Motor(
                MnemonicDeviceWithCylinder cylinder,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<ProcessError> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装  
            var result = new List<LadderCsvRow>();
            // 手動ボタンデバイスの取得
            var manualNumber = _mainViewModel._selectedCylinderControlBoxes
                .Where(cb => cb.CylinderId == cylinder.Cylinder.Id)
                .FirstOrDefault()!.ManualNumber;

            var manualButton = _mainViewModel._selectedControlBoxes
                .Where(cb => cb.BoxNumber == manualNumber)
                .FirstOrDefault()!.ManualButton;

            if (manualButton == null || manualButton == string.Empty)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}の手動操作盤が見つかりません。",
                });
                manualButton = SettingsManager.Settings.AlwaysOFF;
            }

            var functions = new CylinderFunction(_mainViewModel, _errorAggregator, cylinder, _ioAddressService, manualButton, null);


            // CYNumを含むIOの取得
            var sensors = ioList.Where(i => i.IOName != null
                                            && cylinder.Cylinder.CYNum != null
                                            && i.IOName.Contains(cylinder.Cylinder.CYNum)).ToList();

            // 行間ステートメント  
            string id = cylinder.Cylinder.Id.ToString();
            string cyNum = cylinder.Cylinder.CYNum ?? ""; // シリンダー名の取得  
            string cyNumSub = cylinder.Cylinder.CYNameSub.ToString() ?? ""; // シリンダー名の取得  
            string cyName = cyNum + cyNumSub; // シリンダー名の組み合わせ  

            result.Add(LadderRow.AddStatement(id + ":" + cyName + "モーター"));

            var label = cylinder.Mnemonic.DeviceLabel; // ラベルの取得  
            var startNum = cylinder.Mnemonic.StartNum; // ラベルの取得  

            // CYが一致するOperationの取得  
            var cylinderOperations = operations.Where(o => o.Operation.CYId == cylinder.Cylinder.Id).ToList();
            var goOperation = cylinderOperations.Where(o => o.Operation.GoBack == "G").ToList();        // 行きのOperationを取得  
            var backOperation = cylinderOperations.Where(o => o.Operation.GoBack == "B").ToList();      // 帰りのOperationを取得  
            var activeOperation = cylinderOperations.Where(o => o.Operation.GoBack == "A").ToList();    // 作動のOperationを取得  
            var offOperation = cylinderOperations.Where(o => o.Operation.GoBack == "O").ToList();    // 励磁切のOperationを取得  


            // 行き方向自動指令  
            if (goOperation.Count != 0 && activeOperation.Count == 0)
            {
                result.AddRange(functions.GoOperation(goOperation));
                // 帰り方向自動指令
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(goOperation));
                result.AddRange(functions.BackManualOperation(backOperation));

            }
            // 行き方向自動指令がない場合は、行き方向手動指令を使用
            else if (goOperation.Count == 0 && activeOperation.Count != 0)
            {

                result.AddRange(functions.GoOperation(activeOperation));
                // 帰り方向自動指令
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(activeOperation));
                result.AddRange(functions.BackManualOperation(backOperation));

            }

            if (offOperation.Count != 0)
            {
                // 励磁切指令
                result.AddRange(functions.OffOperation(offOperation));
            }


            result.AddRange(functions.ManualReset());


            // Cycleスタート時の方向自動指令
            result.AddRange(functions.CyclePulse());

            // 保持出力
            if (cylinder.Cylinder.MachineNameId == null || cylinder.Cylinder.DriveSubId == null) return result;

            Machine? machine = await _repository.GetMachineByIdAsync(cylinder.Cylinder.MachineNameId!.Value, cylinder.Cylinder.DriveSubId!.Value);

            if (machine == null) return result;

            if (machine.UseValveRetention == false)
            {
                result.AddRange(functions.Excitation(sensors));

            }
            else
            {
                result.AddRange(functions.OutputRetention());
                result.AddRange(functions.Retention(sensors, _mainViewModel.SelectedCycle!.StartDevice));
            }

            // マニュアル
            result.AddRange(functions.ManualButton());
            result.Add(LadderRow.AddNOP());

            // 出力OK
            result.AddRange(functions.ILOK());
            result.Add(LadderRow.AddNOP());

            // 出力検索
            int? cyNameSub = int.TryParse(cylinder.Cylinder.CYNameSub, out int cyNameSubValue) ? cyNameSubValue : (int?)null;
            if (cyNameSub != null)
            {
                result.AddRange(functions.Motor(sensors, cyNameSub));

            }
            else
            {
                result.AddRange(functions.Motor(sensors, null));

            }

            return result;

        }

    }
}
