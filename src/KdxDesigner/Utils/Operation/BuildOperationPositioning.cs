using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;
using MnemonicSpeedDevice = Kdx.Contracts.DTOs.MnemonicSpeedDevice;
namespace KdxDesigner.Utils.Operation

{
    internal class BuildOperationPositioning
    {

        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public BuildOperationPositioning(
            MainViewModel mainViewModel,
            IErrorAggregator errorAggregator,
            IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }


        public List<LadderCsvRow> ServoPositioning(
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
            // ここに単一工程の処理を実装
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            var operationDetails = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();
            OperationFunction operationFunction
                = new(operation, timers, cylinders, ioList, operationDetails, _mainViewModel, _errorAggregator, _ioAddressService);
            OperationHelper helper = new(_mainViewModel, _errorAggregator, _ioAddressService);

            var servo = _mainViewModel._selectedServo.FirstOrDefault(s => s.CylinderId == operation.Operation.CYId);
            string label = string.Empty;
            int outNum = 0;

            if (operation != null
                && operation.Mnemonic != null
                && operation.Mnemonic.DeviceLabel != null
                && servo != null)
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
                .Where(t => t.Operation.Id == operation!.Operation.Id).ToList();

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

            // M7
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));

            result.Add(LadderRow.AddAND(servo!.Prefix + servo!.Status + ".E"));

            result.Add(LadderRow.AddOR(label + (outNum + 7).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 7).ToString()));

            var speedDevice = speeds.FirstOrDefault(s => s.CylinderId == operation.Operation.CYId)?.Device;

            if (operation.Operation.Start == null)
            {
                helper.CreateOperationError(operation, "Startが設定されていません。");
                return result;
            }
            else if (servo == null)
            {
                helper.CreateOperationError(operation, "サーボが設定されていません。");
                return result;
            }
            else if (speedDevice == null)
            {
                helper.CreateOperationError(operation, "速度デバイスが設定されていません。");
                return result;
            }

            result.AddRange(LadderRow.AddMOVPSet("K" + operation.Operation.Start.ToString(), speedDevice));

            // M10 : 速度変化の処理
            for (int i = 0; i < speedChangeCount; i++)
            {
                string speedSensor;
                string operationSpeed;
                switch (i)
                {
                    case 0:
                        speedSensor = operation.Operation.SS1 ?? "";
                        operationSpeed = operation.Operation.S1 ?? "";
                        break;
                    case 1:
                        speedSensor = operation.Operation.SS2 ?? "";
                        operationSpeed = operation.Operation.S2 ?? "";
                        break;
                    case 2:
                        speedSensor = operation.Operation.SS3 ?? "";
                        operationSpeed = operation.Operation.S3 ?? "";
                        break;
                    case 3:
                        speedSensor = operation.Operation.SS4 ?? "";
                        operationSpeed = operation.Operation.S4 ?? "";
                        break;

                    default:
                        helper.CreateOperationError(operation, $"不正な速度変化ステップ インデックス: {i + 1}");
                        return result;
                }
                result.AddRange(operationFunction.GenerateServo(speedSensor, operationSpeed, i));

            }

            // M17
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(servo.Prefix + servo.Status + ".F"));
            result.Add(LadderRow.AddOR(label + (outNum + 17).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 16).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 17).ToString()));

            var operationTimerStable = timers.FirstOrDefault(t => t.Timer.TimerCategoryId == 2);
            var operationTimerONWait = timers.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);

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

            return result; // 生成されたLadderCsvRowのリストを返す

        }

    }
}
