using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.Operation
{
    /// <summary>
    /// OperationのIOデバッグに関連するラダーロジックを生成します。
    /// </summary>
    internal class IODebug
    {
        private readonly MainViewModel _mainViewModel;
        private readonly MnemonicDeviceWithOperation _operation;
        private readonly List<IO> _ioList;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        private readonly string _label;
        private readonly int _outNum;
        private readonly List<MnemonicDeviceWithCylinder> _cylinders;

        public IODebug(
            MainViewModel mainViewModel,
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            IErrorAggregator errorAggregator,
            IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _operation = operation;
            _cylinders = cylinders;
            _ioList = ioList;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
            _label = operation.Mnemonic.DeviceLabel ?? "";
            _outNum = operation.Mnemonic.StartNum;
        }

        /// <summary>
        /// IOデバッグ用の共通ラダー回路を生成します。
        /// </summary>
        public List<LadderCsvRow> GenerateValve()
        {
            var result = new List<LadderCsvRow>();

            if (_operation.Operation.Start != null)
            {
                // RSTセンサーの処理
                if (!string.IsNullOrEmpty(_operation.Operation.SC) && _operation.Operation.SC != "0")
                {
                    // SC複数センサである
                    // Startセンサーがある
                    var startSensors = _ioAddressService.GetAddressRange(
                       _ioList,
                       _operation.Operation.Start!,
                       _operation.Operation.OperationName!,
                       _operation.Operation.Id);

                    foreach (var sensor in startSensors)
                    {
                        if (sensor.Address != null)
                        {
                            result.AddRange(GenerateValveAddress());
                            result.Add(LadderRow.AddRST(sensor.Address));
                        }
                    }
                }
                else
                {
                    // 複数センサではない
                    // Startセンサーがある
                    var startSensor = _ioAddressService.GetSingleAddressOperation(
                       _ioList,
                       _operation.Operation.Start!,
                       isOutput: false,
                       _operation.Operation,
                       isnotInclude: null);

                    if (startSensor != null)
                    {
                        result.AddRange(GenerateValveAddress());
                        result.Add(LadderRow.AddRST(startSensor));
                    }
                }
            }

            if (_operation.Operation.Con != null)
            {
                var controllerSensor = _ioAddressService.GetSingleAddressOperation(
                                           _ioList,
                                           _operation.Operation.Con,
                                           isOutput: false,
                                           _operation.Operation,
                                           isnotInclude: null);

                if (controllerSensor != null)
                {
                    result.AddRange(GenerateValveAddress());
                    result.Add(LadderRow.AddSET(controllerSensor));
                    result.AddRange(GenerateValveAddress());
                    result.Add(LadderRow.AddAND(_label + (_outNum + 19)));
                    result.Add(LadderRow.AddRST(controllerSensor));
                }
            }


            // Startセンサーが無い
            // Finishセンサーの処理
            if (_operation.Operation.Finish != null)
            {
                if (!string.IsNullOrEmpty(_operation.Operation.FC) && _operation.Operation.FC != "0")
                {
                    // FC複数センサである
                    // Finishセンサーがある
                    var finishSensors = _ioAddressService.GetAddressRange(
                       _ioList,
                       _operation.Operation.Finish!,
                       _operation.Operation.OperationName!,
                       _operation.Operation.Id);

                    foreach (var sensor in finishSensors)
                    {
                        if (sensor.Address != null)
                        {
                            result.AddRange(GenerateValveAddress());
                            result.Add(LadderRow.AddAND(_label + (_outNum + 7)));
                            result.Add(LadderRow.AddSET(sensor.Address));
                        }
                    }
                }
                else
                {
                    // 複数センサではない
                    // Startセンサーがある
                    var finishSensor = _ioAddressService.GetSingleAddressOperation(
                       _ioList,
                       _operation.Operation.Finish,
                       isOutput: false,
                       _operation.Operation,
                       isnotInclude: null);

                    if (finishSensor != null)
                    {
                        result.AddRange(GenerateValveAddress());
                        result.Add(LadderRow.AddAND(_label + (_outNum + 7)));
                        result.Add(LadderRow.AddSET(finishSensor));
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// IOデバッグ用の共通ラダー回路を生成します。
        /// </summary>
        public List<LadderCsvRow> GenerateSpeed(int speedCount)
        {
            var result = new List<LadderCsvRow>();

            if (_operation.Operation.Start != null)
            {
                // RSTセンサーの処理
                if (!string.IsNullOrEmpty(_operation.Operation.SC) && _operation.Operation.SC != "0")
                {
                    // SC複数センサである
                    // Startセンサーがある
                    var startSensors = _ioAddressService.GetAddressRange(
                       _ioList,
                       _operation.Operation.Start!,
                       _operation.Operation.OperationName!,
                       _operation.Operation.Id);

                    foreach (var sensor in startSensors)
                    {
                        if (sensor.Address != null)
                        {
                            result.AddRange(GenerateValveAddress());
                            result.Add(LadderRow.AddRST(sensor.Address));
                        }
                    }
                }
                else
                {
                    // 複数センサではない
                    // Startセンサーがある
                    var startSensor = _ioAddressService.GetSingleAddressOperation(
                       _ioList,
                       _operation.Operation.Start!,
                       isOutput: false,
                       _operation.Operation,
                       isnotInclude: null);

                    if (startSensor != null)
                    {
                        result.AddRange(GenerateValveAddress());
                        result.Add(LadderRow.AddRST(startSensor));
                    }
                }
            }

            if (_operation.Operation.Con != null)
            {
                var controllerSensor = _ioAddressService.GetSingleAddressOperation(
                                           _ioList,
                                           _operation.Operation.Con,
                                           isOutput: false,
                                           _operation.Operation,
                                           isnotInclude: null);

                if (controllerSensor != null)
                {
                    result.AddRange(GenerateValveAddress());
                    result.Add(LadderRow.AddSET(controllerSensor));
                }
            }

            bool isSpeed = false;
            var previousSpeedSensor = string.Empty;
            for (var count = 0; count < speedCount; count++)
            {
                // Speedセンサーの処理
                switch (count)
                {
                    case 0:
                        if (_operation.Operation.SS1 != null)
                        {
                            if (_operation.Operation.SS1 == "T") break;

                            var speedSensor = _ioAddressService.GetSingleAddressOperation(
                                _ioList,
                                _operation.Operation.SS1,
                                isOutput: false,
                                _operation.Operation,
                                isnotInclude: null);
                            if (speedSensor != null)
                            {
                                result.AddRange(GenerateValveAddress());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 7)));
                                result.Add(LadderRow.AddSET(speedSensor));
                                previousSpeedSensor = speedSensor;
                                isSpeed = true;
                            }
                        }
                        break;

                    case 1:
                        if (_operation.Operation.SS2 != null)
                        {
                            var speedSensor = _ioAddressService.GetSingleAddressOperation(
                                _ioList,
                                _operation.Operation.SS2,
                                isOutput: false,
                                _operation.Operation,
                                isnotInclude: null);
                            if (speedSensor != null)
                            {
                                if (_operation.Operation.SS1 != "T")
                                {
                                    result.AddRange(GenerateValveAddress());
                                    result.Add(LadderRow.AddAND(_label + (_outNum + 9 + count)));
                                    result.Add(LadderRow.AddRST(previousSpeedSensor));
                                }
                                result.AddRange(GenerateValveAddress());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 9 + count)));
                                result.Add(LadderRow.AddSET(speedSensor));
                                previousSpeedSensor = speedSensor;
                                isSpeed = true;
                            }
                        }
                        break;

                    case 2:
                        if (_operation.Operation.SS3 != null)
                        {
                            var speedSensor = _ioAddressService.GetSingleAddressOperation(
                                _ioList,
                                _operation.Operation.SS3,
                                isOutput: false,
                                _operation.Operation,
                                isnotInclude: null);
                            if (speedSensor != null)
                            {
                                result.AddRange(GenerateValveAddress());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 9 + count)));
                                result.Add(LadderRow.AddRST(previousSpeedSensor));

                                result.AddRange(GenerateValveAddress());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 9 + count)));
                                result.Add(LadderRow.AddSET(speedSensor));
                                previousSpeedSensor = speedSensor;
                                isSpeed = true;
                            }
                        }
                        break;

                    case 3:
                        if (_operation.Operation.SS4 != null)
                        {
                            var speedSensor = _ioAddressService.GetSingleAddressOperation(
                                _ioList,
                                _operation.Operation.SS4,
                                isOutput: false,
                                _operation.Operation,
                                isnotInclude: null);
                            if (speedSensor != null)
                            {
                                result.AddRange(GenerateValveAddress());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 9 + count)));
                                result.Add(LadderRow.AddRST(previousSpeedSensor));

                                result.AddRange(GenerateValveAddress());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 9 + count)));
                                result.Add(LadderRow.AddSET(speedSensor));
                                previousSpeedSensor = speedSensor;
                                isSpeed = true;
                            }
                        }
                        break;
                }
            }

            if (isSpeed)
            {
                result.AddRange(GenerateValveAddress());
                result.Add(LadderRow.AddAND(_label + (_outNum + 9 + speedCount)));
                result.Add(LadderRow.AddRST(previousSpeedSensor));
            }

            if (_operation.Operation.Con != null)
            {
                var controllerSensor = _ioAddressService.GetSingleAddressOperation(
                                           _ioList,
                                           _operation.Operation.Con,
                                           isOutput: false,
                                           _operation.Operation,
                                           isnotInclude: null);

                if (controllerSensor != null)
                {
                    result.AddRange(GenerateValveAddress());
                    result.Add(LadderRow.AddAND(_label + (_outNum + 19)));
                    result.Add(LadderRow.AddRST(controllerSensor));
                }
            }

            // Startセンサーが無い
            // Finishセンサーの処理
            if (_operation.Operation.Finish != null)
            {
                if (!string.IsNullOrEmpty(_operation.Operation.FC) && _operation.Operation.FC != "0")
                {
                    // FC複数センサである
                    // Finishセンサーがある
                    var finishSensors = _ioAddressService.GetAddressRange(
                       _ioList,
                       _operation.Operation.Finish!,
                       _operation.Operation.OperationName!,
                       _operation.Operation.Id);

                    foreach (var sensor in finishSensors)
                    {
                        if (sensor.Address != null) result.Add(LadderRow.AddSET(sensor.Address));
                    }
                }
                else
                {
                    // 複数センサではない
                    // Startセンサーがある
                    var finishSensor = _ioAddressService.GetSingleAddressOperation(
                       _ioList,
                       _operation.Operation.Finish,
                       isOutput: false,
                       _operation.Operation,
                       isnotInclude: null);

                    if (finishSensor != null) result.Add(LadderRow.AddSET(finishSensor));
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private List<LadderCsvRow> GenerateValveAddress()
        {
            var result = new List<LadderCsvRow>();

            // --- デバッグパルスによる基本条件 ---
            result.Add(LadderRow.AddLDP(SettingsManager.Settings.DebugPulse));
            result.Add(LadderRow.AddAND(_label + (_outNum + 6)));
            result.Add(LadderRow.AddANI(_label + (_outNum + 18)));

            string? valve1Address = SettingsManager.Settings.AlwaysON;

            // --- Valve1 の処理 (バルブは出力なので isOutput: true) ---

            switch (_operation.Operation.CategoryId)
            {
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                case 23:
                case 24:
                case 25:
                case 26:
                    break;
                default:
                    if (!string.IsNullOrEmpty(_operation.Operation.Valve1) && _operation.Operation.CategoryId != 20)
                    {
                        // ★修正: GetSingleAddress の引数を最新のI/Fに合わせる
                        valve1Address = _ioAddressService.GetSingleAddress(
                            _ioList,
                            _operation.Operation.Valve1,
                            isOutput: true,
                            _operation.Operation.OperationName!,
                            _operation.Operation.Id,
                            isnotInclude: null);

                        if (valve1Address == null) valve1Address = SettingsManager.Settings.AlwaysON;

                    }
                    result.Add(LadderRow.AddAND(valve1Address));
                    break;
            }

            return result;

        }
    }
}
