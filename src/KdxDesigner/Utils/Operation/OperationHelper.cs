using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;
using static KdxDesigner.Utils.Operation.BuildOperationSpeedChange;

namespace KdxDesigner.Utils.Operation
{
    internal class OperationHelper
    {

        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public OperationHelper(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        // BuildOperationSpeedChange クラス内
        public readonly List<SpeedChangeStepConfig> s_speedChangeConfigs = new()
        {
            new SpeedChangeStepConfig(9,  op => op.SS1!, "SS1"),
            new SpeedChangeStepConfig(10, op => op.SS2!, "SS2"),
            new SpeedChangeStepConfig(11, op => op.SS3!, "SS3"),
            new SpeedChangeStepConfig(12, op => op.SS4!, "SS4")
        };

        // BuildOperationSpeedChange クラス内
        public bool TryGetSpeedChangeParameters(
            int speedChangeIndex,
            MnemonicDeviceWithOperation operation,
            List<MnemonicTimerDeviceWithOperation> operationTimers,
            out MnemonicTimerDeviceWithOperation? speedTimer,
            out string? speedSensor)
        {
            speedTimer = null;
            speedSensor = null;

            if (speedChangeIndex < 0 || speedChangeIndex >= s_speedChangeConfigs.Count)
            {
                // 通常は到達しないが、念のため
                CreateOperationError(operation, $"不正な速度変化ステップ インデックス: {speedChangeIndex + 1}");
                return false;
            }

            var config = s_speedChangeConfigs[speedChangeIndex];

            try
            {
                speedTimer = operationTimers.SingleOrDefault(t => t.Timer.TimerCategoryId == config.TimerCategoryId);
                if (speedTimer == null)
                {
                    CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、カテゴリID {config.TimerCategoryId} (速度変化 {speedChangeIndex + 1}) のタイマーが設定されていません。");
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、カテゴリID {config.TimerCategoryId} (速度変化 {speedChangeIndex + 1}) のタイマーが複数設定されています。");
                return false;
            }

            speedSensor = config.SensorAccessor(operation.Operation);
            if (string.IsNullOrEmpty(speedSensor))
            {
                CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、速度変化 {speedChangeIndex + 1} ({config.SensorPropertyName}) のセンサーが設定されていません。");
                return false;
            }

            return true; // 全て成功
        }

        public void CreateOperationError(MnemonicDeviceWithOperation operation, string message)
        {
            var error = new OutputError
            {
                Message = message,
                RecordName = operation.Operation?.OperationName ?? "N/A",
                MnemonicId = (int)MnemonicType.Operation,
                RecordId = operation.Operation?.Id ?? 0
            };
            _errorAggregator.AddError(error);
        }

        public string FlowSpeedNumber(
            string? operationSpeed,
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithCylinder> cylinders,
            int stepNumber)
        {
            if (string.IsNullOrEmpty(operationSpeed))
            {
                CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) の速度変化ステップ {stepNumber} (S{stepNumber}) が設定されていません。");
                return string.Empty;
            }

            if (operationSpeed.Contains("A"))
            {
                return operationSpeed.Replace("A", "");
            }

            if (operationSpeed.Contains("B"))
            {
                string speedValueStr = operationSpeed.Replace("B", "");
                if (!int.TryParse(speedValueStr, out int speedValue))
                {
                    CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) の速度 {operationSpeed} は不正な形式です。");
                    return string.Empty;
                }

                var flow = cylinders.SingleOrDefault(c => c.Cylinder.Id == operation.Operation.CYId)?.Cylinder?.FlowType;
                switch (flow)
                {
                    case "A5:B5": return (speedValue + 5).ToString();
                    case "A6:B4": return (speedValue + 6).ToString();
                    case "A7:B3": return (speedValue + 7).ToString();
                    case "A10:B0": return (speedValue + 10).ToString();
                    default:
                        CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) の FlowType '{flow}' は未対応です。");
                        return speedValueStr; // 元の数値を返す
                }
            }
            return operationSpeed;
        }
    }
}
