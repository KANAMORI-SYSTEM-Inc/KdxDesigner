using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Utils.Process
{
    /// <summary>
    /// Processのラダー出力を生成する基底クラス
    /// ProcessStartConditionとProcessFinishConditionテーブルを使用する
    /// </summary>
    internal class IBuildProcess
    {
        protected readonly ISupabaseRepository _repository;
        protected readonly List<MnemonicDeviceWithProcessDetail> _details;

        /// <summary>
        /// IBuildProcess のインスタンスを初期化します
        /// </summary>
        /// <param name="repository">Supabaseリポジトリ</param>
        /// <param name="details">工程詳細のリスト</param>
        public IBuildProcess(
            ISupabaseRepository repository,
            List<MnemonicDeviceWithProcessDetail> details)
        {
            _repository = repository;
            _details = details;
        }

        /// <summary>
        /// 開始条件を取得する（新しい中間テーブルを使用）
        /// </summary>
        protected async Task<List<int>> GetStartConditions(MnemonicDeviceWithProcess process)
        {
            // 新しい中間テーブルから開始条件を取得
            var startConditions = await _repository.GetStartConditionsByProcessIdAsync(process.Process.Id);

            return startConditions
                .Select(sc => sc.StartProcessDetailId)
                .ToList();
        }

        /// <summary>
        /// 終了条件を取得する（新しい中間テーブルを使用）
        /// </summary>
        protected async Task<List<int>> GetFinishConditions(MnemonicDeviceWithProcess process)
        {
            // 新しい中間テーブルから終了条件を取得
            var finishConditions = await _repository.GetFinishConditionsByProcessIdAsync(process.Process.Id);

            return finishConditions
                .Select(fc => fc.FinishProcessDetailId)
                .ToList();
        }

        /// <summary>
        /// 行間ステートメントを生成する共通メソッド
        /// </summary>
        protected LadderCsvRow CreateStatement(MnemonicDeviceWithProcess process)
        {
            string id = process.Process.Id.ToString();
            if (string.IsNullOrEmpty(process.Process.ProcessName))
            {
                return LadderRow.AddStatement(id);
            }
            else
            {
                return LadderRow.AddStatement(id + ":" + process.Process.ProcessName);
            }
        }

        /// <summary>
        /// 開始条件のLD/AND行を追加する共通メソッド
        /// </summary>
        protected async Task AddStartConditionRows(
            List<LadderCsvRow> result,
            MnemonicDeviceWithProcess process)
        {
            List<int> startCondition = await GetStartConditions(process);

            if (startCondition.Count == 0)
            {
                result.Add(LadderRow.AddLD(process.Process.AutoStart ?? string.Empty));
            }
            else
            {
                bool first = true;
                foreach (var detailId in startCondition)
                {
                    var target = _details.FirstOrDefault(d => d.Detail.Id == detailId);
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
        }

        /// <summary>
        /// 通常工程のビルド
        /// </summary>
        public virtual async Task<List<LadderCsvRow>> BuildNormal(MnemonicDeviceWithProcess process)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            result.Add(CreateStatement(process));

            // 開始条件を追加
            await AddStartConditionRows(result, process);

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
            List<int> finishConditions = await GetFinishConditions(process);

            if (finishConditions.Any())
            {
                var completeContact = finishConditions.First(); // 最初の終了条件を使用
                var completeDetailRecord = _details.FirstOrDefault(d => d.Mnemonic.RecordId == completeContact);

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
        /// リセット後工程のビルド
        /// </summary>
        public virtual async Task<List<LadderCsvRow>> BuildResetAfter(MnemonicDeviceWithProcess process)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            result.Add(CreateStatement(process));

            // 開始条件を追加
            await AddStartConditionRows(result, process);

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
            List<int> finishConditions = await GetFinishConditions(process);

            if (finishConditions.Any())
            {
                var completeContact = finishConditions.First(); // 最初の終了条件を使用
                var completeDetailRecord = _details.FirstOrDefault(d => d.Mnemonic.RecordId == completeContact);

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
        /// サブプロセスのビルド
        /// </summary>
        public virtual async Task<List<LadderCsvRow>> BuildSubProcess(MnemonicDeviceWithProcess process)
        {
            var result = new List<LadderCsvRow>();

            // 開始条件を追加
            await AddStartConditionRows(result, process);

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
        /// 条件分岐のビルド
        /// </summary>
        public virtual async Task<List<LadderCsvRow>> BuildCondition(MnemonicDeviceWithProcess process)
        {
            var result = new List<LadderCsvRow>();

            // 開始条件を追加
            await AddStartConditionRows(result, process);

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
        /// 条件開始のビルド
        /// </summary>
        public virtual async Task<List<LadderCsvRow>> BuildConditionStart(MnemonicDeviceWithProcess process)
        {
            var result = new List<LadderCsvRow>();

            // 開始条件を追加
            await AddStartConditionRows(result, process);

            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));

            // 自己保持
            result.Add(LadderRow.AddLD(outcoilLabel + outcoilNum.ToString()));
            result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 1).ToString()));

            return result;
        }

        /// <summary>
        /// IL待ちのビルド
        /// </summary>
        public virtual async Task<List<LadderCsvRow>> BuildIL(MnemonicDeviceWithProcess process)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            result.Add(CreateStatement(process));

            // 開始条件を追加
            await AddStartConditionRows(result, process);

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

        /// <summary>
        /// リセットのビルド
        /// </summary>
        public virtual async Task<List<LadderCsvRow>> BuildReset(MnemonicDeviceWithProcess process)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            result.Add(CreateStatement(process));

            // 開始条件を追加
            await AddStartConditionRows(result, process);

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

        /// <summary>
        /// サブルーチンのビルド
        /// </summary>
        public virtual List<LadderCsvRow> BuildSubRoutine(List<MnemonicDeviceWithProcess> process)
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
