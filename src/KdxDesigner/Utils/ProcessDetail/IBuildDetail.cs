using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.ProcessDetail
{
    /// <summary>
    /// ProcessDetail固有のビルドロジックを提供する基底クラス
    /// </summary>
    internal class IBuildDetail
    {
        // --- サービスのフィールド ---
        protected readonly MainViewModel _mainViewModel;
        protected readonly IIOAddressService _ioAddressService;
        protected readonly IErrorAggregator _errorAggregator;
        protected readonly ISupabaseRepository _repository;

        // --- データセットのフィールド ---
        protected readonly List<MnemonicDeviceWithProcess> _processes;
        protected readonly List<MnemonicDeviceWithProcessDetail> _details;
        protected readonly List<MnemonicDeviceWithOperation> _operations;
        protected readonly List<MnemonicDeviceWithCylinder> _cylinders;
        protected readonly List<IO> _ioList;

        /// <summary>
        /// IBuildDetail のインスタンスを初期化します
        /// </summary>
        /// <param name="mainViewModel">MainViewからの初期値</param>
        /// <param name="ioAddressService">IO検索用のサービス</param>
        /// <param name="errorAggregator">エラー出力用のサービス</param>
        /// <param name="repository">ACCESSファイル検索用のリポジトリ</param>
        /// <param name="processes">工程の一覧</param>
        /// <param name="details">工程詳細の一覧</param>
        /// <param name="operations">操作の一覧</param>
        /// <param name="cylinders">CYの一覧</param>
        /// <param name="ioList">IOの一覧</param>
        public IBuildDetail(
            MainViewModel mainViewModel,
            IIOAddressService ioAddressService,
            IErrorAggregator errorAggregator,
            ISupabaseRepository repository,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            _mainViewModel = mainViewModel;
            _ioAddressService = ioAddressService;
            _errorAggregator = errorAggregator;
            _repository = repository;
            _processes = processes;
            _details = details;
            _operations = operations;
            _cylinders = cylinders;
            _ioList = ioList;
        }

        /// <summary>
        /// 工程詳細：通常工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> Normal(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;
            var process = GetProcessForDetail(detail);

            result.Add(CreateStatement(detail, "通常工程"));

            var timer = await GetTimerForOperation(detail);

            // L0 工程開始
            result.AddRange(await L0(detail, process, timer));

            // L1 工程開始
            var operationFinish = _operations.FirstOrDefault(o => o.Mnemonic.RecordId == detail.Detail.OperationId);
            var opFinishNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var opFinishLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(opFinishLabel + (opFinishNum + 19).ToString()));

            if (!string.IsNullOrEmpty(detail.Detail.SkipMode))
            {
                if (detail.Detail.SkipMode.Contains("_"))
                {
                    var skipDevice = detail.Detail.SkipMode.Replace("_", string.Empty);
                    result.Add(LadderRow.AddORI(skipDevice));
                }
                else
                {
                    var skipDevice = detail.Detail.SkipMode;
                    result.Add(LadderRow.AddOR(skipDevice));
                }
            }

            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(L4(detail));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：工程まとめのビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> Summarize(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "工程まとめ"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(L4(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：センサON確認のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> SensorON(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "センサON確認"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作開始
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (!string.IsNullOrEmpty(detail.Detail.FinishSensor))
            {
                var ioSensor = GetSensorIO(detail.Detail.FinishSensor, detail.Detail.DetailName, detail.Detail.Id);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    var isNegated = detail.Detail.FinishSensor.StartsWith("_", StringComparison.Ordinal);
                    result.Add(isNegated
                        ? LadderRow.AddLDI(ioSensor)
                        : LadderRow.AddLD(ioSensor));
                }
                result.Add(LadderRow.AddOR(SettingsManager.Settings.DebugTest));

                // skipModeが設定されている場合は、スキップ処理を追加
                AddSkipModeRows(result, detail);

                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(L4(detail));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：センサOFFのビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> SensorOFF(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "センサOFF確認"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作開始
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (!string.IsNullOrEmpty(detail.Detail.FinishSensor))
            {
                var ioSensor = GetSensorIO(detail.Detail.FinishSensor, detail.Detail.DetailName, detail.Detail.Id);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    // OFF工程 → 常にLDI接点
                    result.Add(LadderRow.AddLDI(ioSensor));
                }
                result.Add(LadderRow.AddOR(SettingsManager.Settings.DebugTest));

                // skipModeが設定されている場合は、スキップ処理を追加
                AddSkipModeRows(result, detail);

                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(L4(detail));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：工程分岐のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> Branch(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "工程分岐"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作開始
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (!string.IsNullOrEmpty(detail.Detail.FinishSensor))
            {
                var ioSensor = GetSensorIO(detail.Detail.FinishSensor, detail.Detail.DetailName, detail.Detail.Id);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    var isNegated = detail.Detail.FinishSensor.StartsWith("_", StringComparison.Ordinal);
                    result.Add(isNegated
                        ? LadderRow.AddLDI(ioSensor)
                        : LadderRow.AddLD(ioSensor));
                }
            }
            else
            {
                // FinishSensorの設定ナシ
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                DetailError(detail, "FinishSensor が設定されていません。");
            }

            result.Add(LadderRow.AddANI(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(L4(detail));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：工程合流のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> Merge(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "工程合流"));

            // L0 の初期値を設定
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = _processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessDetailの開始条件を取得（中間テーブルから）
            var processDetailStartIds = new List<int>();

            // 中間テーブルから取得
            var connections = await _repository.GetConnectionsByToIdAsync(detail.Detail.Id);
            processDetailStartIds.AddRange(connections.Select(c => c.FromProcessDetailId));

            var processDetailStartDevices = _details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            // L0 工程開始
            // 設定値を使う場合の構文 SettingsManager.Settings.""
            // 設定値の初期値は\Model\AppSettings.csに定義

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 0).ToString()));

            // 初回はLD命令
            var first = true;

            foreach (var d in processDetailStartDevices)
            {
                var row = first
                    ? LadderRow.AddLD(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 4).ToString())
                    : LadderRow.AddOR(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 4).ToString());

                result.Add(row);
                first = false;
            }
            result.Add(LadderRow.AddANB());
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));

            // L4 工程完了
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 4).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 4).ToString()));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：IL待ち工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> ILWait(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "IL待ち"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作開始
            if (!string.IsNullOrEmpty(detail.Detail.FinishSensor))
            {
                // FinishSensorが設定されている場合
                result.Add(LadderRow.AddLD(detail.Detail.FinishSensor));
            }
            else
            {
                // FinishSensornの設定ナシ
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                DetailError(detail, "FinishSensor が設定されていません。");
            }
            result.Add(LadderRow.AddOR(SettingsManager.Settings.DebugTest));

            // skipModeが設定されている場合は、スキップ処理を追加
            AddSkipModeRows(result, detail);

            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            if (!string.IsNullOrEmpty(detail.Detail.StartSensor))
            {
                result.Add(LadderRow.AddLD(label + (outNum + 0).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 1).ToString()));
                result.Add(LadderRow.AddOUT(detail.Detail.StartSensor));
            }

            // L4 工程完了
            result.AddRange(L4(detail));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：工程OFF確認のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> ProcessOFF(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "工程OFF確認"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作開始
            // ProcessDetailFinishテーブルから終了工程IDを取得
            var finishes = await _repository.GetFinishesByProcessDetailIdAsync(detail.Detail.Id);
            var detailOffIds = finishes.Select(f => f.FinishProcessDetailId).ToList();

            if (detailOffIds.Count != 1)
            {
                DetailError(detail, "工程OFF確認に終了工程が複数設定されています。");
                return result; // エラーがある場合は、空のリストを返す
            }

            if (detailOffIds.Count == 0)
            {
                DetailError(detail, "工程OFF確認に終了工程が設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            var detailOffDevice = detailOffIds[0];
            var detailOffDeviceMnemonic = _details
                .FirstOrDefault(d => d.Mnemonic.RecordId == detailOffDevice);

            if (detailOffDeviceMnemonic == null)
            {
                DetailError(detail, $"工程OFF確認の終了工程に設定されているデバイスが見つかりません。ID: {detailOffDevice}");
                return result; // エラーがある場合は、空のリストを返す
            }

            result.Add(LadderRow.AddLD(detailOffDeviceMnemonic.Mnemonic.DeviceLabel
                + detailOffDeviceMnemonic.Mnemonic.StartNum.ToString()));

            // skipModeが設定されている場合は、スキップ処理を追加
            AddSkipModeRows(result, detail);

            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 4).ToString()));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：リセット工程開始のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> ResetAfterStart(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "リセット工程開始"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作開始
            var processDetailFinishDevices = await FinishProcessDevices(detail);

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));

            // FinishSensorが設定されていない場合
            // 終了工程のStartNum+5を出力
            bool isFirst = true;
            foreach (var d in processDetailFinishDevices)
            {
                if (isFirst)
                {
                    result.Add(LadderRow.AddLDI(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 4).ToString()));
                    isFirst = false;
                }
                else
                {
                    result.Add(LadderRow.AddOR(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 4).ToString()));
                }
            }

            result.Add(LadderRow.AddANB());
            result.Add(LadderRow.AddANI(SettingsManager.Settings.SoftResetSignal));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L4 工程完了
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 4).ToString()));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：期間工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> Season(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "期間工程"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L2 操作停止
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (!string.IsNullOrEmpty(detail.Detail.FinishSensor))
            {
                var ioSensor = GetSensorIO(detail.Detail.FinishSensor, detail.Detail.DetailName, detail.Detail.Id);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    var isNegated = detail.Detail.FinishSensor.StartsWith("_", StringComparison.Ordinal);
                    result.Add(isNegated
                        ? LadderRow.AddLDI(ioSensor)
                        : LadderRow.AddLD(ioSensor));
                }
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            var processDetailFinishDevices = await FinishProcessDevices(detail);
            if (!string.IsNullOrEmpty(detail.Detail.FinishSensor))
            {
                // FinishSensorが設定されている場合
                // 終了工程のStartNum+1を出力
                foreach (var d in processDetailFinishDevices)
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 1).ToString()));
                }
            }
            else
            {
                // FinishSensorが設定されていない場合
                // 終了工程のStartNum+5を出力
                foreach (var d in processDetailFinishDevices)
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 4).ToString()));
                }
            }

            // skipModeが設定されている場合は、スキップ処理を追加
            AddSkipModeRows(result, detail);

            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 2).ToString()));

            // L4 工程完了
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 4).ToString()));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：タイマ工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <param name="detailTimers">タイマの詳細リスト</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> TimerProcess(MnemonicDeviceWithProcessDetail detail, List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "タイマ工程"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            var timers = detailTimers.Where(t => t.Timer.RecordId == detail.Detail.Id);

            if (timers != null)
            {
                result.Add(LadderRow.AddLDP(label + (outNum + 0).ToString()));

                foreach (var timer in timers)
                {
                    // タイマの開始デバイスを取得
                    result.Add(LadderRow.AddRST(timer.Timer.TimerDeviceT));
                }
            }
            else
            {
                DetailError(detail, "タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L2 タイマ開始
            var stopTimer = detailTimers.FirstOrDefault(t => t.Timer.RecordId == detail.Detail.Id);

            if (stopTimer == null)
            {
                DetailError(detail, "タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 2).ToString()));
            result.AddRange(LadderRow.AddTimer(stopTimer.Timer.TimerDeviceT, stopTimer.Timer.TimerDeviceZR));
            result.Add(LadderRow.AddLD(stopTimer.Timer.TimerDeviceT));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 2).ToString()));

            // L4 工程完了
            result.AddRange(L4(detail));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 工程詳細：タイマのビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <param name="detailTimers">タイマの詳細リスト</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> Timer(MnemonicDeviceWithProcessDetail detail, List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "タイマ"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            var timers = detailTimers.Where(t => t.Timer.RecordId == detail.Detail.Id);

            if (timers != null)
            {
                result.Add(LadderRow.AddLDP(label + (outNum + 0).ToString()));

                foreach (var timer in timers)
                {
                    // タイマの開始デバイスを取得
                    result.Add(LadderRow.AddRST(timer.Timer.TimerDeviceT));
                }
            }
            else
            {
                DetailError(detail, "タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            // L2 タイマ開始
            var stopTimer = detailTimers.FirstOrDefault(t => t.Timer.RecordId == detail.Detail.Id);

            if (stopTimer == null)
            {
                DetailError(detail, "タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 4).ToString()));
            result.AddRange(LadderRow.AddTimer(stopTimer.Timer.TimerDeviceT, stopTimer.Timer.TimerDeviceZR));
            result.Add(LadderRow.AddLD(stopTimer.Timer.TimerDeviceT));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            result.Add(LadderRow.AddOR(label + (outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 4).ToString()));

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        /// <summary>
        /// 複合工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public virtual async Task<List<LadderCsvRow>> Module(MnemonicDeviceWithProcessDetail detail)
        {
            var result = new List<LadderCsvRow>();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // 行間ステートメントを追加
            result.Add(CreateStatement(detail, "複合工程"));

            // L0 工程開始
            result.AddRange(await AddL0StartRows(detail));

            // L1 操作実行
            result.Add(LadderRow.AddLD(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 1).ToString()));

            var processDetailFinishDevices = await FinishProcessDevices(detail);
            if (processDetailFinishDevices.Count == 0)
            {
                DetailError(detail, "複数工程では終了工程が必須です");
                return result; // エラーがある場合は、空のリストを返す
            }
            else if (processDetailFinishDevices.Count != 1)
            {
                DetailError(detail, "複数工程では終了工程を1つにしてください");
                return result; // エラーがある場合は、空のリストを返す
            }
            else
            {
                var finishLabel = processDetailFinishDevices.First().Mnemonic.DeviceLabel ?? string.Empty;
                var finishNum = processDetailFinishDevices.First().Mnemonic.StartNum;
                result.Add(LadderRow.AddLD(finishLabel + (finishNum + 4).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(label + (outNum + 4).ToString()));
                result.Add(LadderRow.AddAND(label + (outNum + 0).ToString()));
                result.Add(LadderRow.AddOUT(label + (outNum + 4).ToString()));
            }

            // Manualリセット
            result.AddRange(ManualReset(detail));

            return result;
        }

        // ========== 以下、保護されたヘルパーメソッド ==========

        /// <summary>
        /// ProcessDetailに対応するProcessを取得
        /// </summary>
        protected MnemonicDeviceWithProcess GetProcessForDetail(MnemonicDeviceWithProcessDetail detail)
        {
            var process = _processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            if (process == null)
            {
                throw new InvalidOperationException($"Process with RecordId {detail.Detail.ProcessId} not found for ProcessDetail {detail.Detail.Id}");
            }
            return process;
        }

        /// <summary>
        /// OperationIdに基づいて適切なTimerを取得
        /// </summary>
        protected async Task<MnemonicTimerDevice?> GetTimerForOperation(MnemonicDeviceWithProcessDetail detail)
        {
            // StartTimerIdが設定されている場合は、直接そのタイマーを使用
            if (detail.Detail.StartTimerId.HasValue)
            {
                // MnemonicTimerDeviceテーブルから、指定されたTimerIdを持つレコードを取得
                var timerDevices = await _repository.GetMnemonicTimerDevicesAsync();
                var timerDevice = timerDevices.FirstOrDefault(t =>
                    t.TimerId == detail.Detail.StartTimerId.Value);

                if (timerDevice != null)
                {
                    return timerDevice;
                }
            }

            return null; // タイマーが見つからない場合はnullを返す
        }

        /// <summary>
        /// 行間ステートメントを生成する共通処理
        /// </summary>
        protected LadderCsvRow CreateStatement(MnemonicDeviceWithProcessDetail detail, string collection)
        {
            string id = detail.Detail.Id.ToString();
            return LadderRow.AddStatement(id + ":" + detail.Detail.DetailName + "_" + collection);
        }

        /// <summary>
        /// センサーIO取得の共通処理
        /// </summary>
        /// <param name="sensorKey">センサーキー（StartSensor または FinishSensor）</param>
        /// <param name="detailName">工程詳細名（エラーメッセージ用）</param>
        /// <param name="detailId">工程詳細ID（エラーメッセージ用）</param>
        /// <returns>IO文字列（取得できなかった場合はnull）</returns>
        protected string? GetSensorIO(string? sensorKey, string? detailName, int detailId)
        {
            if (string.IsNullOrWhiteSpace(sensorKey))
                return null;

            return _ioAddressService.GetSingleAddress(
                _ioList,
                sensorKey,
                false,
                detailName!,
                detailId,
                null);
        }

        /// <summary>
        /// SkipMode処理の共通メソッド - SkipModeが設定されている場合にOR/ORI行を追加
        /// </summary>
        /// <param name="rows">追加先のリスト</param>
        /// <param name="detail">工程詳細</param>
        protected void AddSkipModeRows(List<LadderCsvRow> rows, MnemonicDeviceWithProcessDetail detail)
        {
            if (string.IsNullOrEmpty(detail.Detail.SkipMode))
                return;

            var skipDevice = detail.Detail.SkipMode.TrimStart('_');
            var isNegated = detail.Detail.SkipMode.StartsWith("_", StringComparison.Ordinal);

            rows.Add(isNegated
                ? LadderRow.AddORI(skipDevice)
                : LadderRow.AddOR(skipDevice));
        }

        /// <summary>
        /// L0工程開始処理の共通メソッド
        /// </summary>
        /// <param name="detail">工程詳細</param>
        /// <returns>L0工程開始のラダー行リスト</returns>
        protected async Task<List<LadderCsvRow>> AddL0StartRows(MnemonicDeviceWithProcessDetail detail)
        {
            MnemonicTimerDevice? timer = null;
            if (detail.Detail.OperationId != null)
            {
                timer = await GetTimerForOperation(detail);
            }

            var process = GetProcessForDetail(detail);
            return await L0(detail, process, timer);
        }

        /// <summary>
        /// 手動操作釦ラベル名称を生成
        /// </summary>
        protected string? ManualButtonBulider(MnemonicDeviceWithProcessDetail detail)
        {
            // ① OperationId が null なら終了
            if (detail?.Detail?.OperationId is not int opId) return null;

            // ② Operation と Cylinder を特定
            var operation = _operations.FirstOrDefault(o => o.Operation.Id == opId);
            if (operation is null) return null;

            var cylinder = _cylinders.FirstOrDefault(c => c.Cylinder.Id == operation.Operation.CYId);
            if (cylinder is null) return null;

            // ③ 同一 Cylinder に属する OperationId 集合（高速判定用）
            var opIdsInCylinder = _operations
                .Where(o => o.Operation.CYId == cylinder.Cylinder.Id)
                .Select(o => o.Operation.Id)
                .ToHashSet();

            // ④ 対象明細を抽出 → 並べ替え（SortNumber null は末尾）＋ Id で安定化
            var ordered = _details
                .Where(d => d.Detail.OperationId is int id && opIdsInCylinder.Contains(id))
                .OrderBy(d => d.Detail.SortNumber ?? int.MaxValue)
                .ThenBy(d => d.Detail.Id)
                .ToList();

            // ⑤ 現在の明細の位置（0始まり）
            int index = ordered.FindIndex(d => d.Detail.Id == detail.Detail.Id);
            if (index < 0) return null;

            // ⑥ 1始まりに変換してラベル生成（0始まりにしたいなら +1 を外す）
            int detailNumber = index + 1;
            string prefix = $"{Label.PREFIX}{cylinder.Cylinder.CYNum}";
            string manualButton = $"{prefix}.Process[{detailNumber}].bStart";

            return manualButton;
        }

        /// <summary>
        /// 手動リセット釦ラベル名称を生成
        /// </summary>
        protected string? ManualResetBulider(MnemonicDeviceWithProcessDetail detail)
        {
            // ① OperationId が null なら終了
            if (detail?.Detail?.OperationId is not int opId) return null;

            // ② Operation と Cylinder を特定
            var operation = _operations.FirstOrDefault(o => o.Operation.Id == opId);
            if (operation is null) return null;

            var cylinder = _cylinders.FirstOrDefault(c => c.Cylinder.Id == operation.Operation.CYId);
            if (cylinder is null) return null;

            // ③ 同一 Cylinder に属する OperationId 集合（高速判定用）
            var opIdsInCylinder = _operations
                .Where(o => o.Operation.CYId == cylinder.Cylinder.Id)
                .Select(o => o.Operation.Id)
                .ToHashSet();

            // ④ 対象明細を抽出 → 並べ替え（SortNumber null は末尾）＋ Id で安定化
            var ordered = _details
                .Where(d => d.Detail.OperationId is int id && opIdsInCylinder.Contains(id))
                .OrderBy(d => d.Detail.SortNumber ?? int.MaxValue)
                .ThenBy(d => d.Detail.Id)
                .ToList();

            // ⑤ 現在の明細の位置（0始まり）
            int index = ordered.FindIndex(d => d.Detail.Id == detail.Detail.Id);
            if (index < 0) return null;

            // ⑥ 1始まりに変換してラベル生成（0始まりにしたいなら +1 を外す）
            int detailNumber = index + 1;
            string prefix = $"{Label.PREFIX}{cylinder.Cylinder.CYNum}";
            string manualButton = $"{prefix}.Process[{detailNumber}].bReset";

            return manualButton;
        }

        /// <summary>
        /// 手動操作釦ラベル名称を生成
        /// </summary>
        protected string? ManualOperateBulider(MnemonicDeviceWithProcessDetail detail)
        {
            // ① OperationId が null なら終了
            if (detail?.Detail?.OperationId is not int opId) return null;

            // ② Operation と Cylinder を特定
            var operation = _operations.FirstOrDefault(o => o.Operation.Id == opId);
            if (operation is null) return null;

            var cylinder = _cylinders.FirstOrDefault(c => c.Cylinder.Id == operation.Operation.CYId);
            if (cylinder is null) return null;

            // ③ 同一 Cylinder に属する OperationId 集合（高速判定用）
            var opIdsInCylinder = _operations
                .Where(o => o.Operation.CYId == cylinder.Cylinder.Id)
                .Select(o => o.Operation.Id)
                .ToHashSet();

            // ④ 対象明細を抽出 → 並べ替え（SortNumber null は末尾）＋ Id で安定化
            var ordered = _details
                .Where(d => d.Detail.OperationId is int id && opIdsInCylinder.Contains(id))
                .OrderBy(d => d.Detail.SortNumber ?? int.MaxValue)
                .ThenBy(d => d.Detail.Id)
                .ToList();

            // ⑤ 現在の明細の位置（0始まり）
            int index = ordered.FindIndex(d => d.Detail.Id == detail.Detail.Id);
            if (index < 0) return null;

            // ⑥ 1始まりに変換してラベル生成（0始まりにしたいなら +1 を外す）
            int detailNumber = index + 1;
            string prefix = $"{Label.PREFIX}{cylinder.Cylinder.CYNum}";
            string manualButton = $"{prefix}.Process[{detailNumber}].bOperate";

            return manualButton;
        }

        /// <summary>
        /// CylinderIdに対応するControlBoxを取得
        /// </summary>
        protected async Task<List<ControlBox>?> GetControlBoxesByCylinderId(MnemonicDeviceWithProcessDetail detail)
        {
            // ① OperationId が null なら終了
            if (detail?.Detail?.OperationId is not int opId) return null;

            // ② Operation と Cylinder を特定
            var operation = _operations.FirstOrDefault(o => o.Operation.Id == opId);
            if (operation is null) return null;

            var cylinder = _cylinders.FirstOrDefault(c => c.Cylinder.Id == operation.Operation.CYId);
            if (cylinder is null) return null;

            // ③ plcId
            var plcId = _mainViewModel?.SelectedPlc?.Id ?? 0;
            if (plcId == 0) return null;

            // ④ 中間テーブル（対象Cylinderのみ）
            var mapsAll = await _repository.GetCylinderControlBoxesByPlcIdAsync(plcId);
            var maps = mapsAll.Where(m => m.CylinderId == cylinder.Cylinder.Id).ToList();
            if (maps.Count == 0) return null;

            // ⑤ ControlBox を辞書化（BoxNumber→ControlBox）
            var cbByNumber = (await _repository.GetControlBoxesByPlcIdAsync(plcId))
                .GroupBy(cb => cb.BoxNumber)
                .ToDictionary(g => g.Key, g => g.First());

            // ⑥ マップ順で引き当て（重複BoxNumberは除外）
            var seen = new HashSet<int>();
            var result = new List<ControlBox>();
            foreach (var m in maps)
            {
                if (seen.Add(m.ManualNumber) && cbByNumber.TryGetValue(m.ManualNumber, out var cb))
                {
                    result.Add(cb);
                }
            }

            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// Manualリセット処理
        /// </summary>
        protected List<LadderCsvRow> ManualReset(MnemonicDeviceWithProcessDetail detail)
        {
            List<LadderCsvRow> result = new();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            string? manualButton = ManualButtonBulider(detail);
            string? manualReset = ManualResetBulider(detail);
            string? manualOperate = ManualOperateBulider(detail);

            if (!string.IsNullOrEmpty(manualReset) && !string.IsNullOrEmpty(manualOperate))
            {
                result.Add(LadderRow.AddLDP(label + (outNum + 4).ToString()));
                result.Add(LadderRow.AddORP(manualReset));
                result.Add(LadderRow.AddRST(manualOperate));
            }
            return result;
        }

        /// <summary>
        /// L0 工程開始のLadderCsvRowを生成します。
        /// </summary>
        protected async Task<List<LadderCsvRow>> L0(MnemonicDeviceWithProcessDetail detail, MnemonicDeviceWithProcess process, MnemonicTimerDevice? timer)
        {
            List<LadderCsvRow> result = new();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            // ProcessDetailの開始条件を取得（中間テーブルから）
            var processDetailStartIds = new List<int>();

            // 中間テーブルから取得
            var connections = await _repository.GetConnectionsByToIdAsync(detail.Detail.Id);
            processDetailStartIds.AddRange(connections.Select(c => c.FromProcessDetailId));

            var processDetailStartDevices = _details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // 手動操作釦ラベル名称
            string? manualButton = ManualButtonBulider(detail);
            string? manualReset = ManualResetBulider(detail);
            string? manualOperate = ManualOperateBulider(detail);
            List<ControlBox>? manualOperateGo = await GetControlBoxesByCylinderId(detail);

            // 各個操作モードがOFFの際はリセットする
            if (manualOperateGo is not null
                && manualButton is not null
                && manualReset is not null)
            {
                bool isFirst = true;
                foreach (var cb in manualOperateGo)
                {
                    if (isFirst)
                    {
                        result.Add(LadderRow.AddLDI(cb.ManualMode));
                        isFirst = false;
                    }
                    else
                    {
                        result.Add(LadderRow.AddORI(cb.ManualMode));
                    }
                }
                result.Add(LadderRow.AddRST(manualButton));
                result.Add(LadderRow.AddPLS(manualReset));
            }

            if (!string.IsNullOrEmpty(manualButton) && !string.IsNullOrEmpty(manualOperate))
            {
                // 手動リセットがあれば AND NOT で追加
                if (manualOperateGo is not null)
                {
                    bool isFirst = true;
                    foreach (var cb in manualOperateGo)
                    {
                        if (isFirst)
                        {
                            result.Add(LadderRow.AddLD(cb.ManualButton));
                            isFirst = false;
                        }
                        else
                        {
                            result.Add(LadderRow.AddOR(cb.ManualButton));

                        }
                        result.Add(LadderRow.AddAND(manualButton));
                        result.Add(LadderRow.AddSET(manualOperate));
                    }
                }
            }

            // L0 工程開始
            // StartSensor が先頭 '_' なら反転、SkipMode も同様のルール
            bool IsNegated(string s) => s.StartsWith("_", StringComparison.Ordinal);
            string Strip(string s) => s.TrimStart('_');

            if (timer is not null)
            {
                // タイマーが設定されている場合は、タイマーの開始を追加
                result.Add(LadderRow.AddLD(timer.TimerDeviceT));
            }
            else
            {
                var startSensorKey = detail.Detail.StartSensor;

                if (!string.IsNullOrWhiteSpace(startSensorKey))
                {
                    // IO 取得
                    var ioSensor = _ioAddressService.GetSingleAddress(
                        _ioList,
                        startSensorKey,
                        false,
                        detail.Detail.DetailName!,
                        detail.Detail.Id,
                        null);

                    if (ioSensor is null)
                    {
                        // センサー未解決 → 常時OFF
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                    }
                    else
                    {
                        // 先頭 '_' で反転（LD or LDI）
                        result.Add(IsNegated(startSensorKey)
                            ? LadderRow.AddLDI(ioSensor)
                            : LadderRow.AddLD(ioSensor));

                        // 固定OR（_label + (_outNum + 3)）
                        result.Add(LadderRow.AddOR($"{label}{outNum + 3}"));

                        // SkipMode があれば OR / ORI で追加
                        var skip = detail.Detail.SkipMode;
                        if (!string.IsNullOrWhiteSpace(skip))
                        {
                            var tok = Strip(skip);
                            result.Add(IsNegated(skip)
                                ? LadderRow.AddORI(tok)
                                : LadderRow.AddOR(tok));
                        }
                    }

                    // 最後に PauseSignal を AND
                    result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                }
                else
                {
                    // StartSensor 未設定 → PauseSignal だけで開始
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                }
            }

            result.Add(LadderRow.AddOR(label + (outNum + 0).ToString()));

            // 複数工程かどうか
            int blockNumber = detail.Detail.BlockNumber ?? 0;
            if (blockNumber != 0)
            {
                var moduleDetail = _details.FirstOrDefault(d => d.Detail.Id == blockNumber);

                if (moduleDetail != null)
                {
                    processDeviceStartNum = moduleDetail.Mnemonic.StartNum;
                    processDeviceLabel = moduleDetail.Mnemonic.DeviceLabel ?? string.Empty;
                }
                else
                {
                    DetailError(detail, $"BlockNumber {blockNumber} に対応する工程詳細が見つかりません。");
                    return result; // エラーがある場合は、空のリストを返す
                }

            }

            // isResetAfter が true の場合、常時ONにする

            if (detail.Detail.IsResetAfter == true)
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysON));
            }
            else
            {
                result.Add(LadderRow.AddLD(processDeviceLabel + (processDeviceStartNum + 1).ToString()));
            }

            foreach (var d in processDetailStartDevices)
            {
                if (!string.IsNullOrEmpty(detail.Detail.StartSensor))
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 1).ToString()));
                }
                else
                {
                    // ブロック工程の場合は1にしたい
                    if (detail.Detail.BlockNumber != d.Detail.Id)
                    {
                        result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 4).ToString()));
                    }
                    else
                    {
                    }
                }
            }

            if (!string.IsNullOrEmpty(manualOperate))
            {
                result.Add(LadderRow.AddOR(manualOperate));
            }

            result.Add(LadderRow.AddANB());
            result.Add(LadderRow.AddOUT(label + (outNum + 0).ToString()));

            return result;
        }

        /// <summary>
        /// L4 工程完了処理
        /// </summary>
        protected List<LadderCsvRow> L4(MnemonicDeviceWithProcessDetail detail)
        {
            List<LadderCsvRow> result = new();
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            var outNum = detail.Mnemonic.StartNum;

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 4).ToString()));

            return result;
        }

        /// <summary>
        /// 開始工程デバイスを取得
        /// </summary>
        protected async Task<List<MnemonicDeviceWithProcessDetail>> StartDevices(MnemonicDeviceWithProcessDetail detail)
        {
            // ProcessDetailの開始条件を取得（中間テーブルから）
            var processDetailStartIds = new List<int>();

            // 中間テーブルから取得
            var connections = await _repository.GetConnectionsByToIdAsync(detail.Detail.Id);
            processDetailStartIds.AddRange(connections.Select(c => c.FromProcessDetailId));

            var processDetailStartDevices = _details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();
            return processDetailStartDevices;
        }

        /// <summary>
        /// ProcessDetailFinishテーブルからこのインスタンスの終了工程IDを取得
        /// </summary>
        /// <param name="detail">工程詳細</param>
        /// <returns>List<MnemonicDeviceWithProcessDetail>終了工程ID</returns>
        protected async Task<List<MnemonicDeviceWithProcessDetail>> FinishProcessDevices(MnemonicDeviceWithProcessDetail detail)
        {
            var finishes = await _repository.GetFinishesByProcessDetailIdAsync(detail.Detail.Id);
            var processDetailFinishIds = finishes.Select(f => f.FinishProcessDetailId).ToList();

            var processDetailFinishDevices = _details
                .Where(d => processDetailFinishIds.Contains(d.Mnemonic.RecordId))
                .ToList();
            return processDetailFinishDevices;
        }

        /// <summary>
        /// Detail用のｴﾗｰ出力メソッド
        /// </summary>
        /// <param name="detail">工程詳細</param>
        /// <param name="message">エラーメッセージ</param>
        protected void DetailError(MnemonicDeviceWithProcessDetail detail, string message)
        {
            // エラーをアグリゲートするメソッドを呼び出す
            _errorAggregator.AddError(new OutputError
            {
                Message = message,
                RecordName = detail.Detail.DetailName,
                MnemonicId = (int)MnemonicType.ProcessDetail,
                RecordId = detail.Detail.Id,
                IsCritical = true
            });
        }
    }
}
