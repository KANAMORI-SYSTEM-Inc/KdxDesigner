using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.ViewModels;
using Kdx.Contracts.Interfaces;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Enums;


namespace KdxDesigner.Utils.ProcessDetail
{
    // ... BuildDetailクラス ...

    /// <summary>
    /// 単一の工程詳細レコードのラダー生成ロジックをカプセル化するクラス。
    /// </summary>
    internal class DetailUnitBuilder
    {
        // --- サービスのフィールド ---
        private readonly MainViewModel _mainViewModel;
        private readonly IIOAddressService _ioAddressService;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IAccessRepository _repository;

        // --- データセットのフィールド ---
        private readonly MnemonicDeviceWithProcessDetail _detail;
        private readonly List<MnemonicDeviceWithProcessDetail> _details;
        private readonly List<MnemonicDeviceWithProcess> _processes;
        private readonly List<MnemonicDeviceWithOperation> _operations;
        private readonly List<MnemonicDeviceWithCylinder> _cylinders;
        private readonly List<IO> _ioList;

        // --- 派生したフィールド ---
        private readonly MnemonicDeviceWithProcess _process;
        private readonly string _label;
        private readonly int _outNum;

        /// <summary>
        /// コンストラクタで、ビルドに必要なすべての情報を受け取る
        /// </summary>
        public DetailUnitBuilder(
            // データセット
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            // サービス
            MainViewModel mainViewModel,
            IIOAddressService ioAddressService,
            IErrorAggregator errorAggregator,
            IAccessRepository repository)
        {
            // --- サービス ---
            _mainViewModel = mainViewModel;
            _ioAddressService = ioAddressService;
            _errorAggregator = errorAggregator;
            _repository = repository;

            // --- データセット ---
            _detail = detail;
            _details = details;
            _processes = processes;
            _operations = operations;
            _cylinders = cylinders;
            _ioList = ioList;

            // --- 派生データ (コンストラクタで一度だけ計算) ---
            _label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            _outNum = detail.Mnemonic.StartNum;
            if (_processes == null)
            {
                throw new ArgumentNullException(nameof(processes), "Processes list cannot be null");
            }

            var process = _processes.FirstOrDefault(p => p.Mnemonic.RecordId == _detail.Detail.ProcessId);
            if (process == null)
            {
                throw new InvalidOperationException($"Process with RecordId {_detail.Detail.ProcessId} not found for ProcessDetail {_detail.Detail.Id}");
            }
            _process = process;
        }

        /// <summary>
        /// 通常工程のビルド
        /// </summary>
        public List<LadderCsvRow> BuildNormal()
        {
            var result = new List<LadderCsvRow>();
            result.Add(CreateStatement("通常工程"));

            var detailFunctions = CreateDetailFunctions();
            var timer = GetTimerForOperation();

            // L0 工程開始
            result.AddRange(detailFunctions.L0(timer));

            // L1 工程開始
            var operationFinish = _operations.FirstOrDefault(o => o.Mnemonic.RecordId == _detail.Detail.OperationId);
            var opFinishNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var opFinishLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(opFinishLabel + (opFinishNum + 19).ToString()));

            if (!string.IsNullOrEmpty(_detail.Detail.SkipMode))
            {
                if (_detail.Detail.SkipMode.Contains("_"))
                {
                    var skipDevice = _detail.Detail.SkipMode.Replace("_", string.Empty);
                    result.Add(LadderRow.AddORI(skipDevice));
                }
                else
                {
                    var skipDevice = _detail.Detail.SkipMode;
                    result.Add(LadderRow.AddOR(skipDevice));
                }
            }

            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(detailFunctions.L4());

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 工程まとめのビルド
        /// </summary>
        /// <returns></returns>
        public List<LadderCsvRow> BuildSummarize()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("工程まとめ"));
            var detailFunctions = CreateDetailFunctions();

            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));
            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(detailFunctions.L4());

            return result;

        }

        /// <summary>
        /// センサON確認のビルド
        /// </summary>
        /// <returns></returns>
        public List<LadderCsvRow> BuildSensorON()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("センサON確認"));
            
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            // 3行目　センサー名称からIOリスト参照
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (!string.IsNullOrEmpty(_detail.Detail.FinishSensor))
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.FinishSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)//　万一nullの場合は　空でLD接点入れておく
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (_detail.Detail.FinishSensor.Contains("_"))    // ON工程　→　_の有無問わず　LD接点
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));//恐らく使用されない
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor));

                    }
                }
                result.Add(LadderRow.AddOR(SettingsManager.Settings.DebugTest));

                // skipModeが設定されている場合は、スキップ処理を追加
                if (!string.IsNullOrEmpty(_detail.Detail.SkipMode))
                {
                    if (_detail.Detail.SkipMode.Contains("_"))
                    {
                        var skipDevice = _detail.Detail.SkipMode.Replace("_", string.Empty);
                        result.Add(LadderRow.AddORI(skipDevice));

                    }
                    else
                    {
                        var skipDevice = _detail.Detail.SkipMode;
                        result.Add(LadderRow.AddOR(skipDevice));
                    }
                }

                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(detailFunctions.L4());

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 工程詳細：センサOFFのビルド
        /// </summary>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildSensorOFF()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("センサOFF確認"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
                        // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            //3行目　センサー名称からIOリスト参照

            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (!string.IsNullOrEmpty(_detail.Detail.FinishSensor))
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.FinishSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)//　万一nullの場合は　空でLD接点入れておく
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));

                }
                else
                {
                    if (_detail.Detail.FinishSensor.Contains("_"))    // OFF工程　→　_の有無問わず　LDI接点
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));//恐らく使用されない
                    }
                    else
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));

                    }
                }
                result.Add(LadderRow.AddOR(SettingsManager.Settings.DebugTest));

                // skipModeが設定されている場合は、スキップ処理を追加
                if (!string.IsNullOrEmpty(_detail.Detail.SkipMode))
                {
                    if (_detail.Detail.SkipMode.Contains("_"))
                    {
                        var skipDevice = _detail.Detail.SkipMode.Replace("_", string.Empty);
                        result.Add(LadderRow.AddORI(skipDevice));

                    }
                    else
                    {
                        var skipDevice = _detail.Detail.SkipMode;
                        result.Add(LadderRow.AddOR(skipDevice));
                    }
                }

                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(detailFunctions.L4());

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 工程詳細：工程分岐のビルド
        /// </summary>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildBranch()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("工程分岐"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            // L1 操作開始
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (!string.IsNullOrEmpty(_detail.Detail.FinishSensor))
            {
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.FinishSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (_detail.Detail.FinishSensor.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor));

                    }
                }

            }
            else
            {
                // FinishSensorの設定ナシ
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                _errorAggregator.AddError(new OutputError
                {
                    Message = "FinishSensor が設定されていません。",
                    RecordName = _detail.Detail.DetailName,
                    MnemonicId = (int)MnemonicType.ProcessDetail,
                    RecordId = _detail.Detail.Id,
                    IsCritical = false
                });
            }

            result.Add(LadderRow.AddANI(_label + (_outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            // L4 工程完了
            result.AddRange(detailFunctions.L4());

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 工程詳細：工程合流のビルド
        /// </summary>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildMerge()
        {
            var result = new List<LadderCsvRow>();
            var detailFunctions = CreateDetailFunctions();

            // 行間ステートメントを追加
            string id = _detail.Detail.Id.ToString();
            result.Add(CreateStatement("工程合流"));


            // L0 の初期値を設定
            var deviceNum = _detail.Mnemonic.StartNum;
            var label = _detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = _processes.FirstOrDefault(p => p.Mnemonic.RecordId == _detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessDetailの開始条件を取得（中間テーブルから）
            var processDetailStartIds = new List<int>();
            
            // 中間テーブルから取得
            var connections = _repository.GetConnectionsByToId(_detail.Detail.Id);
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
            result.AddRange(detailFunctions.ManualReset());
            return result;

        }

        /// <summary>
        /// 工程詳細：IL待ち工程のビルド
        /// </summary>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildILWait()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("IL待ち"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            // L1 操作開始
            if (!string.IsNullOrEmpty(_detail.Detail.FinishSensor))
            {
                // StartSensorが設定されている場合
                result.Add(LadderRow.AddLD(_detail.Detail.FinishSensor));
            }
            else
            {
                // StartSensornの設定ナシ
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                detailFunctions.DetailError("FinishSensor が設定されていません。");
            }
            result.Add(LadderRow.AddOR(SettingsManager.Settings.DebugTest));

            // skipModeが設定されている場合は、スキップ処理を追加
            if (!string.IsNullOrEmpty(_detail.Detail.SkipMode))
            {
                if (_detail.Detail.SkipMode.Contains("_"))
                {
                    var skipDevice = _detail.Detail.SkipMode.Replace("_", string.Empty);
                    result.Add(LadderRow.AddORI(skipDevice));

                }
                else
                {
                    var skipDevice = _detail.Detail.SkipMode;
                    result.Add(LadderRow.AddOR(skipDevice));
                }
            }
            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));


            if (!string.IsNullOrEmpty(_detail.Detail.ILStart))
            {
                result.Add(LadderRow.AddLD(_label + (_outNum + 0).ToString()));
                result.Add(LadderRow.AddANI(_label + (_outNum + 1).ToString()));
                result.Add(LadderRow.AddOUT(_detail.Detail.ILStart));
            }

            // L4 工程完了
            result.AddRange(detailFunctions.L4());

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 工程詳細：工程OFF確認のビルド
        /// </summary>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildDetailProcessOFF()
        {
            var result = new List<LadderCsvRow>();
            
            // 行間ステートメントを追加
            result.Add(CreateStatement("工程OFF確認"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            // L1 操作開始
            // ProcessDetailFinishテーブルから終了工程IDを取得
            var finishes = _repository.GetFinishesByProcessDetailId(_detail.Detail.Id);
            var detailOffIds = finishes.Select(f => f.FinishProcessDetailId).ToList();

            if (detailOffIds.Count != 1)
            {
                detailFunctions.DetailError("工程OFF確認に終了工程が複数設定されています。");
                return result; // エラーがある場合は、空のリストを返す
            }

            if (detailOffIds.Count == 0)
            {
                detailFunctions.DetailError("工程OFF確認に終了工程が設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            var detailOffDevice = detailOffIds[0];

            var detailOffDeviceMnemonic = _details
                .FirstOrDefault(d => d.Mnemonic.RecordId == detailOffDevice);

            if (detailOffDeviceMnemonic == null)
            {
                detailFunctions.DetailError($"工程OFF確認の終了工程に設定されているデバイスが見つかりません。ID: {detailOffDevice}");
                return result; // エラーがある場合は、空のリストを返す
            }
            else
            {
                result.Add(LadderRow.AddLD(detailOffDeviceMnemonic.Mnemonic.DeviceLabel 
                    + detailOffDeviceMnemonic.Mnemonic.StartNum.ToString()));

                // skipModeが設定されている場合は、スキップ処理を追加
                if (!string.IsNullOrEmpty(_detail.Detail.SkipMode))
                {
                    if (_detail.Detail.SkipMode.Contains("_"))
                    {
                        var skipDevice = _detail.Detail.SkipMode.Replace("_", string.Empty);
                        result.Add(LadderRow.AddORI(skipDevice));

                    }
                    else
                    {
                        var skipDevice = _detail.Detail.SkipMode;
                        result.Add(LadderRow.AddOR(skipDevice));
                    }
                }

                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
                result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
                result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

                
                
            }
            result.Add(LadderRow.AddLD(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 4).ToString()));

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 工程詳細：期間工程のビルド
        /// </summary>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildSeason()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("期間工程"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            // L2 操作停止
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (!string.IsNullOrEmpty(_detail.Detail.FinishSensor))
            {
                // ioの取得を共通コンポーネント化すること
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.FinishSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (_detail.Detail.FinishSensor.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddLDI(ioSensor ?? ""));
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor ?? ""));

                    }
                }
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            var processDetailFinishDevices = detailFunctions.FinishDevices();
            if (!string.IsNullOrEmpty(_detail.Detail.FinishSensor))
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
            if (!string.IsNullOrEmpty(_detail.Detail.SkipMode))
            {
                if (_detail.Detail.SkipMode.Contains("_"))
                {
                    var skipDevice = _detail.Detail.SkipMode.Replace("_", string.Empty);
                    result.Add(LadderRow.AddORI(skipDevice));

                }
                else
                {
                    var skipDevice = _detail.Detail.SkipMode;
                    result.Add(LadderRow.AddOR(skipDevice));
                }
            }

            result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 2).ToString()));

            // L4 工程完了
            // detailのoperationIdからOperationの先頭デバイスを取得
            var operationFinish = _operations.FirstOrDefault(o => o.Mnemonic.RecordId == _detail.Detail.OperationId);
            var operationFinishStartNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var operationFinishDeviceLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 2).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 4).ToString()));

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 工程詳細：タイマ工程のビルド
        /// </summary>
        /// <param name="detailTimers">タイマの詳細リスト</param>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildTimerProcess(List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("タイマ工程"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timersDetail = _repository.GetTimersByRecordId(_mainViewModel.SelectedCycle!.Id, 3, _detail.Detail.OperationId.Value);
                var timer = timersDetail.Where(t => t.TimerCategoryId == 15).FirstOrDefault();

                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            var timers = detailTimers.Where(t => t.Timer.RecordId == _detail.Detail.Id);

            if (timers != null)
            {
                result.Add(LadderRow.AddLDP(_label + (_outNum + 0).ToString()));

                foreach (var timer in timers)
                {
                    // タイマの開始デバイスを取得
                    result.Add(LadderRow.AddRST(timer.Timer.TimerDeviceT));
                }
            }
            else
            {
                detailFunctions.DetailError("タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            // L2 タイマ開始
            var stopTimer = detailTimers.FirstOrDefault(t => t.Timer.RecordId == _detail.Detail.Id);

            if (stopTimer == null)
            {
                detailFunctions.DetailError("タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            result.Add(LadderRow.AddLD(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_label + (_outNum + 2).ToString()));
            result.AddRange(LadderRow.AddTimer(stopTimer.Timer.TimerDeviceT, stopTimer.Timer.TimerDeviceZR));
            result.Add(LadderRow.AddLD(stopTimer.Timer.TimerDeviceT));
            result.Add(LadderRow.AddOR(_label + (_outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 2).ToString()));

            // L4 工程完了
            result.AddRange(detailFunctions.L4());

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 工程詳細：タイマのビルド
        /// </summary>
        /// <param name="detailTimers">タイマの詳細リスト</param>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildTimer(List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("タイマ"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timersDetail = _repository.GetTimersByRecordId(_mainViewModel.SelectedCycle!.Id, 3, _detail.Detail.OperationId.Value);

                var timer = timersDetail.Where(t => t.TimerCategoryId == 15).FirstOrDefault();

                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            var timers = detailTimers.Where(t => t.Timer.RecordId == _detail.Detail.Id);

            if (timers != null)
            {
                result.Add(LadderRow.AddLDP(_label + (_outNum + 0).ToString()));

                foreach (var timer in timers)
                {
                    // タイマの開始デバイスを取得
                    result.Add(LadderRow.AddRST(timer.Timer.TimerDeviceT));
                }
            }
            else
            {
                detailFunctions.DetailError("タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            // L2 タイマ開始
            var stopTimer = detailTimers.FirstOrDefault(t => t.Timer.RecordId == _detail.Detail.Id);

            if (stopTimer == null)
            {
                detailFunctions.DetailError("タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            result.Add(LadderRow.AddLD(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_label + (_outNum + 4).ToString()));
            result.AddRange(LadderRow.AddTimer(stopTimer.Timer.TimerDeviceT, stopTimer.Timer.TimerDeviceZR));
            result.Add(LadderRow.AddLD(stopTimer.Timer.TimerDeviceT));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            result.Add(LadderRow.AddOR(_label + (_outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 4).ToString()));

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 複合工程のビルド
        /// </summary>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildModule()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("複合工程"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            // L1 操作実行
            result.Add(LadderRow.AddLD(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 1).ToString()));

            var processDetailFinishDevices = detailFunctions.FinishDevices();
            if (processDetailFinishDevices.Count == 0)
            {
                detailFunctions.DetailError("複数工程では終了工程が必須です");
                return result; // エラーがある場合は、空のリストを返す
            }
            else if (processDetailFinishDevices.Count != 1)
            {
                detailFunctions.DetailError("複数工程では終了工程を1つにしてください");
                return result; // エラーがある場合は、空のリストを返す
            }
            else
            {
                var finishLabel = processDetailFinishDevices.First().Mnemonic.DeviceLabel ?? string.Empty;
                var finishNum = processDetailFinishDevices.First().Mnemonic.StartNum;    
                result.Add(LadderRow.AddLD(finishLabel + (finishNum + 4).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(_label + (_outNum + 4).ToString()));
                result.Add(LadderRow.AddAND(_label + (_outNum + 0).ToString()));
                result.Add(LadderRow.AddOUT(_label + (_outNum + 4).ToString()));

            }

            // Manualリセット
            result.AddRange(detailFunctions.ManualReset());

            return result;
        }

        /// <summary>
        /// 複数工程のビルド
        /// </summary>
        /// <returns>ニモニックリスト</returns>
        public List<LadderCsvRow> BuildReset()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement("リセット"));
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            // L0 工程開始
            if (_detail.Detail.OperationId != null)
            {
                var timer = GetTimerForOperation();
                result.AddRange(detailFunctions.L0(timer));

            }
            else
            {
                result.AddRange(detailFunctions.L0(null));

            }

            // L4 操作実行
            result.Add(LadderRow.AddLD(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 4).ToString()));

            return result;
        }



        /// <summary>
        /// OperationIdに基づいて適切なTimerを取得
        /// </summary>
        private MnemonicTimerDevice? GetTimerForOperation()
        {
            // StartTimerIdが設定されている場合は、直接そのタイマーを使用
            if (_detail.Detail.StartTimerId.HasValue)
            {
                // MnemonicTimerDeviceテーブルから、指定されたTimerIdを持つレコードを取得
                var timerDevices = _repository.GetMnemonicTimerDevices();
                var timerDevice = timerDevices.FirstOrDefault(t => 
                    t.TimerId == _detail.Detail.StartTimerId.Value);
                
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
        private LadderCsvRow CreateStatement(string collection)
        {
            string id = _detail.Detail.Id.ToString();
            return LadderRow.AddStatement(id + ":" + _detail.Detail.DetailName + "_" + collection);
        }

        /// <summary>
        /// BuildDetailFunctions のインスタンスを生成する共通処理
        /// </summary>
        private BuildDetailFunctions CreateDetailFunctions()
        {
            return new BuildDetailFunctions(
                _detail,
                _process,
                _mainViewModel,
                _ioAddressService,
                _errorAggregator,
                _repository,
                _processes,
                _details,
                _operations,
                _cylinders,
                _ioList
            );
        }

    }
}
