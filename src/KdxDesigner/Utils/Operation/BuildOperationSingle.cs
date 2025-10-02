using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.Operation

{
    internal class BuildOperationSingle
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public BuildOperationSingle(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        public List<LadderCsvRow> Retention(
            MnemonicDeviceWithOperation operation,
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
            var label = operation.Mnemonic.DeviceLabel;
            var outNum = operation.Mnemonic.StartNum;
            var operationTimers = timers.Where(t => t.Operation.Id == operation.Operation.Id).ToList();
            var operationDetails = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();
            OperationFunction operationFunction = new(operation, operationTimers, cylinders, ioList, operationDetails, _mainViewModel, _errorAggregator, _ioAddressService);
            OperationHelper helper = new(_mainViewModel, _errorAggregator, _ioAddressService);

            // 行間ステートメント
            string id = operation.Operation.Id.ToString();
            if (string.IsNullOrEmpty(operation.Operation.OperationName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + operation.Operation.OperationName + "保持"));
            }

            // OperationIdが一致する工程詳細のフィルタリング
            // M0
            var detailList = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();
            result.AddRange(operationFunction.GenerateM0());

            // M2
            result.AddRange(operationFunction.GenerateM2());

            // M5
            result.AddRange(operationFunction.GenerateM5());

            // M6
            result.AddRange(operationFunction.GenerateM6());

            // M7
            result.AddRange(operationFunction.GenerateM7());

            // M16
            if (operation.Operation.Finish != null)
            {
                result.AddRange(operationFunction.GenerateM16());
            }

            // M17
            var thisTimer = timers.Where(t => t.Timer.RecordId == operation.Operation.Id).ToList();
            var operationTimerONWait = thisTimer.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);
            // 深当たりタイマがある場合
            if (operation.Operation.Finish != null && operationTimerONWait != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 16).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 17).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
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

            if (operation.Operation.Finish != null)
            {
                result.Add(LadderRow.AddAND(label + (outNum + 16).ToString()));
            }
            else if (operation.Operation.Start != null)
            {
                result.Add(LadderRow.AddAND(label + (outNum + 7).ToString()));
            }
            else
            {
                result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
            }
            result.Add(LadderRow.AddOUT(label + (outNum + 17).ToString()));

            // M19
            result.AddRange(operationFunction.GenerateM19());

            // Reset信号の生成
            result.AddRange(operationFunction.GenerateReset());

            // エラー回路の生成
            ErrorBuilder errorBuilder = new(_errorAggregator);

            result.AddRange(errorBuilder.Operation(
                operation.Operation,
                mnemonicError,
                label,
                outNum
            ));

            ProsTimeBuilder prosTimeBuilder = new(_errorAggregator);

            // 工程タイムの生成
            result.AddRange(prosTimeBuilder.Common(
                operation.Operation,
                prosTimes,
                label,
                outNum));

            if (_mainViewModel.IsDebug)
            {
                IODebug iODebug = new(_mainViewModel, operation, cylinders, ioList, _errorAggregator, _ioAddressService);
                result.AddRange(iODebug.GenerateValve());
            }


            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> Excitation(
                MnemonicDeviceWithOperation operation,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<ProcessError> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            var label = operation.Mnemonic.DeviceLabel; // ラベルの取得
            var outNum = operation.Mnemonic.StartNum; // スタート番号の取得
            var operationTimers = timers.Where(t => t.Operation.Id == operation.Operation.Id).ToList();
            var operationDetails = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();
            OperationFunction operationFunction = new(operation, operationTimers, cylinders, ioList, operationDetails, _mainViewModel, _errorAggregator, _ioAddressService);

            // 実際の処理ロジックをここに追加

            // 行間ステートメント
            string id = operation.Operation.Id.ToString();
            if (string.IsNullOrEmpty(operation.Operation.OperationName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + operation.Operation.OperationName + "励磁"));
            }

            // OperationIdが一致する工程詳細のフィルタリング
            // M0
            result.AddRange(operationFunction.GenerateM0());

            // M2
            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 2).ToString()));

            // M5
            result.AddRange(operationFunction.GenerateM5());

            // M6
            result.AddRange(operationFunction.GenerateM6());

            // M17
            var operationTimerONWait = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);
            // 深当たりタイマがある場合
            if (operationTimerONWait != null)
            {
                bool isFirst = true;
                foreach (var detail in operationDetails)
                {
                    var detailLabel = detail.Mnemonic.DeviceLabel; // 工程詳細のラベル取得
                    var detailOutNum = detail.Mnemonic.StartNum; // 工程詳細のラベル取得

                    if (isFirst)
                    {
                        result.Add(LadderRow.AddLDP(detailLabel + (detailOutNum + 2).ToString()));
                        isFirst = false;
                    }
                    else
                    {
                        result.Add(LadderRow.AddORP(detailLabel + (detailOutNum + 2).ToString()));
                    }
                }

                result.Add(LadderRow.AddOR(label + (outNum + 16).ToString()));
                result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                result.Add(LadderRow.AddOUT(label + (outNum + 16).ToString()));

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
            if (operationTimerONWait != null && operationTimerONWait.Timer.TimerDeviceT != null)
            {
                result.Add(LadderRow.AddAND(operationTimerONWait.Timer.TimerDeviceT));
            }
            else
            {
                result.Add(LadderRow.AddANI(label + (outNum + 0).ToString()));

            }

            result.Add(LadderRow.AddOR(label + (outNum + 17).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 17).ToString()));

            // M18
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddOR(label + (outNum + 18).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 17).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 18).ToString()));

            // M19
            var thisTimer = timers.Where(t => t.Timer.RecordId == operation.Operation.Id).ToList();
            List<MnemonicTimerDeviceWithOperation> stTimers = thisTimer
.Where(t => t.Timer.MnemonicId == 3 && t.Timer.RecordId == operation.Operation.Id)
.ToList();
            var operationTimerStable = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 2);

            if (operationTimerStable != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 17).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerStable.Timer.TimerDeviceT ?? "",
                    operationTimerStable.Timer.TimerDeviceZR ?? ""
                    ));
            }

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            // 深当たりタイマがある場合
            if (operationTimerStable != null)
            {
                result.Add(LadderRow.AddAND(operationTimerStable!.Timer.TimerDeviceT!));
            }
            result.Add(LadderRow.AddOR(label + (outNum + 19).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 18).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 19).ToString()));

            // Reset信号の生成
            result.AddRange(operationFunction.GenerateReset());

            ProsTimeBuilder prosTimeBuilder = new(_errorAggregator);

            // 工程タイムの生成
            result.AddRange(prosTimeBuilder.Common(
                operation.Operation,
                prosTimes,
                label!,
                outNum));

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> ExcitationOFF(
                MnemonicDeviceWithOperation operation,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<ProcessError> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            var label = operation.Mnemonic.DeviceLabel; // ラベルの取得
            var outNum = operation.Mnemonic.StartNum; // スタート番号の取得
            var operationTimers = timers.Where(t => t.Operation.Id == operation.Operation.Id).ToList();
            var operationDetails = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();
            OperationFunction operationFunction = new(operation, operationTimers, cylinders, ioList, operationDetails, _mainViewModel, _errorAggregator, _ioAddressService);

            // 実際の処理ロジックをここに追加

            // 行間ステートメント
            string id = operation.Operation.Id.ToString();
            if (string.IsNullOrEmpty(operation.Operation.OperationName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + operation.Operation.OperationName));
            }

            // OperationIdが一致する工程詳細のフィルタリング
            // M0
            result.AddRange(operationFunction.GenerateM0());

            // M2
            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 2).ToString()));

            // M5
            result.AddRange(operationFunction.GenerateM5());

            // M6
            result.AddRange(operationFunction.GenerateM6());

            // M9 強制OFF
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 17).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 9).ToString()));

            // M17
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOR(label + (outNum + 8).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 17).ToString()));


            // M18
            var operationTimerONWait = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);
            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 8).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 18).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerONWait.Timer.TimerDeviceT ?? "",
                    operationTimerONWait.Timer.TimerDeviceZR ?? ""
                    ));
            }

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            // 深当たりタイマがある場合
            if (operationTimerONWait != null && operationTimerONWait.Timer.TimerDeviceT != null)
            {
                result.Add(LadderRow.AddAND(operationTimerONWait.Timer.TimerDeviceT));
            }
            result.Add(LadderRow.AddOR(label + (outNum + 18).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 17).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 18).ToString()));

            // M19
            result.AddRange(operationFunction.GenerateM19());

            // Reset信号の生成
            result.AddRange(operationFunction.GenerateReset());

            ProsTimeBuilder prosTimeBuilder = new(_errorAggregator);

            // 工程タイムの生成
            result.AddRange(prosTimeBuilder.Common(
                operation.Operation,
                prosTimes,
                label!,
                outNum));

            return result; // 生成されたLadderCsvRowのリストを返す
        }


    }
}
