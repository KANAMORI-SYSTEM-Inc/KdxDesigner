using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Interfaces;
using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils.Process
{
    /// <summary>
    /// Processのラダー出力を生成するクラス
    /// ProcessStartConditionとProcessFinishConditionテーブルを使用する
    /// </summary>
    internal class BuildProcess
    {
        /// <summary>
        /// 開始条件を取得する（新しい中間テーブルを使用）
        /// </summary>
        private static List<int> GetStartConditions(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process)
        {
            // 新しい中間テーブルから開始条件を取得
            var startConditions = repository.GetStartConditionsByProcessId(process.Process.Id);

            return startConditions
                .Select(sc => sc.StartProcessDetailId)
                .ToList();
        }

        /// <summary>
        /// 終了条件を取得する（新しい中間テーブルを使用）
        /// </summary>
        private static List<int> GetFinishConditions(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process)
        {
            // 新しい中間テーブルから終了条件を取得
            var finishConditions = repository.GetFinishConditionsByProcessId(process.Process.Id);

            return finishConditions
                .Select(fc => fc.FinishProcessDetailId)
                .ToList();
        }

        /// <summary>
        /// BuildNormalメソッド
        /// </summary>
        public static List<LadderCsvRow> BuildNormal(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            string id = process.Process.Id.ToString();
            if (string.IsNullOrEmpty(process.Process.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + process.Process.ProcessName));
            }

            // 開始条件を新しい方法で取得
            List<int> startCondition = GetStartConditions(repository, process);

            if (startCondition.Count == 0)
            {
                // 開始条件が無い場合は、ProcessDetailのIDを取得する
                result.Add(LadderRow.AddLD(process.Process.AutoStart ?? string.Empty));
            }
            else
            {
                // 開始条件がある場合
                bool first = true;
                foreach (var detailId in startCondition)
                {
                    // 1. IDから対象のProcessDetailのレコードを取得する。
                    var target = detail.FirstOrDefault(d => d.Detail.Id == detailId);

                    if (target == null)
                    {
                        // エラー処理を追加してください issue#13
                        continue;
                    }

                    // 2. ProcessDetailのレコードから、完了のアウトコイルの数値を取得する。
                    var mnemonic = target.Mnemonic;

                    // 3. ラベルと数値を取得して結合する。
                    var label = mnemonic.DeviceLabel ?? string.Empty;
                    var deviceNumber = mnemonic.StartNum + mnemonic.OutCoilCount - 1;
                    var device = deviceNumber.ToString();

                    var labelDevice = label + device;

                    // 4. 命令を生成する
                    var row = first
                        ? LadderRow.AddLD(labelDevice)
                        : LadderRow.AddAND(labelDevice);

                    result.Add(row);
                    first = false;
                }
            }

            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));

            // OUT L1 開始
            // 試運転スル
            var debugContact = process.Process.TestMode;
            var debugStartContact = process.Process.TestStart;
            var debugCondition = process.Process.TestCondition;
            var startContact = process.Process.AutoStart;

            if (debugContact == null
                || debugStartContact == null
                || debugCondition == null
                || startContact == null)
            {
                // エラーを追加してください   issue#10
            }
            else
            {
                result.Add(LadderRow.AddLDI(debugContact));

                // 試運転実行処理
                result.Add(LadderRow.AddLD(debugStartContact));
                result.Add(LadderRow.AddAND(debugCondition));
                result.Add(LadderRow.AddAND(debugContact));
                result.Add(LadderRow.AddORB());
                result.Add(LadderRow.AddAND(outcoilLabel + outcoilNum.ToString()));
                result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddOR(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddAND(startContact));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 1).ToString()));

                // CJの実装
                result.Add(LadderRow.AddLDP(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddAND(debugStartContact));
                result.Add(LadderRow.AddCJ($"P{process.Process.CycleId}"));

                // OUT L2 実行中
                result.Add(LadderRow.AddLD(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 4).ToString()));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 2).ToString()));
            }

            // OUT L4 完了
            // 終了条件を新しい方法で取得
            List<int> finishConditions = GetFinishConditions(repository, process);

            if (finishConditions.Any())
            {
                var completeContact = finishConditions.First(); // 最初の終了条件を使用
                var completeDetailRecord = detail.FirstOrDefault(d => d.Mnemonic.RecordId == completeContact);

                if (completeDetailRecord != null)
                {
                    var completeMnemonic = completeDetailRecord.Mnemonic;
                    var completeLabel = completeMnemonic.DeviceLabel ?? string.Empty;
                    var completeNumber = completeMnemonic.StartNum + completeMnemonic.OutCoilCount - 1;
                    var completeDevice = completeNumber.ToString();
                    var completeLabelDevice = completeLabel + completeDevice;

                    result.Add(LadderRow.AddLD(outcoilLabel + (outcoilNum + 1).ToString()));
                    result.Add(LadderRow.AddAND(completeLabelDevice));
                    result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 4).ToString()));
                }
            }

            return result;
        }

        /// <summary>
        /// BuildResetAfterメソッド
        /// </summary>
        public static List<LadderCsvRow> BuildResetAfter(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            string id = process.Process.Id.ToString();
            if (string.IsNullOrEmpty(process.Process.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + process.Process.ProcessName));
            }

            // 開始条件を新しい方法で取得
            List<int> startCondition = GetStartConditions(repository, process);

            if (startCondition.Count == 0)
            {
                result.Add(LadderRow.AddLD(process.Process.AutoStart ?? string.Empty));
            }
            else
            {
                bool first = true;
                foreach (var detailId in startCondition)
                {
                    var target = detail.FirstOrDefault(d => d.Detail.Id == detailId);
                    if (target == null) continue;

                    var mnemonic = target.Mnemonic;
                    var label = mnemonic.DeviceLabel ?? string.Empty;
                    var deviceNumber = mnemonic.StartNum + mnemonic.OutCoilCount - 1;
                    var device = deviceNumber.ToString();
                    var labelDevice = label + device;

                    var row = first
                        ? LadderRow.AddLD(labelDevice)
                        : LadderRow.AddAND(labelDevice);

                    result.Add(row);
                    first = false;
                }
            }

            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));

            // SET
            result.Add(LadderRow.AddLDF(outcoilLabel + outcoilNum.ToString()));
            result.Add(LadderRow.AddSET(outcoilLabel + (outcoilNum + 3).ToString()));

            // OUT L1 開始
            // 試運転スル
            var debugContact = process.Process.TestMode;
            var debugStartContact = process.Process.TestStart;
            var startContact = process.Process.AutoStart;

            if (debugContact == null
                || debugStartContact == null
                || startContact == null)
            {
                // エラーを追加してください   issue#10
            }
            else
            {
                result.Add(LadderRow.AddLDI(debugContact));

                // 試運転実行処理
                result.Add(LadderRow.AddLD(debugStartContact));
                result.Add(LadderRow.AddAND(debugContact));
                result.Add(LadderRow.AddORB());

                // アウトコイルまで
                result.Add(LadderRow.AddAND(outcoilLabel + outcoilNum.ToString()));
                result.Add(LadderRow.AddAND(outcoilLabel + (outcoilNum + 3).ToString()));
                result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddOR(outcoilLabel + (outcoilNum + 1).ToString()));

                result.Add(LadderRow.AddAND(startContact));
                result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 4).ToString()));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 1).ToString()));

                // CJの実装
                result.Add(LadderRow.AddLDP(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddAND(debugStartContact));
                result.Add(LadderRow.AddCJ($"P{process.Process.CycleId}"));
            }

            // OUT L2 実行中
            // 終了条件を新しい方法で取得
            List<int> finishConditions = GetFinishConditions(repository, process);

            if (finishConditions.Any())
            {
                var completeContact = finishConditions.First(); // 最初の終了条件を使用
                var completeDetailRecord = detail.FirstOrDefault(d => d.Mnemonic.RecordId == completeContact);

                if (completeDetailRecord != null)
                {
                    var completeMnemonic = completeDetailRecord.Mnemonic;
                    var completeLabel = completeMnemonic.DeviceLabel ?? string.Empty;
                    var completeNumber = completeMnemonic.StartNum + completeMnemonic.OutCoilCount - 1;
                    var completeDevice = completeNumber.ToString();
                    var completeLabelDevice = completeLabel + completeDevice;

                    result.Add(LadderRow.AddLD(outcoilLabel + (outcoilNum + 1).ToString()));
                    result.Add(LadderRow.AddAND(completeLabelDevice));
                    result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 4).ToString()));
                }
            }

            // RST
            result.Add(LadderRow.AddLDP(outcoilLabel + (outcoilNum + 4).ToString()));
            result.Add(LadderRow.AddRST(outcoilLabel + (outcoilNum + 2).ToString()));

            return result;
        }

        /// <summary>
        /// BuildSubProcessメソッド
        /// </summary>
        public static List<LadderCsvRow> BuildSubProcess(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            // 開始条件を新しい方法で取得
            List<int> startCondition = GetStartConditions(repository, process);

            if (startCondition.Count == 0)
            {
                result.Add(LadderRow.AddLD(process.Process.AutoStart ?? string.Empty));
            }
            else
            {
                bool first = true;
                foreach (var detailId in startCondition)
                {
                    var target = detail.FirstOrDefault(d => d.Detail.Id == detailId);
                    if (target == null) continue;

                    var mnemonic = target.Mnemonic;
                    var label = mnemonic.DeviceLabel ?? string.Empty;
                    var deviceNumber = mnemonic.StartNum + mnemonic.OutCoilCount - 1;
                    var device = deviceNumber.ToString();
                    var labelDevice = label + device;

                    var row = first
                        ? LadderRow.AddLD(labelDevice)
                        : LadderRow.AddAND(labelDevice);

                    result.Add(row);
                    first = false;
                }
            }

            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));

            // 自己保持とRST追加
            result.Add(LadderRow.AddLD(outcoilLabel + outcoilNum.ToString()));
            result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 1).ToString()));

            result.Add(LadderRow.AddLDF(outcoilLabel + (outcoilNum + 1).ToString()));
            result.Add(LadderRow.AddRST(outcoilLabel + outcoilNum.ToString()));

            return result;
        }

        /// <summary>
        /// BuildConditionメソッド
        /// </summary>
        public static List<LadderCsvRow> BuildCondition(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            // 開始条件を新しい方法で取得
            List<int> startCondition = GetStartConditions(repository, process);

            if (startCondition.Count == 0)
            {
                result.Add(LadderRow.AddLD(process.Process.AutoStart ?? string.Empty));
            }
            else
            {
                bool first = true;
                foreach (var detailId in startCondition)
                {
                    var target = detail.FirstOrDefault(d => d.Detail.Id == detailId);
                    if (target == null) continue;

                    var mnemonic = target.Mnemonic;
                    var label = mnemonic.DeviceLabel ?? string.Empty;
                    var deviceNumber = mnemonic.StartNum + mnemonic.OutCoilCount - 1;
                    var device = deviceNumber.ToString();
                    var labelDevice = label + device;

                    var row = first
                        ? LadderRow.AddLD(labelDevice)
                        : LadderRow.AddAND(labelDevice);

                    result.Add(row);
                    first = false;
                }
            }

            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));

            // 分岐処理の実装
            if (!string.IsNullOrEmpty(process.Process.ProcessName))
            {
                result.Add(LadderRow.AddLDF(outcoilLabel + outcoilNum.ToString()));
                result.Add(LadderRow.AddCJ($"P{process.Process.CycleId}"));
            }

            return result;
        }

        /// <summary>
        /// BuildConditionStartメソッド
        /// </summary>
        public static List<LadderCsvRow> BuildConditionStart(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            // 開始条件を新しい方法で取得
            List<int> startCondition = GetStartConditions(repository, process);

            if (startCondition.Count == 0)
            {
                result.Add(LadderRow.AddLD(process.Process.AutoStart ?? string.Empty));
            }
            else
            {
                bool first = true;
                foreach (var detailId in startCondition)
                {
                    var target = detail.FirstOrDefault(d => d.Detail.Id == detailId);
                    if (target == null) continue;

                    var mnemonic = target.Mnemonic;
                    var label = mnemonic.DeviceLabel ?? string.Empty;
                    var deviceNumber = mnemonic.StartNum + mnemonic.OutCoilCount - 1;
                    var device = deviceNumber.ToString();
                    var labelDevice = label + device;

                    var row = first
                        ? LadderRow.AddLD(labelDevice)
                        : LadderRow.AddAND(labelDevice);

                    result.Add(row);
                    first = false;
                }
            }

            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));

            // 自己保持
            result.Add(LadderRow.AddLD(outcoilLabel + outcoilNum.ToString()));
            result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 1).ToString()));

            return result;
        }

        // BuildIL
        public static List<LadderCsvRow> BuildIL(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            string id = process.Process.Id.ToString();
            if (string.IsNullOrEmpty(process.Process.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + process.Process.ProcessName));
            }

            // 開始条件を新しい方法で取得
            List<int> startCondition = GetStartConditions(repository, process);

            if (startCondition.Count == 0)
            {
                result.Add(LadderRow.AddLD(process.Process.AutoStart ?? string.Empty));
            }
            else
            {
                bool first = true;
                foreach (var detailId in startCondition)
                {
                    var target = detail.FirstOrDefault(d => d.Detail.Id == detailId);
                    if (target == null) continue;

                    var mnemonic = target.Mnemonic;
                    var label = mnemonic.DeviceLabel ?? string.Empty;
                    var deviceNumber = mnemonic.StartNum + mnemonic.OutCoilCount - 1;
                    var device = deviceNumber.ToString();
                    var labelDevice = label + device;

                    var row = first
                        ? LadderRow.AddLD(labelDevice)
                        : LadderRow.AddAND(labelDevice);

                    result.Add(row);
                    first = false;
                }
            }

            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));

            // OUT L1 開始
            // 試運転スル
            var debugContact = process.Process.TestMode;
            var debugStartContact = process.Process.TestStart;
            var startContact = process.Process.AutoStart;
            var iLStart = process.Process.ILStart;

            if (debugContact == null
                || debugStartContact == null
                || startContact == null
                || iLStart == null)
            {
                // エラーを追加してください   issue#10
            }
            else
            {
                result.Add(LadderRow.AddLDI(debugContact));

                // 試運転実行処理
                result.Add(LadderRow.AddLD(debugStartContact));

                result.Add(LadderRow.AddAND(debugContact));
                result.Add(LadderRow.AddORB());

                // アウトコイルまで
                result.Add(LadderRow.AddAND(outcoilLabel + outcoilNum.ToString()));
                result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddOR(outcoilLabel + (outcoilNum + 1).ToString()));

                result.Add(LadderRow.AddAND(startContact));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 1).ToString()));

                // OUT L2 開始
                result.Add(LadderRow.AddLD(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 4).ToString()));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 2).ToString()));

                // OUT L3 開始確認
                result.Add(LadderRow.AddLD(iLStart));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 3).ToString()));

                // OUT L4 完了
                result.Add(LadderRow.AddLD(outcoilLabel + (outcoilNum + 3).ToString()));
                result.Add(LadderRow.AddOR(outcoilLabel + (outcoilNum + 4).ToString()));
                result.Add(LadderRow.AddAND(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 4).ToString()));
            }
            return result;
        }

        // BuildReset
        public static List<LadderCsvRow> BuildReset(
            IAccessRepository repository,
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            string id = process.Process.Id.ToString();
            if (string.IsNullOrEmpty(process.Process.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + process.Process.ProcessName));
            }

            // 開始条件を新しい方法で取得
            List<int> startCondition = GetStartConditions(repository, process);

            if (startCondition.Count == 0)
            {
                result.Add(LadderRow.AddLD(process.Process.AutoStart ?? string.Empty));
            }
            else
            {
                bool first = true;
                foreach (var detailId in startCondition)
                {
                    var target = detail.FirstOrDefault(d => d.Detail.Id == detailId);
                    if (target == null) continue;

                    var mnemonic = target.Mnemonic;
                    var label = mnemonic.DeviceLabel ?? string.Empty;
                    var deviceNumber = mnemonic.StartNum + mnemonic.OutCoilCount - 1;
                    var device = deviceNumber.ToString();
                    var labelDevice = label + device;

                    var row = first
                        ? LadderRow.AddLD(labelDevice)
                        : LadderRow.AddAND(labelDevice);

                    result.Add(row);
                    first = false;
                }
            }

            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));

            // OUT L1 開始
            // 試運転スル
            var debugContact = process.Process.TestMode;
            var debugStartContact = process.Process.TestStart;
            var startContact = process.Process.AutoStart;
            var iLStart = process.Process.ILStart;

            if (debugContact == null
                || debugStartContact == null
                || startContact == null
                || iLStart == null)
            {
                // エラーを追加してください   issue#10
            }
            else
            {
                result.Add(LadderRow.AddLDI(debugContact));

                // 試運転実行処理
                result.Add(LadderRow.AddLD(debugStartContact));

                result.Add(LadderRow.AddAND(debugContact));
                result.Add(LadderRow.AddORB());

                // アウトコイルまで
                result.Add(LadderRow.AddAND(outcoilLabel + outcoilNum.ToString()));
                result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddOR(outcoilLabel + (outcoilNum + 1).ToString()));

                result.Add(LadderRow.AddAND(startContact));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 1).ToString()));

                // OUT L4 完了
                result.Add(LadderRow.AddLD(outcoilLabel + (outcoilNum + 1).ToString()));
                result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 4).ToString()));
                if (!string.IsNullOrEmpty(process.Process.ILStart))
                {
                    result.Add(LadderRow.AddOUT(process.Process.ILStart));
                }
                else
                {
                    // エラーを追加してください   issue#10
                }
            }
            return result;
        }

        // BuildSubRoutine
        public static List<LadderCsvRow> BuildSubRoutine(
            List<MnemonicDeviceWithProcess> process)
        {
            var result = new List<LadderCsvRow>();
            result.Add(LadderRow.AddStatement("SubRoutine"));

            //// TestStart を重複なしで取得
            //List<string> testStartList = process
            //    .Where(p => p.Process.TestStart != null) // null除外
            //    .Select(p => p.Process.TestStart!)       // stringに変換
            //    .Distinct()                              // 重複排除
            //    .ToList();

            //result.Add(LadderRow.AddFEND()); // FEND命令を追加
            //result.Add(LadderRow.AddPoint($"P{process.First().Process.CycleId}"));          // issue#11


            //result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysON));
            //foreach(var testStart in testStartList)
            //{
            //    // TestStartごとにLDP命令を追加
            //    result.Add(LadderRow.AddRST(testStart));

            //}
            //result.Add(LadderRow.AddRET()); // FEND命令を追加

            return result;
        }
    }
}
