using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;
using MnemonicSpeedDevice = Kdx.Contracts.DTOs.MnemonicSpeedDevice;
using OperationDto = Kdx.Contracts.DTOs.Operation;
namespace KdxDesigner.Utils.Operation
{
    internal class BuildOperationSpeedChange
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public BuildOperationSpeedChange(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        // KdxDesigner.Utils.Operation.BuildOperationSpeedChange クラス内
        public List<LadderCsvRow> Inverter(
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<ProcessError> mnemonicError,
            List<ProsTime> prosTimes,
            List<MnemonicSpeedDevice> speeds,
            List<IO> ioList,
            int speedChangeCount)
        {
            var result = new List<LadderCsvRow>();
            var operationDetails = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();
            OperationFunction operationFunction = new(operation, timers, cylinders, ioList, operationDetails, _mainViewModel, _errorAggregator, _ioAddressService);
            OperationHelper helper = new(_mainViewModel, _errorAggregator, _ioAddressService);

            string label = string.Empty;
            int outNum = 0;

            if (operation != null
                && operation.Mnemonic != null
                && operation.Mnemonic.DeviceLabel != null)
            {
                label = operation.Mnemonic.DeviceLabel;
                outNum = operation.Mnemonic.StartNum;

            }
            else
            {
                helper.CreateOperationError(operation!, $"Mnemonicデバイスが設定されていません。");
                return result;
            }

            List<MnemonicTimerDeviceWithOperation> operationTimers = timers
                .Where(t => t.Timer.MnemonicId == 3 && t.Timer.RecordId == operation.Operation.Id)
                .ToList();


            string id = operation!.Operation.Id.ToString();
            if (string.IsNullOrEmpty(operation.Operation.OperationName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + operation.Operation.OperationName));
            }

            var detailList = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();

            // outNum.Value を一度変数に格納して再利用
            var outNumValue = outNum;

            result.AddRange(operationFunction.GenerateM0());
            result.AddRange(operationFunction.GenerateM2());
            result.AddRange(operationFunction.GenerateM5());
            result.AddRange(operationFunction.GenerateM6());

            if (operation.Operation.S1 != null)
            {
                var speedDevice = speeds.FirstOrDefault(s => s.CylinderId == operation.Operation.CYId)?.Device ?? "";
                var operationSpeed = operation.Operation.S1;
                if (operationSpeed.Contains("B"))
                {
                    string speedValueStr = operationSpeed.Replace("A", "").Replace("B", "");

                    if (int.TryParse(speedValueStr, out int speedValue))
                    {
                        var flow = cylinders.SingleOrDefault(c => c.Cylinder.Id == operation.Operation.CYId)?.Cylinder?.FlowType;

                        switch (flow)
                        {
                            case "A5:B5":
                                speedValueStr = (speedValue + 5).ToString();
                                break;
                            case "A6:B4":
                                speedValueStr = (speedValue + 6).ToString();
                                break;
                            case "A7:B3":
                                speedValueStr = (speedValue + 7).ToString();
                                break;
                            case "A10:B0":
                                speedValueStr = (speedValue + 10).ToString();
                                break;
                            default:
                                speedValueStr = "";
                                break;
                        }
                    }
                    result.AddRange(LadderRow.AddMOVPSet("K" + speedValueStr.ToString().Replace("A", "").Replace("B", ""), speedDevice));

                }
                else
                {
                    result.AddRange(LadderRow.AddMOVPSet("K" + operation.Operation.S1.ToString().Replace("A", "").Replace("B", ""), speedDevice));

                }

            }

            // M7 : 出力開始の処理
            result.AddRange(operationFunction.GenerateM7());

            if (operation.Operation.Con != null && operation.Operation.Con != string.Empty)
            {
                var conTimer = operationTimers.SingleOrDefault(t => t.Timer.TimerCategoryId == 15);

                if (conTimer == null)
                {
                    helper.CreateOperationError(operation!, $"Conタイマが設定されていません。");
                }
                else
                {
                    result.AddRange(operationFunction.GenerateM9(conTimer));
                }

            }

            // M10 : 速度変化の処理
            result.AddRange(operationFunction.SpeedCheck(speeds, speedChangeCount, operationTimers));

            // M16
            if (operation.Operation.Finish != null)
            {
                result.AddRange(operationFunction.GenerateM16());
            }
            else
            {
                helper.CreateOperationError(operation!, $"Finishが入力されていません。");
                return result;
            }

            // M17
            var operationTimersSecond = timers.Where(t => t.Operation.Id == operation.Operation.Id).ToList();
            var operationTimerONWait = operationTimersSecond.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);
            // 深当たりタイマがある場合
            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 16).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 17).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerONWait.Timer.TimerDeviceT ?? "",
                    operationTimerONWait.Timer.TimerDeviceZR ?? ""
                    ));
            }

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            // 深当たりタイマがある場合
            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddAND(operationTimerONWait.Timer.TimerDeviceT!));
            }
            result.Add(LadderRow.AddOR(label + (outNum + 17).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 16).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 17).ToString()));

            // M19
            result.AddRange(operationFunction.GenerateM19());
            // Reset信号の生成
            result.AddRange(operationFunction.GenerateReset());


            // エラー回路の生成
            ErrorBuilder errorBuilder = new(_errorAggregator);
            result.AddRange(errorBuilder.Operation(operation.Operation, mnemonicError, label, outNumValue));

            // 工程タイムの生成
            ProsTimeBuilder prosTimeBuilder = new(_errorAggregator);
            result.AddRange(prosTimeBuilder.Common(operation.Operation, prosTimes, label, outNumValue));

            // IO Debug
            if (_mainViewModel.IsDebug)
            {
                IODebug iODebug = new(_mainViewModel, operation, cylinders, ioList, _errorAggregator, _ioAddressService);
                result.AddRange(iODebug.GenerateSpeed(speedChangeCount));
            }

            return result;
        }

        public class SpeedChangeStepConfig
        {
            public int TimerCategoryId { get; }
            public Func<OperationDto, string> SensorAccessor { get; }
            public string SensorPropertyName { get; } // エラーメッセージ用

            public SpeedChangeStepConfig(int categoryId, Func<OperationDto, string> accessor, string sensorPropertyName)
            {
                TimerCategoryId = categoryId;
                SensorAccessor = accessor;
                SensorPropertyName = sensorPropertyName;
            }
        }
    }
}
