using Kdx.Contracts.DTOs;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;
using MnemonicSpeedDevice = Kdx.Contracts.DTOs.MnemonicSpeedDevice;

namespace KdxDesigner.Utils.Operation
{
    public class OperationBuilder
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public OperationBuilder(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        public List<LadderCsvRow> GenerateLadder(
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<MnemonicSpeedDevice> speed,
            List<ProcessError> mnemonicErrors,
            List<ProsTime> prosTimes,
            List<IO> ioList)
        {
            LadderCsvRow.ResetKeyCounter();                     // 0から再スタート
            var allRows = new List<LadderCsvRow>();
            List<OutputError> errorsForOperation = new(); // 各工程詳細のエラーリスト
            BuildOperationSingle buildOperationSingle = new(_mainViewModel, _errorAggregator, _ioAddressService);
            BuildOperationSpeedChange buildOperationSpeed = new(_mainViewModel, _errorAggregator, _ioAddressService);
            BuildOperationPositioning buildOperationPositioning = new(_mainViewModel, _errorAggregator, _ioAddressService);



            foreach (var operation in operations)
            {
                switch (operation.Operation.CategoryId)
                {
                    case 1:                // 励磁
                        allRows.AddRange(buildOperationSingle.Excitation(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            ioList
                            ));
                        break;
                    case 2 or 20:     // 保持
                        allRows.AddRange(buildOperationSingle.Retention(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            ioList
                            ));
                        break;
                    case 14:
                        allRows.AddRange(buildOperationSingle.ExcitationOFF(
                        operation,
                        details,
                        operations,
                        cylinders,
                        timers,
                        mnemonicErrors,
                        prosTimes,
                        ioList
                        ));
                        break;
                    case 3 or 9 or 15 or 27:      // 速度変化1回
                        allRows.AddRange(buildOperationSpeed.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            0
                            ));
                        break;

                    case 4 or 10 or 16 or 28:     // 速度変化2回
                        allRows.AddRange(buildOperationSpeed.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            1
                            ));
                        break;

                    case 5 or 11 or 17:     // 速度変化3回
                        allRows.AddRange(buildOperationSpeed.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            2
                            ));
                        break;

                    case 6 or 12 or 18:     // 速度変化4回
                        allRows.AddRange(buildOperationSpeed.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            3
                            ));
                        break;

                    case 7 or 13 or 19:     // 速度変化5回
                        allRows.AddRange(buildOperationSpeed.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            4
                            ));
                        break;

                    case 31:
                        allRows.AddRange(buildOperationPositioning.ServoPositioning(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            0
                            ));
                        break;
                    case 32:
                        allRows.AddRange(buildOperationPositioning.ServoPositioning(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            1
                            ));
                        break;
                    case 33:
                        allRows.AddRange(buildOperationPositioning.ServoPositioning(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            2
                            ));
                        break;
                    case 34:
                        allRows.AddRange(buildOperationPositioning.ServoPositioning(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            3
                            ));
                        break;

                    case 35:
                        allRows.AddRange(buildOperationPositioning.ServoPositioning(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            4
                            ));
                        break;
                    default:
                        break;
                }
            }

            return allRows;
        }

    }
}
