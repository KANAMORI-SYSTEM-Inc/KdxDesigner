using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;


namespace KdxDesigner.Utils.ProcessDetail
{
    /// <summary>
    /// ProcessDetailのビルド関数を提供するクラス。
    /// </summary>
    /// <remarks>
    /// 継承している <see cref="BuildDetail"/> クラスは、processDetailの出力単位ビルド機能を提供するのに対し、
    /// このクラスは各アウトコイル単位のビルド機能を提供します。
    /// </remarks>
    internal class BuildDetailFunctions : BuildDetail
    {
        // --- このクラス固有のフィールド ---
        private readonly MnemonicDeviceWithProcessDetail _detail;
        private readonly MnemonicDeviceWithProcess _process;
        private readonly string _label;
        private readonly int _outNum;

        /// <summary>
        /// 新しい BuildDetailFunctions のインスタンスを初期化します。
        /// </summary>
        /// <param name="detail">処理対象の工程詳細データ</param>
        /// <param name="process">処理対象の工程データ</param>
        /// <param name="mainViewModel">MainViewからの初期値</param>
        /// <param name="ioAddressService">IO検索用のサービス</param>
        /// <param name="errorAggregator">エラー出力用のサービス</param>
        /// <param name="repository">ACCESSファイル検索用のリポジトリ</param>
        /// <param name="processes">全工程のリスト</param>
        /// <param name="details">全工程詳細のリスト</param>
        /// <param name="operations">全操作のリスト</param>
        /// <param name="cylinders">全CYのリスト</param>
        /// <param name="ioList">全IOのリスト</param>
        public BuildDetailFunctions(
            // --- このクラスで直接使用する引数 ---
            MnemonicDeviceWithProcessDetail detail,
            MnemonicDeviceWithProcess process,
            // --- 基底クラスに渡すための引数 ---
            MainViewModel mainViewModel,
            IIOAddressService ioAddressService,
            IErrorAggregator errorAggregator,
            ISupabaseRepository repository,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
            // --- 基底クラスのコンストラクタを正しく呼び出す ---
            : base(mainViewModel, ioAddressService, errorAggregator, repository,
                   processes, details, operations, cylinders, ioList)
        {
            // このクラス固有のフィールドを初期化
            _detail = detail;
            _process = process;
            _label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            _outNum = detail.Mnemonic.StartNum;

            // _details や _ioList の初期化は不要（基底クラスのコンストラクタが実行するため）
        }

        public string? ManualButtonBulider()
        {
            // ① OperationId が null なら終了
            if (_detail?.Detail?.OperationId is not int opId) return null;

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
            int index = ordered.FindIndex(d => d.Detail.Id == _detail.Detail.Id);
            if (index < 0) return null;

            // ⑥ 1始まりに変換してラベル生成（0始まりにしたいなら +1 を外す）
            int detailNumber = index + 1;
            string prefix = $"{Label.PREFIX}{cylinder.Cylinder.CYNum}";
            string manualButton = $"{prefix}.Process[{detailNumber}].bStart";

            return manualButton;
        }

        public string? ManualResetBulider()
        {
            // ① OperationId が null なら終了
            if (_detail?.Detail?.OperationId is not int opId) return null;

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
            int index = ordered.FindIndex(d => d.Detail.Id == _detail.Detail.Id);
            if (index < 0) return null;

            // ⑥ 1始まりに変換してラベル生成（0始まりにしたいなら +1 を外す）
            int detailNumber = index + 1;
            string prefix = $"{Label.PREFIX}{cylinder.Cylinder.CYNum}";
            string manualButton = $"{prefix}.Process[{detailNumber}].bReset";

            return manualButton;
        }

        public string? ManualOperateBulider()
        {
            // ① OperationId が null なら終了
            if (_detail?.Detail?.OperationId is not int opId) return null;

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
            int index = ordered.FindIndex(d => d.Detail.Id == _detail.Detail.Id);
            if (index < 0) return null;

            // ⑥ 1始まりに変換してラベル生成（0始まりにしたいなら +1 を外す）
            int detailNumber = index + 1;
            string prefix = $"{Label.PREFIX}{cylinder.Cylinder.CYNum}";
            string manualButton = $"{prefix}.Process[{detailNumber}].bOperate";

            return manualButton;
        }

        public async Task<List<ControlBox>?> GetControlBoxesByCylinderId()
        {
            // ① OperationId が null なら終了
            if (_detail?.Detail?.OperationId is not int opId) return null;

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

        public List<LadderCsvRow> ManualReset()
        {
            List<LadderCsvRow> result = new();

            string? manualButton = ManualButtonBulider();
            string? manualReset = ManualResetBulider();
            string? manualOperate = ManualOperateBulider();


            if (!string.IsNullOrEmpty(manualReset) && !string.IsNullOrEmpty(manualOperate))
            {
                result.Add(LadderRow.AddLDP(_label + (_outNum + 4).ToString()));
                result.Add(LadderRow.AddLDP(manualReset));
                result.Add(LadderRow.AddRST(manualOperate));
            }
            return result;
        }

        /// <summary>
        /// L0 工程開始のLadderCsvRowを生成します。
        /// </summary>
        /// <returns></returns>
        public async Task<List<LadderCsvRow>> L0(MnemonicTimerDevice? timer)
        {
            List<LadderCsvRow> result = new();

            // ProcessDetailの開始条件を取得（中間テーブルから）
            var processDetailStartIds = new List<int>();

            // 中間テーブルから取得
            var connections = await _repository.GetConnectionsByToIdAsync(_detail.Detail.Id);
            processDetailStartIds.AddRange(connections.Select(c => c.FromProcessDetailId));

            var processDetailStartDevices = _details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            var processDeviceStartNum = _process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = _process?.Mnemonic.DeviceLabel ?? string.Empty;

            // 手動操作釦ラベル名称
            string? manualButton = ManualButtonBulider();
            string? manualReset = ManualResetBulider();
            string? manualOperate = ManualOperateBulider();
            List<ControlBox>? manualOperateGo = await GetControlBoxesByCylinderId();

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
                var startSensorKey = _detail.Detail.StartSensor;

                if (!string.IsNullOrWhiteSpace(startSensorKey))
                {
                    // IO 取得
                    var ioSensor = _ioAddressService.GetSingleAddress(
                        _ioList,
                        startSensorKey,
                        false,
                        _detail.Detail.DetailName!,
                        _detail.Detail.Id,
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
                        result.Add(LadderRow.AddOR($"{_label}{_outNum + 3}"));

                        // SkipMode があれば OR / ORI で追加
                        var skip = _detail.Detail.SkipMode;
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

            result.Add(LadderRow.AddOR(_label + (_outNum + 0).ToString()));

            // 複数工程かどうか
            int blockNumber = _detail.Detail.BlockNumber ?? 0;
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
                    DetailError($"BlockNumber {blockNumber} に対応する工程詳細が見つかりません。");
                    return result; // エラーがある場合は、空のリストを返す
                }

            }
            result.Add(LadderRow.AddLD(processDeviceLabel + (processDeviceStartNum + 1).ToString()));

            foreach (var d in processDetailStartDevices)
            {
                if (!string.IsNullOrEmpty(_detail.Detail.StartSensor))
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 1).ToString()));
                }
                else
                {
                    // ブロック工程の場合は1にしたい
                    if (_detail.Detail.BlockNumber != d.Detail.Id)
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
            result.Add(LadderRow.AddOUT(_label + (_outNum + 0).ToString()));

            return result;
        }

        public List<LadderCsvRow> L4()
        {
            List<LadderCsvRow> result = new();

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_outNum + 4).ToString()));
            result.Add(LadderRow.AddAND(_label + (_outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_outNum + 4).ToString()));

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<MnemonicDeviceWithProcessDetail>> StartDevices()
        {
            // ProcessDetailの開始条件を取得（中間テーブルから）
            var processDetailStartIds = new List<int>();

            // 中間テーブルから取得
            var connections = await _repository.GetConnectionsByToIdAsync(_detail.Detail.Id);
            processDetailStartIds.AddRange(connections.Select(c => c.FromProcessDetailId));

            var processDetailStartDevices = _details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();
            return processDetailStartDevices;
        }

        /// <summary>
        /// ProcessDetailFinishテーブルからこのインスタンスの終了工程IDを取得
        /// </summary>
        /// <returns>List<MnemonicDeviceWithProcessDetail>終了工程ID</returns>
        public async Task<List<MnemonicDeviceWithProcessDetail>> FinishDevices()
        {
            var finishes = await _repository.GetFinishesByProcessDetailIdAsync(_detail.Detail.Id);
            var processDetailFinishIds = finishes.Select(f => f.FinishProcessDetailId).ToList();

            var processDetailFinishDevices = _details
                .Where(d => processDetailFinishIds.Contains(d.Mnemonic.RecordId))
                .ToList();
            return processDetailFinishDevices;
        }

        /// <summary>
        /// Detail用のｴﾗｰ出力メソッド
        /// </summary>
        /// <param name="message"></param>
        public void DetailError(string message)
        {
            // エラーをアグリゲートするメソッドを呼び出す
            _errorAggregator.AddError(new OutputError
            {
                Message = message,
                RecordName = _detail.Detail.DetailName,
                MnemonicId = (int)MnemonicType.ProcessDetail,
                RecordId = _detail.Detail.Id,
                IsCritical = true
            });
        }

    }
}
