using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Interfaces;

namespace KdxDesigner.Utils.Operation
{
    internal class ProsTimeBuilder
    {
        private readonly IErrorAggregator _errorAggregator;
        public ProsTimeBuilder(IErrorAggregator errorAggregator)
        {
            _errorAggregator = errorAggregator;
        }

        public List<LadderCsvRow> Common(
            Kdx.Contracts.DTOs.Operation operation,
            List<ProsTime> prosTimes,
            string label,
            int outNum)
        {
            List<LadderCsvRow>? result = new();
            var prosTimeList = prosTimes.Where(p => p.RecordId == operation.Id).OrderBy(p => p.SortId).ToList();

            // カウント信号の追加
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(label + (outNum + 5).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.Clock01));
            result.Add(LadderRow.AddPLS(label + (outNum + 3).ToString()));

            foreach (var pros in prosTimeList)
            {
                if (string.IsNullOrEmpty(pros.CurrentDevice) || string.IsNullOrEmpty(pros.PreviousDevice)) continue;

                switch (pros.CategoryId)
                {
                    case 6: // 工程
                        result.Add(LadderRow.AddLD(label + (outNum + 5).ToString()));
                        result.AddRange(LadderRow.AddMOVPSet(SettingsManager.Settings.CycleTime, pros.CurrentDevice));
                        break;
                    default: // 出力可、開始、出力停止、完了 
                        result.Add(LadderRow.AddLD(label + (outNum + 3).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + pros.CategoryId - 1).ToString()));
                        result.Add(LadderRow.AddINC(pros.CurrentDevice));
                        break;
                }
            }

            var current = prosTimeList[0].CurrentDevice;
            var previous = prosTimeList[0].PreviousDevice;
            var count = "K" + prosTimeList.Count.ToString();

            // リセット信号の追加
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(previous)) return result;
            result.Add(LadderRow.AddLDP(label + (outNum + 19).ToString()));
            result.AddRange(LadderRow.AddBMOVSet(current, previous, count));
            result.AddRange(LadderRow.AddFMOVSet("K0", current, count));

            // CYタイム 全体
            var cylinderDevice = prosTimeList.SingleOrDefault(p => p.SortId == 0)?.CylinderDevice ?? "";
            var cylinderDeviceSecond = prosTimeList.SingleOrDefault(p => p.SortId == 1)?.CylinderDevice ?? "";

            var finalProsDevice = prosTimeList.OrderByDescending(p => p.SortId).FirstOrDefault()?.PreviousDevice ?? "";
            var firstProsDevice = prosTimeList.Where(p => p.SortId == 1).FirstOrDefault()!.PreviousDevice ?? "";


            result.Add(LadderRow.AddLDP(label + (outNum + 19).ToString()));
            result.AddRange(LadderRow.AddMOVPSet(
                finalProsDevice,
                cylinderDevice));

            result.Add(LadderRow.AddLDP(label + (outNum + 19).ToString()));
            result.AddRange(LadderRow.AddMOVPSet(
                firstProsDevice,
                cylinderDeviceSecond));

            foreach (var pros in prosTimeList)
            {
                if (pros.SortId == 0 || pros.SortId == 1) continue; // SortId 0は全体のCYタイムなのでスキップ
                var previousDevice1 = pros.PreviousDevice;
                var previousDevice2 = prosTimeList.SingleOrDefault(p => p.SortId == pros.SortId - 1)?.PreviousDevice ?? "";
                var cylinderDeviceSort = prosTimeList.SingleOrDefault(p => p.SortId == pros.SortId)?.CylinderDevice ?? "";

                result.AddRange(LadderRow.AddSUBP(
                previousDevice1,
                previousDevice2,
                cylinderDeviceSort));
            }

            return result;
        }
    }
}
