using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Interfaces;
using OperationDto = Kdx.Contracts.DTOs.Operation;

namespace KdxDesigner.Utils.Operation
{
    internal class ErrorBuilder
    {
        private readonly IErrorAggregator _errorAggregator;

        public ErrorBuilder(IErrorAggregator errorAggregator)
        {
            _errorAggregator = errorAggregator;
        }

        public List<LadderCsvRow> Operation(
            OperationDto operation,
            List<ProcessError> error,
            string label,
            int outNum)
        {
            List<LadderCsvRow>? result = new();

            List<ProcessError> errorList = error.Where(e => e.RecordId == operation.Id).ToList();

            int countSpeed = 0;

            foreach (var each in errorList)
            {
                switch (each.AlarmId)
                {
                    case 1: // 開始
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 7).ToString()));
                        break;

                    case 2: // 開始確認
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 7).ToString()));
                        result.Add(LadderRow.AddAND(label + (outNum + 19).ToString()));
                        break;

                    case 3: // 途中TO
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 10 + countSpeed).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
                        countSpeed++;
                        break;

                    case 4: // 取り込みTO
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 10 + countSpeed).ToString()));
                        result.Add(LadderRow.AddAND(label + (outNum + 19).ToString()));
                        break;

                    case 5: // 完了TO
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
                        break;

                    default:
                        AddError("不明なエラータイプです。", each.Device ?? "", each.MnemonicId ?? 0, operation.Id);
                        continue;

                }

                // アウトコイルの出力
                if (!string.IsNullOrEmpty(each.ErrorTimeDevice) && !string.IsNullOrEmpty(each.Device))
                {
                    result.AddRange(LadderRow.AddTimer(each.ErrorTimeDevice, "K" + each.ErrorTime.ToString()));
                    result.Add(LadderRow.AddLD(each.ErrorTimeDevice));
                    result.Add(LadderRow.AddOUT(each.Device));
                    // エラー番号のMOVセット
                    result.AddRange(LadderRow.AddMOVSet("K" + each.ErrorNum.ToString(), SettingsManager.Settings.ErrorDevice));
                }
                else
                {
                    result.Add(LadderRow.AddOUT(SettingsManager.Settings.OutErrorDevice));
                    AddError($"エラーのデバイスが null または空です: '{operation.OperationName}'",
                        operation.OperationName ?? "", 3, 0);
                }
            }
            return result;
        }

        private void AddError(
            string message,
            string detailName,
            int mnemonicId,
            int processId)
        {
            _errorAggregator.AddError(new OutputError
            {
                Message = message,
                RecordName = detailName,
                MnemonicId = mnemonicId,
                RecordId = processId
            });
        }
    }
}
