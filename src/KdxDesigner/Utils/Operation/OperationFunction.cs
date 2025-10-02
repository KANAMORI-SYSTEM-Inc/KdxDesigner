using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;
using MnemonicSpeedDevice = Kdx.Contracts.DTOs.MnemonicSpeedDevice;
namespace KdxDesigner.Utils.Operation
{
    internal class OperationFunction
    {
        private readonly MnemonicDeviceWithOperation _operation;
        private readonly List<MnemonicTimerDeviceWithOperation> _timers;
        private readonly List<MnemonicDeviceWithCylinder> _cylinders;
        private readonly List<IO> _ioList;
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        private readonly string _label;
        private readonly int _outNum;
        private readonly List<MnemonicDeviceWithProcessDetail> _detailList;

        public OperationFunction(
            MnemonicDeviceWithOperation operation,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            List<MnemonicDeviceWithProcessDetail> detailList,
            MainViewModel mainViewModel,
            IErrorAggregator errorAggregator,
            IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _timers = timers;
            _cylinders = cylinders;
            _ioList = ioList;
            _detailList = detailList;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
            _operation = operation;
            _label = operation.Mnemonic.DeviceLabel!; // ラベルの取得
            _outNum = operation.Mnemonic.StartNum; // ラベルの取得
        }

        public List<LadderCsvRow> GenerateM0()
        {
            var result = new List<LadderCsvRow>();
            if (_detailList.Count == 0)
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                result.Add(LadderRow.AddOUT(_label + (_outNum + 0).ToString()));
            }
            else
            {
                bool notFirst = false; // 最初の工程詳細かどうかを示すフラグ
                foreach (var detail in _detailList)
                {
                    var detailLabel = detail.Mnemonic.DeviceLabel; // 工程詳細のラベル取得
                    var detailOutNum = detail.Mnemonic.StartNum; // 工程詳細のラベル取得

                    result.Add(LadderRow.AddLD(detailLabel + (detailOutNum + 1).ToString()));
                    result.Add(LadderRow.AddANI(detailLabel + (detailOutNum + 4).ToString()));
                    if (notFirst) result.Add(LadderRow.AddORB());
                    notFirst = true;
                }
                result.Add(LadderRow.AddOUT(_label + (_outNum + 0).ToString()));
            }

            return result;
        }

        public List<LadderCsvRow> GenerateM2()
        {
            var result = new List<LadderCsvRow>();
            result.Add(LadderRow.AddLD(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddSET(_label + (_outNum + 2).ToString()));

            return result;
        }

        public List<LadderCsvRow> GenerateM5()
        {
            var result = new List<LadderCsvRow>();
            var thisTimer = _timers.Where(t => t.Timer.RecordId == _operation.Operation.Id).ToList();


            result.Add(LadderRow.AddLD(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddLD(_label + (_outNum + 2).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddORB());

            result.Add(LadderRow.AddOR(_label + (_outNum + 5).ToString()));
            result.Add(LadderRow.AddANI(_label + (_outNum + 19).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.SoftResetSignal)); // ソフトリセット信号を追加
            result.Add(LadderRow.AddANI(_label + (_outNum + 4).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 5).ToString()));


            // 関連するタイマがある場合
            if (thisTimer != null && thisTimer.Count > 0)
            {
                result.Add(LadderRow.AddLDP(_label + (_outNum + 5).ToString()));

                foreach (var timer in thisTimer)
                {
                    result.Add(LadderRow.AddRST(timer.Timer.TimerDeviceT));
                }
            }
            return result;
        }

        public List<LadderCsvRow> GenerateM6()
        {
            var result = new List<LadderCsvRow>();
            // 開始待ちタイマがある場合
            var thisTimer = _timers.Where(t => t.Timer.RecordId == _operation.Operation.Id).ToList();
            var operationTimerWait = thisTimer.FirstOrDefault(t => t.Timer.TimerCategoryId == 1);

            if (operationTimerWait != null)
            {
                result.Add(LadderRow.AddLD(_label + (_outNum + 5).ToString()));
                result.Add(LadderRow.AddANI(_label + (_outNum + 6).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerWait.Timer.TimerDeviceT ?? "",
                    operationTimerWait.Timer.TimerDeviceZR ?? ""));
            }

            // M6
            // 開始待ちタイマがあるかどうかで分岐
            if (operationTimerWait != null)
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
                result.Add(LadderRow.AddAND(operationTimerWait.Timer.TimerDeviceT ?? ""));
            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
            }
            result.Add(LadderRow.AddOR(_label + (_outNum + 6).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 6).ToString()));

            return result;
        }

        public List<LadderCsvRow> GenerateM7()
        {
            var result = new List<LadderCsvRow>();
            // 開始待ちタイマがある場合
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));

            // SCが設定されている場合
            if (_operation.Operation.SC != null && _operation.Operation.SC != 0)
            {
                var ioSensorMulti = _ioAddressService.GetAddressRange(
                    _ioList,
                    _operation.Operation.Start!,
                    _operation.Operation.OperationName!,
                    _operation.Operation.Id);

                if (ioSensorMulti != null)
                {
                    foreach (var io in ioSensorMulti)
                    {
                        result.Add(LadderRow.AddANI(io.Address!));
                    }
                }
            }
            else
            {
                var ioSensor = _ioAddressService.GetSingleAddress(
                        _ioList,
                        _operation.Operation.Start!,
                        false,
                        _operation.Operation.OperationName!,
                        _operation.Operation.Id
                        , null);

                // SCが設定されていない場合は常にON
                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
                }
                else
                {
                    if (_operation.Operation.Start!.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddAND(ioSensor));
                    }
                    else
                    {
                        result.Add(LadderRow.AddANI(ioSensor));

                    }
                }
            }

            result.Add(LadderRow.AddOR(_label + (_outNum + 6).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 7).ToString()));

            return result;
        }

        /// <summary>
        /// 制御ｾﾝｻタイマを設定します。
        /// </summary>
        /// <returns></returns>
        public List<LadderCsvRow> GenerateM9(MnemonicTimerDeviceWithOperation operationTimer)
        {
            var result = new List<LadderCsvRow>();
            var conSensor = _operation.Operation.Con;
            if (string.IsNullOrEmpty(conSensor))
            {
                return result;
            }
            else
            {
                var conAddress = _ioAddressService.GetSingleAddress(
                    _ioList,
                    conSensor,
                    false,
                    conSensor,
                    _operation.Operation.Id,
                    null);

                if (conAddress == null)
                {
                    _errorAggregator.AddError(new OutputError
                    {
                        Message = $"操作「{_operation.Operation.OperationName}」(ID: {_operation.Operation.Id}) の制御センサ「{conSensor}」が設定されていません。",
                        MnemonicId = (int)MnemonicType.Operation,
                        RecordId = _operation.Operation.Id,
                        RecordName = _operation.Operation.OperationName,
                        IsCritical = true
                    });
                    return result;
                }

                result.Add(LadderRow.AddLD(conAddress));
                result.Add(LadderRow.AddAND(_label + (_outNum + 6).ToString()));
                result.Add(LadderRow.AddANI(_label + (_outNum + 9).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimer.Timer.TimerDeviceT ?? "",
                    operationTimer.Timer.TimerDeviceZR ?? ""));

                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
                result.Add(LadderRow.AddAND(operationTimer.Timer.TimerDeviceT!));
                result.Add(LadderRow.AddOR(_label + (_outNum + 9).ToString()));
                result.Add(LadderRow.AddAND(_label + (_outNum + 6).ToString()));
                result.Add(LadderRow.AddOUT(_label + (_outNum + 9).ToString()));
            }


            return result;
        }

        public List<LadderCsvRow> GenerateSpeed(
            MnemonicTimerDeviceWithOperation operationTimer,
            string speedSensor,
            List<MnemonicSpeedDevice> speeds,
            string operationSpeed,
            int speedCount)
        {
            var result = new List<LadderCsvRow>();

            if (speedSensor!.StartsWith("T"))
            {
                result.Add(LadderRow.AddLD(_label + (_outNum + 6).ToString()));
                result.Add(LadderRow.AddANI(_label + (_outNum + 10 + speedCount).ToString()));
                result.AddRange(LadderRow.AddTimer(
                        operationTimer.Timer.TimerDeviceT ?? "",
                        operationTimer.Timer.TimerDeviceZR ?? ""));

                // M10 + sppeedCount
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
                result.Add(LadderRow.AddAND(operationTimer.Timer.TimerDeviceT!));
                result.Add(LadderRow.AddOR(_label + (_outNum + speedCount + 10).ToString()));
                result.Add(LadderRow.AddAND(_label + (_outNum + 6).ToString()));
                result.Add(LadderRow.AddOUT(_label + (_outNum + speedCount + 10).ToString()));
            }
            else
            {
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    speedSensor,
                    false,
                    _operation.Operation.OperationName,
                    _operation.Operation.Id,
                    null);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
                }
                else
                {
                    if (speedSensor.StartsWith("_"))
                    {
                        result.Add(LadderRow.AddLD(ioSensor));
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor));

                    }
                }

                if (speedCount == 0)
                {
                    result.Add(LadderRow.AddAND(_label + (_outNum + 6).ToString()));
                }
                else
                {
                    result.Add(LadderRow.AddAND(_label + (_outNum + 10 + speedCount - 1).ToString()));

                }
                result.Add(LadderRow.AddANI(_label + (_outNum + 10 + speedCount).ToString()));
                result.AddRange(LadderRow.AddTimer(
                        operationTimer.Timer.TimerDeviceT ?? "",
                        operationTimer.Timer.TimerDeviceZR ?? ""));

                // M10 + sppeedCount
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
                result.Add(LadderRow.AddAND(operationTimer.Timer.TimerDeviceT!));
                result.Add(LadderRow.AddOR(_label + (_outNum + 10 + speedCount).ToString()));
                result.Add(LadderRow.AddAND(_label + (_outNum + 6).ToString()));
                result.Add(LadderRow.AddOUT(_label + (_outNum + 10 + speedCount).ToString()));

                // 速度指令
                var speedDevice = speeds.FirstOrDefault(s => s.CylinderId == _operation.Operation.CYId)?.Device ?? "";
                result.AddRange(LadderRow.AddMOVPSet("K" + operationSpeed.ToString(), speedDevice));
            }

            return result;
        }

        public List<LadderCsvRow> GenerateServo(
            string speedSensor,
            string operationSpeed,
            int speedCount)
        {
            var result = new List<LadderCsvRow>();

            var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    speedSensor,
                    false,
                    _operation.Operation.OperationName,
                    _operation.Operation.Id,
                    null);

            // M10 + sppeedCount
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));

            if (ioSensor == null)
            {
                result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
            }
            else
            {
                if (speedSensor.StartsWith("_"))
                {
                    result.Add(LadderRow.AddAND(ioSensor));
                }
                else
                {
                    result.Add(LadderRow.AddANI(ioSensor));
                }
            }

            result.Add(LadderRow.AddOR(_label + (_outNum + +10 + speedCount).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 10 + speedCount).ToString()));

            // 速度指令
            var speedDevice = _operation.Operation.Valve1;
            if (string.IsNullOrEmpty(speedDevice))
            {
                _errorAggregator.AddError(new OutputError
                {
                    Message = $"操作「{_operation.Operation.OperationName}」(ID: {_operation.Operation.Id}) の速度デバイスが設定されていません。",
                    MnemonicId = (int)MnemonicType.Operation,
                    RecordId = _operation.Operation.Id,
                    RecordName = _operation.Operation.OperationName,
                    IsCritical = true
                });
            }
            else
            {
                result.AddRange(LadderRow.AddMOVPSet("K" + operationSpeed.ToString(), speedDevice));
            }
            return result;
        }

        public List<LadderCsvRow> GenerateM16()
        {
            var result = new List<LadderCsvRow>();
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
            // ioの取得を共通コンポーネント化すること
            // SCが設定されている場合
            if (_operation.Operation.FC != null && _operation.Operation.FC != 0)
            {
                var ioSensorMulti = _ioAddressService.GetAddressRange(
                    _ioList,
                    _operation.Operation.Finish!,
                    _operation.Operation.OperationName!,
                    _operation.Operation.Id);

                if (ioSensorMulti != null && ioSensorMulti.Count != 0)
                {
                    foreach (var io in ioSensorMulti)
                    {
                        if (io.Address!.StartsWith("X"))
                            result.Add(LadderRow.AddAND(io.Address!));
                    }
                }
            }
            else
            {
                var ioSensor = _ioAddressService.GetSingleAddress(
                        _ioList,
                        _operation.Operation.Finish!,
                        false,
                        _operation.Operation.OperationName!,
                        _operation.Operation.Id,
                        null);

                // SCが設定されていない場合は常にON
                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
                }
                else
                {
                    if (_operation.Operation.Finish!.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddANI(ioSensor));
                    }
                    else
                    {
                        result.Add(LadderRow.AddAND(ioSensor));

                    }
                }
            }
            result.Add(LadderRow.AddOR(_label + (_outNum + 16).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 16).ToString()));

            return result;
        }



        public List<LadderCsvRow> GenerateM19()
        {
            var result = new List<LadderCsvRow>();

            var thisTimer = _timers.Where(t => t.Timer.RecordId == _operation.Operation.Id).ToList();
            List<MnemonicTimerDeviceWithOperation> operationTimers = thisTimer
.Where(t => t.Timer.MnemonicId == 3 && t.Timer.RecordId == _operation.Operation.Id)
.ToList();
            var operationTimerStable = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 2);

            if (operationTimerStable != null)
            {
                result.Add(LadderRow.AddLD(_label + (_outNum + 17).ToString()));
                result.Add(LadderRow.AddANI(_label + (_outNum + 19).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerStable.Timer.TimerDeviceT ?? "",
                    operationTimerStable.Timer.TimerDeviceZR ?? ""
                    ));
            }

            // M19
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
            // 深当たりタイマがある場合
            if (operationTimerStable != null)
            {
                result.Add(LadderRow.AddAND(operationTimerStable!.Timer.TimerDeviceT!));
            }
            result.Add(LadderRow.AddOR(_label + (_outNum + 19).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 17).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 19).ToString()));

            return result;
        }

        public List<LadderCsvRow> GenerateReset()
        {
            var result = new List<LadderCsvRow>();

            result.Add(LadderRow.AddLD(_label + (_outNum + 19).ToString()));
            result.Add(LadderRow.AddRST(_label + (_outNum + 2).ToString()));
            result.Add(LadderRow.AddPLS(_label + (_outNum + 4).ToString()));

            return result;
        }

        public List<LadderCsvRow> SpeedCheck(
            List<MnemonicSpeedDevice> speeds,
            int speedChangeCount,
            List<MnemonicTimerDeviceWithOperation> operationTimers)
        {
            var result = new List<LadderCsvRow>();
            var helper = new OperationHelper(_mainViewModel, _errorAggregator, _ioAddressService);

            for (int i = 0; i < speedChangeCount; i++)
            {
                if (i >= helper.s_speedChangeConfigs.Count) continue;

                if (!helper.TryGetSpeedChangeParameters(
                    i, _operation, operationTimers, out var speedTimer, out var speedSensor))
                {
                    // TryGet内でエラーが追加されるため、ここでは何もしない
                    continue;
                }

                string? operationSpeed = string.Empty;

                // 速度変化ステップごとの処理
                switch (i)
                {
                    case 0: operationSpeed = _operation.Operation.S2; break;
                    case 1: operationSpeed = _operation.Operation.S3; break;
                    case 2: operationSpeed = _operation.Operation.S4; break;
                    case 3: operationSpeed = _operation.Operation.S5; break;
                    default:
                        // このケースは speedChangeConfigs.Count のチェックで基本的に到達しない
                        continue;
                }

                operationSpeed = helper.FlowSpeedNumber(
                    operationSpeed,
                    _operation,
                    _cylinders, i + 1);


                result.AddRange(GenerateSpeed(speedTimer!, speedSensor!, speeds, operationSpeed, i));

            }
            return result;
        }
    }
}
