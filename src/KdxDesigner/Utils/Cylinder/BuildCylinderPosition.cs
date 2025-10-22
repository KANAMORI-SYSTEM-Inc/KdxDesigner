using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;
using MnemonicSpeedDevice = Kdx.Contracts.DTOs.MnemonicSpeedDevice;
namespace KdxDesigner.Utils.Cylinder
{
    class BuildCylinderPosition
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public BuildCylinderPosition(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        public List<LadderCsvRow> Servo(
                MnemonicDeviceWithCylinder cylinder,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<MnemonicTimerDeviceWithCylinder> cylinderTimers,
                List<MnemonicSpeedDevice> speed,
                List<ProcessError> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            string prefix = $"{Label.PREFIX}{cylinder.Cylinder.CYNum}";
            string _bJogGo = $"{prefix}.Jog.bGo";
            string _bJogBack = $"{prefix}.Jog.bBack";
            string _wJogGoSpeed = $"{prefix}.Jog.wGoSpeed";
            string _wJogBackSpeed = $"{prefix}.Jog.wBackSpeed";

            var result = new List<LadderCsvRow>();
            var servo = _mainViewModel._selectedServo.FirstOrDefault(s => s.CylinderId == cylinder.Cylinder.Id); // 選択されたサーボの取得
            if (servo == null)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}のサーボが見つかりません。",
                });
                return result; // サーボが見つからない場合は空のリストを返す
            }

            var cySpeedDevice = speed.Where(s => s.CylinderId == cylinder.Cylinder.Id).SingleOrDefault(); // スピードデバイスの取得
            string? speedDevice;
            if (cySpeedDevice == null)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}のスピードデバイスが見つかりません。",
                });
                speedDevice = null; // スピードデバイスが見つからない場合はnullを設定
            }
            else
            {
                speedDevice = cySpeedDevice.Device; // スピードデバイスの取得
            }

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

            var functions = new CylinderFunction(_mainViewModel, _errorAggregator, cylinder, _ioAddressService, manualButton, speedDevice);


            // CYNumを含むIOの取得
            var sensors = ioList.Where(i => i.IOName != null
                                            && cylinder.Cylinder.CYNum != null
                                            && i.IOName.Contains(cylinder.Cylinder.CYNum)).ToList();

            // 行間ステートメント  
            string id = cylinder.Cylinder.Id.ToString();
            string cyNum = cylinder.Cylinder.CYNum ?? ""; // シリンダー名の取得  
            string cyNumSub = cylinder.Cylinder.CYNameSub?.ToString() ?? ""; // シリンダー名の取得  
            string cyName = cyNum + cyNumSub; // シリンダー名の組み合わせ
            result.Add(LadderRow.AddStatement(id + ":" + cyName + " サーボ"));


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

            result.Add(LadderRow.AddNOP());

            // 行き方向自動
            result.Add(LadderRow.AddLD(label + (startNum + 0).ToString()));
            result.Add(LadderRow.AddOR(label + (startNum + 2).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 12).ToString()));

            // 帰り方向自動
            result.Add(LadderRow.AddLD(label + (startNum + 1).ToString()));
            result.Add(LadderRow.AddOR(label + (startNum + 3).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 13).ToString()));

            // 指令ON
            result.Add(LadderRow.AddLD(label + (startNum + 12).ToString()));
            result.Add(LadderRow.AddOR(label + (startNum + 13).ToString()));
            result.Add(LadderRow.AddLD(label + (startNum + 7).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddORB());
            result.Add(LadderRow.AddLD(label + (startNum + 8).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddORB());
            result.Add(LadderRow.AddOUT(label + (startNum + 10).ToString()));
            result.Add(LadderRow.AddNOP());

            // サーボ軸停止
            result.Add(LadderRow.AddLD(label + (startNum + 10).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddLDI(_bJogGo));
            result.Add(LadderRow.AddANI(_bJogBack));
            result.Add(LadderRow.AddORB());
            result.Add(LadderRow.AddAND(servo.Busy));
            result.Add(LadderRow.AddANI(servo.OriginalPosition));
            result.Add(LadderRow.AddOUT(label + (startNum + 40).ToString()));

            result.Add(LadderRow.AddLDI(label + (startNum + 40).ToString()));
            result.AddRange(LadderRow.AddMOVSet("K0", servo.Prefix + servo.AxisStop));

            result.Add(LadderRow.AddLD(label + (startNum + 40).ToString()));
            result.AddRange(LadderRow.AddMOVSet("K1", servo.Prefix + servo.AxisStop));

            result.Add(LadderRow.AddNOP());

            // サーボ作動時エラー発生
            result.Add(LadderRow.AddLD(servo.Prefix + servo.Status + ".D"));
            result.Add(LadderRow.AddAND(label + (startNum + 10).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 41).ToString()));

            // JOG正転OK
            result.Add(LadderRow.AddLD(_bJogGo));
            result.Add(LadderRow.AddAND(label + (startNum + 17).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddANI(label + (startNum + 41).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 42).ToString()));

            // JOG逆転OK
            result.Add(LadderRow.AddLD(_bJogBack));
            result.Add(LadderRow.AddAND(label + (startNum + 18).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddANI(label + (startNum + 41).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 43).ToString()));

            // 行きOK 35
            result.Add(LadderRow.AddLD(label + (startNum + 12).ToString()));
            result.Add(LadderRow.AddAND(label + (startNum + 15).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (startNum + 42).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 35).ToString()));

            // 帰りOK 36
            result.Add(LadderRow.AddLD(label + (startNum + 13).ToString()));
            result.Add(LadderRow.AddAND(label + (startNum + 16).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (startNum + 43).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 36).ToString()));

            // 指令OK 37
            result.Add(LadderRow.AddLD(label + (startNum + 35).ToString()));
            result.Add(LadderRow.AddOR(label + (startNum + 36).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 37).ToString()));

            //// JOGバッファメモリ指定正転
            //result.Add(LadderRow.AddLD(label + (startNum + 42).ToString()));
            //result.AddRange(LadderRow.AddMOVSet("K0", servo.Prefix + servo.PositioningStartNum));
            //result.AddRange(LadderRow.AddMOVSet("K1", servo.Prefix + servo.StartFowardJog));
            //result.AddRange(LadderRow.AddMOVSet("K0", servo.Prefix + servo.StartReverseJog));

            //// JOGバッファメモリ指定逆転
            //result.Add(LadderRow.AddLD(label + (startNum + 43).ToString()));
            //result.AddRange(LadderRow.AddMOVSet("K0", servo.Prefix + servo.PositioningStartNum));
            //result.AddRange(LadderRow.AddMOVSet("K0", servo.Prefix + servo.StartFowardJog));
            //result.AddRange(LadderRow.AddMOVSet("K1", servo.Prefix + servo.StartReverseJog));

            //// 位置決め始動
            //result.Add(LadderRow.AddLD(label + (startNum + 12).ToString()));
            //result.Add(LadderRow.AddAND(label + (startNum + 35).ToString()));
            //result.Add(LadderRow.AddLD(label + (startNum + 13).ToString()));
            //result.Add(LadderRow.AddAND(label + (startNum + 36).ToString()));
            //result.Add(LadderRow.AddORB());
            //result.AddRange(LadderRow.AddMOVSet(speedDevice!, servo.Prefix + servo.PositioningStartNum));
            //result.Add(LadderRow.AddOUT(servo.PositioningStart));

            //// JOG停止
            //result.Add(LadderRow.AddLDI(label + (startNum + 42).ToString()));
            //result.Add(LadderRow.AddANI(label + (startNum + 43).ToString()));
            //result.AddRange(LadderRow.AddMOVSet("K0", servo.Prefix + servo.StartFowardJog));
            //result.AddRange(LadderRow.AddMOVSet("K0", servo.Prefix + servo.StartReverseJog));

            return result;
        }
    }
}
