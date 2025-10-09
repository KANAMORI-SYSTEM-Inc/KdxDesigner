using Kdx.Contracts.DTOs;
using Kdx.Contracts.DTOs.MnemonicCommon;

using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.Cylinder
{
    internal interface IControlBoxResolver
    {
        // 見つかれば ControlBox、見つからなければ null とエラー
        bool TryResolve(MnemonicDeviceWithCylinder cylinder, out ControlBox controlBox, out OutputError? error);
    }

    internal sealed class ControlBoxResolver : IControlBoxResolver
    {
        private readonly IReadOnlyDictionary<int, CylinderControlBox> _cylinderToCtrl;
        private readonly IReadOnlyDictionary<int, ControlBox> _boxNumberToCtrl;

        public ControlBoxResolver(MainViewModel vm)
        {
            // 初期化時に辞書化（重いLINQの都度実行を回避）
            _cylinderToCtrl = vm._selectedCylinderControlBoxes
                .GroupBy(x => x.CylinderId)
                .ToDictionary(g => g.Key, g => g.First());

            _boxNumberToCtrl = vm._selectedControlBoxes
                .GroupBy(x => x.BoxNumber)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public bool TryResolve(MnemonicDeviceWithCylinder cylinder, out ControlBox controlBox, out OutputError? error)
        {
            error = null;

            if (!_cylinderToCtrl.TryGetValue(cylinder.Cylinder.Id, out var cyCtrl))
            {
                error = OutputErrorFactory.MissingControlPanelForCylinder(cylinder);
                controlBox = NullControlBox.Create(); // Null Object
                return false;
            }

            if (!_boxNumberToCtrl.TryGetValue(cyCtrl.ManualNumber, out var found))
            {
                error = OutputErrorFactory.MissingControlPanelNumber(cylinder, cyCtrl.ManualNumber);
                controlBox = NullControlBox.Create();
                return false;
            }

            controlBox = found;
            return true;
        }
    }

    internal static class NullControlBox
    {
        public static ControlBox Create(string? manualButton = null, string? manualMode = null)
            => new ControlBox
            {
                BoxNumber = -1,
                ManualButton = manualButton ?? "UNASSIGNED",
                ManualMode = manualMode ?? SettingsManager.Settings.AlwaysON
            };
    }

    internal static class OutputErrorFactory
    {
        public static OutputError MissingControlPanelForCylinder(MnemonicDeviceWithCylinder c) => new()
        {
            RecordName = c.Cylinder.CYNum ?? "",
            Message = $"シリンダー '{c.Cylinder.CYNum}' に対応する操作盤が設定されていません。",
            MnemonicId = (int)MnemonicType.CY,
            RecordId = c.Cylinder.Id,
            IsCritical = true
        };

        public static OutputError MissingControlPanelNumber(MnemonicDeviceWithCylinder c, int manualNumber) => new()
        {
            RecordName = c.Cylinder.CYNum ?? "",
            Message = $"操作盤番号 '{manualNumber}' の操作盤が見つかりません。",
            MnemonicId = (int)MnemonicType.CY,
            RecordId = c.Cylinder.Id,
            IsCritical = true
        };
    }

    internal class CylinderFunction
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        private readonly MnemonicDeviceWithCylinder _cylinder;
        private string _label; // ラベルの取得
        private int _startNum; // ラベルの取得
        private string? _speedDevice;
        private ControlBox _controlBox;
        private string _bJogGo;
        private string _bJogBack;
        private string? _wJogGoSpeed;
        private string? _wJogBackSpeed;

        public CylinderFunction(
            MainViewModel mainViewModel,
            IErrorAggregator errorAggregator,
            MnemonicDeviceWithCylinder cylinder,
            IIOAddressService ioAddressService,
            string manualButton,
            string? speedDevice)
        {
            // ---- Guard clauses
            if (mainViewModel is null) throw new ArgumentNullException(nameof(mainViewModel));
            if (errorAggregator is null) throw new ArgumentNullException(nameof(errorAggregator));
            if (cylinder is null) throw new ArgumentNullException(nameof(cylinder));
            if (ioAddressService is null) throw new ArgumentNullException(nameof(ioAddressService));
            if (string.IsNullOrWhiteSpace(manualButton)) throw new ArgumentException("manualButton is required.", nameof(manualButton));

            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
            _cylinder = cylinder;
            _startNum = cylinder.Mnemonic.StartNum;
            _label = cylinder.Mnemonic.DeviceLabel;
            _speedDevice = speedDevice;

            // ---- 操作盤の解決（責務をResolverに委譲）
            var resolver = new ControlBoxResolver(_mainViewModel);
            if (resolver.TryResolve(_cylinder, out var ctrl, out var err))
            {
                _controlBox = ctrl;
            }
            else
            {
                _controlBox = NullControlBox.Create(manualButton);
                if (err != null) _errorAggregator.AddError(err);
            }

            // NullControlBox の場合でも ManualButton/ManualMode を補正
            if (_controlBox.BoxNumber == -1)
            {
                // 最小限のフォールバック
                _controlBox.ManualButton = manualButton;
                _controlBox.ManualMode ??= SettingsManager.Settings.AlwaysON;
            }

            // stKDX_****.Jog.bGo           手動操作行きJOG
            // stKDX_****.Jog.bBack         手動操作帰りJOG
            // stKDX_****.Jog.wGoSpeed      手動操作行き速度
            // stKDX_****.Jog.wBackSpeed    手動操作帰り速度
            string prefix = $"{Label.PREFIX}{cylinder.Cylinder.CYNum}";
            _bJogGo = $"{prefix}.Jog.bGo";
            _bJogBack = $"{prefix}.Jog.bBack";
            _wJogGoSpeed = $"{prefix}.Jog.wGoSpeed";
            _wJogBackSpeed = $"{prefix}.Jog.wBackSpeed";

        }

        public List<LadderCsvRow> GoOperation(List<MnemonicDeviceWithOperation> goOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            // 行き方向自動指令
            foreach (var go in goOperation)
            {
                var operationLabel = go.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = go.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }

            result.Add(LadderRow.AddOUT(_label + (_startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> OffOperation(List<MnemonicDeviceWithOperation> offOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            foreach (var off in offOperation)
            {
                var operationLabel = off.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = off.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }

            if (offOperation.Count != 0)
                result.Add(LadderRow.AddOUT(_label + (_startNum + 9).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> BackOperation(List<MnemonicDeviceWithOperation> backOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            // 行き方向自動指令
            foreach (var back in backOperation)
            {
                var operationLabel = back.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = back.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }

            if (backOperation.Count != 0)
                result.Add(LadderRow.AddOUT(_label + (_startNum + 1).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> GoManualOperation(List<MnemonicDeviceWithOperation> goOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            // 行き方向自動指令
            foreach (var go in goOperation)
            {
                var operationLabel = go.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = go.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(operationLabel + (operationOutcoil + 2).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }

            if (goOperation.Count != 0)
                result.Add(LadderRow.AddOUT(_label + (_startNum + 2).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> BackManualOperation(List<MnemonicDeviceWithOperation> backOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            // 行き方向自動指令
            foreach (var back in backOperation)
            {
                var operationLabel = back.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = back.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(operationLabel + (operationOutcoil + 2).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }
            if (backOperation.Count != 0)
                result.Add(LadderRow.AddOUT(_label + (_startNum + 3).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> OutputRetention()
        {
            List<LadderCsvRow> result = new();

            // 行き方向自動保持
            result.Add(LadderRow.AddLDP(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddORP(_label + (_startNum + 2).ToString()));
            result.Add(LadderRow.AddSET(_label + (_startNum + 5).ToString()));

            // 帰り方向自動保持
            result.Add(LadderRow.AddLDP(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddORP(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddORP(_label + (_startNum + 4).ToString()));
            result.Add(LadderRow.AddSET(_label + (_startNum + 6).ToString()));

            // 行き方向自動保持
            result.Add(LadderRow.AddLDP(_label + (_startNum + 6).ToString()));
            result.Add(LadderRow.AddORP(SettingsManager.Settings.SoftResetSignal));
            result.Add(LadderRow.AddRST(_label + (_startNum + 5).ToString()));

            // 帰り方向自動保持
            result.Add(LadderRow.AddLDP(_label + (_startNum + 5).ToString()));
            result.Add(LadderRow.AddORP(SettingsManager.Settings.SoftResetSignal));
            result.Add(LadderRow.AddRST(_label + (_startNum + 6).ToString()));

            return result;
        }

        public List<LadderCsvRow> CyclePulse()
        {
            List<LadderCsvRow> result = new();
            var cylinderCycles = _mainViewModel._selectedCylinderCycles;

            if (cylinderCycles != null
                || cylinderCycles!.Count != 0)
            {
                // ★ 1. Split と int.Parse を安全に行う
                List<int> startCycles = cylinderCycles
                    .Where(cc => cc.CylinderId == _cylinder.Cylinder.Id) // 変換に失敗した(0になった)要素を除外
                    .Select(cc => cc.CycleId)
                    .ToList();

                // ★ 2. isFirst のロジックを foreach ループの外側で扱う方がシンプル
                bool isFirstCycleInLoop = true;
                foreach (var startCycleId in startCycles)
                {
                    // 各サイクルIDに対して処理を行う  
                    var eachCycle = _mainViewModel.Cycles.FirstOrDefault(c => c.Id == startCycleId);
                    if (eachCycle != null)
                    {
                        if (!isFirstCycleInLoop)
                        {
                            // 2つ目以降のサイクルの場合はORBを追加
                            result.Add(LadderRow.AddORB());
                        }

                        // Cycleに関連する処理をここに追加
                        result.Add(LadderRow.AddLDP(eachCycle.StartDevice));
                        result.Add(LadderRow.AddANI(_label + (_startNum + 0).ToString()));
                        result.Add(LadderRow.AddANI(_label + (_startNum + 1).ToString()));

                        isFirstCycleInLoop = false; // フラグを更新
                    }
                }

                // ★ 3. ループで有効なサイクルが1つでも処理された場合にのみ、PLSを出力
                if (!isFirstCycleInLoop)
                {
                    result.Add(LadderRow.AddPLS(_label + (_startNum + 4).ToString()));
                }
            }

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> Excitation(List<IO> sensors)
        {
            List<LadderCsvRow> result = new();
            result.Add(LadderRow.AddLDI(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 2).ToString()));
            result.Add(LadderRow.AddAND(_label + (_startNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 19).ToString()));

            result.Add(LadderRow.AddLDI(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddAND(_label + (_startNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 20).ToString()));

            return result;
        }

        public List<LadderCsvRow> Retention(List<IO> sensors, string cycleDevice)
        {
            // ■■■ 1. ヘルパーメソッドを使って、Go/Backセンサーのアドレスリストをシンプルに取得 ■■■
            int? goSensorCount = int.TryParse(_cylinder.Cylinder.GoSensorCount, out int goCount) ? goCount : (int?)null;
            int? backSensorCount = int.TryParse(_cylinder.Cylinder.BackSensorCount, out int backCount) ? backCount : (int?)null;

            var goSensorAddresses = GetSensorAddresses(sensors, _cylinder.Cylinder.RetentionSensorGo, goSensorCount, false); // isOutput: false
            var backSensorAddresses = GetSensorAddresses(sensors, _cylinder.Cylinder.RetentionSensorBack, backSensorCount, false); // isOutput: false

            // ■■■ 2. ラダーロジックの生成 ■■■
            var result = new List<LadderCsvRow>();

            // --- 保持出力 行き (内部リレー _startNum + 19) ---
            result.Add(LadderRow.AddLDI(_label + (_startNum + 0)));
            result.Add(LadderRow.AddANI(_label + (_startNum + 2)));

            if (goSensorAddresses.Any())
            {
                // 見つかった全てのGoセンサーをAND条件で直列に接続
                foreach (var address in goSensorAddresses)
                {
                    result.Add(LadderRow.AddAND(address));
                }
            }
            else
            {
                // ★ センサーが見つからなかった場合のエラーチェックを簡素化
                // (設定が "null" 文字列でない場合にのみエラーとする)
                if (!string.IsNullOrEmpty(_cylinder.Cylinder.RetentionSensorGo) &&
                    !_cylinder.Cylinder.RetentionSensorGo.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    _errorAggregator.AddError(new OutputError
                    {
                        Message = $"保持条件に必要な行き方向センサー '{_cylinder.Cylinder.RetentionSensorGo}' がIOリストに見つかりませんでした。",
                        RecordName = _cylinder.Cylinder.CYNum,
                        RecordId = _cylinder.Cylinder.Id
                    });
                }
            }
            result.Add(LadderRow.AddAND(_label + (_startNum + 5)));
            result.Add(LadderRow.AddAND(cycleDevice));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 19)));


            // --- 保持出力 帰り (内部リレー _startNum + 20) ---
            result.Add(LadderRow.AddLDI(_label + (_startNum + 1)));
            result.Add(LadderRow.AddANI(_label + (_startNum + 3)));

            if (backSensorAddresses.Any())
            {
                // 見つかった全てのBackセンサーをAND条件で直列に接続
                foreach (var address in backSensorAddresses)
                {
                    result.Add(LadderRow.AddAND(address));
                }
            }
            else
            {
                // ★ センサーが見つからなかった場合のエラーチェックを簡素化
                if (!string.IsNullOrEmpty(_cylinder.Cylinder.RetentionSensorBack) &&
                    !_cylinder.Cylinder.RetentionSensorBack.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    _errorAggregator.AddError(new OutputError
                    {
                        Message = $"保持条件に必要な帰り方向センサー '{_cylinder.Cylinder.RetentionSensorBack}' がIOリストに見つかりませんでした。",
                        RecordName = _cylinder.Cylinder.CYNum,
                        RecordId = _cylinder.Cylinder.Id
                    });
                }
            }
            result.Add(LadderRow.AddAND(_label + (_startNum + 6)));
            result.Add(LadderRow.AddAND(cycleDevice));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 20)));

            // 行きチェック
            if (goSensorAddresses.Any())
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 10).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 12).ToString()));
                foreach (var address in goSensorAddresses)
                {
                    result.Add(LadderRow.AddANI(address));
                }
                result.Add(LadderRow.AddOUT(_label + (_startNum + 48).ToString()));
            }

            // 帰りﾁｪｯｸ
            if (backSensorAddresses.Any())
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 11).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 13).ToString()));
                foreach (var address in backSensorAddresses)
                {
                    result.Add(LadderRow.AddANI(address));
                }
                result.Add(LadderRow.AddOUT(_label + (_startNum + 49).ToString()));
            }

            return result;
        }

        public List<LadderCsvRow> RetentionFlow(List<IO> sensors)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト

            // センサーの取得
            var goSensor = _ioAddressService.GetSingleAddress(sensors, _cylinder.Cylinder.RetentionSensorGo ?? string.Empty, false, _cylinder.Cylinder.CYNum!, _cylinder.Cylinder.Id, null);
            var backSensor = _ioAddressService.GetSingleAddress(sensors, _cylinder.Cylinder.RetentionSensorBack ?? string.Empty, false, _cylinder.Cylinder.CYNum!, _cylinder.Cylinder.Id, null);

            result.Add(LadderRow.AddLDI(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 2).ToString()));
            if (goSensor != null)
            {
                result.Add(LadderRow.AddAND(goSensor));
            }
            else
            {
                result.Add(LadderRow.AddANI(SettingsManager.Settings.AlwaysON));
            }
            result.Add(LadderRow.AddAND(_label + (_startNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 19).ToString()));

            if (_speedDevice != null)
            {
                result.AddRange(LadderRow.AddMOVSet("K1", _speedDevice)); // スピードデバイスの設定
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    IsCritical = false
                });
            }


            // 保持出力行き
            result.Add(LadderRow.AddLDI(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 3).ToString()));
            if (backSensor != null)
            {
                result.Add(LadderRow.AddAND(backSensor));
            }
            else
            {
                result.Add(LadderRow.AddANI(SettingsManager.Settings.AlwaysON));
            }
            result.Add(LadderRow.AddAND(_label + (_startNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 20).ToString()));

            if (_speedDevice != null)
            {

                switch (_cylinder.Cylinder.FlowType)
                {
                    case "A5:B5":
                        result.AddRange(LadderRow.AddMOVSet("K6", _speedDevice)); // スピードデバイスの設定
                        break;
                    case "A6:B4":
                        result.AddRange(LadderRow.AddMOVSet("K7", _speedDevice)); // スピードデバイスの設定
                        break;
                    case "A10:B0":
                        result.AddRange(LadderRow.AddMOVSet("K1", _speedDevice)); // スピードデバイスの設定
                        break;
                    default:
                        result.AddRange(LadderRow.AddMOVSet("K6", _speedDevice)); // スピードデバイスの設定
                        break;
                }
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    IsCritical = false
                });
            }


            // 行きチェック
            if (goSensor != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 10).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 12).ToString()));
                result.Add(LadderRow.AddANI(goSensor));
                result.Add(LadderRow.AddOUT(_label + (_startNum + 48).ToString()));
            }

            // 帰りﾁｪｯｸ
            if (backSensor != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 11).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 13).ToString()));
                result.Add(LadderRow.AddANI(backSensor));
                result.Add(LadderRow.AddOUT(_label + (_startNum + 49).ToString()));
            }


            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> ManualReset()
        {
            List<LadderCsvRow> result = new();

            // 操作盤「実行」ボタンと「各個」モードの対応デバイス
            // 「実行」ボタンを離した時と、「各個」がOFFになったときにリセットする。
            result.Add(LadderRow.AddLDF(_controlBox.ManualButton));
            result.Add(LadderRow.AddLDF(_controlBox.ManualMode));

            // JOGスイッチのリセット処理
            result.Add(LadderRow.AddRST(_bJogGo));
            result.Add(LadderRow.AddRST(_bJogBack));

            return result;

        }

        public List<LadderCsvRow> ManualButton()
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト

            // JOG
            result.Add(LadderRow.AddLD(_bJogGo));
            // マニュアル出力
            result.Add(LadderRow.AddOR(_label + (_startNum + 2).ToString()));
            result.Add(LadderRow.AddAND(_controlBox.ManualButton));
            result.Add(LadderRow.AddANI(_label + (_startNum + 9).ToString()));

            result.Add(LadderRow.AddOUT(_label + (_startNum + 10).ToString()));

            // 保持出力
            result.Add(LadderRow.AddLD(_label + (_startNum + 19).ToString()));
            // 自動出力
            result.Add(LadderRow.AddOR(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(_controlBox.ManualButton));
            result.Add(LadderRow.AddANI(_label + (_startNum + 9).ToString()));

            result.Add(LadderRow.AddOUT(_label + (_startNum + 12).ToString()));

            // JOG
            result.Add(LadderRow.AddLD(_bJogBack));
            // マニュアル出力
            result.Add(LadderRow.AddOR(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddAND(_controlBox.ManualButton));
            result.Add(LadderRow.AddANI(_label + (_startNum + 9).ToString()));

            result.Add(LadderRow.AddOUT(_label + (_startNum + 11).ToString()));

            // 保持出力
            result.Add(LadderRow.AddLD(_label + (_startNum + 20).ToString()));
            // 自動出力
            result.Add(LadderRow.AddOR(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_controlBox.ManualButton));
            result.Add(LadderRow.AddANI(_label + (_startNum + 9).ToString()));

            result.Add(LadderRow.AddOUT(_label + (_startNum + 13).ToString()));

            // アウトコイル
            result.Add(LadderRow.AddLD(_label + (_startNum + 10).ToString()));
            result.Add(LadderRow.AddOR(_label + (_startNum + 11).ToString()));
            result.Add(LadderRow.AddOR(_label + (_startNum + 12).ToString()));
            result.Add(LadderRow.AddOR(_label + (_startNum + 13).ToString()));

            result.Add(LadderRow.AddOUT(_label + (_startNum + 14).ToString()));

            return result;
        }

        public List<LadderCsvRow> FlowOperate()
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト

            if (_speedDevice == null)
            {
                _errorAggregator.AddError(new OutputError
                {
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    IsCritical = false
                });
                return result; // スピードデバイスがない場合は空のリストを返す
            }

            for (int i = 1; i < 11; i++)
            {
                result.AddRange(LadderRow.AddLDE(_speedDevice, ("K" + i.ToString())));
                result.Add(LadderRow.AddAND(_label + (_startNum + 14).ToString()));
                result.Add(LadderRow.AddOUT(_label + (_startNum + (i + 20)).ToString()));
            }

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> ILOK()
        {
            var result = new List<LadderCsvRow>();

            // 行きOK
            result.Add(LadderRow.AddLD(_label + (_startNum + 10).ToString()));
            result.Add(LadderRow.AddAND(_label + (_startNum + 17).ToString()));
            result.Add(LadderRow.AddLD(_label + (_startNum + 12).ToString()));
            result.Add(LadderRow.AddAND(_label + (_startNum + 15).ToString()));
            result.Add(LadderRow.AddORB());

            result.Add(LadderRow.AddANI(_label + (_startNum + 9).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 35)));

            // 帰りOK
            result.Add(LadderRow.AddLD(_label + (_startNum + 11).ToString()));
            result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
            result.Add(LadderRow.AddLD(_label + (_startNum + 13).ToString()));
            result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));
            result.Add(LadderRow.AddORB());
            result.Add(LadderRow.AddOUT(_label + (_startNum + 36)));

            // 指令OK
            result.Add(LadderRow.AddLD(_label + (_startNum + 35)));
            result.Add(LadderRow.AddOR(_label + (_startNum + 36)));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 37)));

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> SingleValve(List<IO> sensors, int? multiSensorCount)
        {
            var result = new List<LadderCsvRow>();
            string valveSearchString = SettingsManager.Settings.ValveSearchWord;

            // 行き方向のバルブアドレスをリストとして保持する
            var goValveAddresses = new List<string>();

            // multiSensorCount の有無でバルブの検索方法を分岐
            if (multiSensorCount.HasValue && multiSensorCount.Value > 0)
            {
                // multiSensorCountが指定されている場合： "G1", "G2"... のように複数のバルブを検索
                for (int i = 1; i <= multiSensorCount.Value; i++)
                {
                    // シリンダ設定のGoサフィックス（例: "G"）と連番で検索文字列を作成
                    string searchName = _cylinder.Cylinder.Go + i;
                    var foundValve = _ioAddressService.GetSingleAddress(
                        sensors,
                        searchName,
                        true, // errorIfNotFound
                        _cylinder.Cylinder.CYNum!,
                        _cylinder.Cylinder.Id,
                        null);

                    if (foundValve != null)
                    {
                        goValveAddresses.Add(foundValve);
                    }
                    // 見つからない場合のエラーは GetSingleAddress 内で記録される
                }
            }
            else
            {
                // multiSensorCountが指定されていない場合： 従来のロジックで単一のバルブを検索
                string? goValve = _cylinder.Cylinder.CYNameSub != null
                    ? _ioAddressService.GetSingleAddress(
                        sensors,
                        _cylinder.Cylinder.Go + _cylinder.Cylinder.CYNameSub,
                        true, // errorIfNotFound
                        _cylinder.Cylinder.CYNum!,
                        _cylinder.Cylinder.Id,
                        null)
                    : _ioAddressService.GetSingleAddress(
                        sensors,
                        valveSearchString,
                        true, // errorIfNotFound
                        _cylinder.Cylinder.CYNum!,
                        _cylinder.Cylinder.Id,
                        null);
                if (goValve != null)
                {
                    goValveAddresses.Add(goValve);
                }
                // 見つからない場合のエラーは GetSingleAddress 内で記録される
            }

            // 片ソレノイドのため、帰り方向は内部リレーをONする
            result.Add(LadderRow.AddLD(_label + (_startNum + 13).ToString()));
            result.Add(LadderRow.AddANI(_controlBox.ManualButton));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));

            result.Add(LadderRow.AddLD(_label + (_startNum + 11).ToString()));
            result.Add(LadderRow.AddAND(_controlBox.ManualButton));
            result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
            result.Add(LadderRow.AddORB());
            // 出力は内部リレー
            result.Add(LadderRow.AddOUT(_label + (_startNum + 45).ToString()));

            // ■■■ 行き方向のバルブ出力 (複数対応) ■■■
            if (goValveAddresses.Any()) // 行き方向のバルブが1つでも見つかっていれば
            {
                // --- 条件ブロック (共通) ---
                result.Add(LadderRow.AddLD(_label + (_startNum + 12).ToString()));
                result.Add(LadderRow.AddANI(_controlBox.ManualButton));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 15).ToString()));

                result.Add(LadderRow.AddLD(_label + (_startNum + 10).ToString()));
                result.Add(LadderRow.AddAND(_controlBox.ManualButton));
                result.Add(LadderRow.AddAND(_label + (_startNum + 17).ToString()));
                result.Add(LadderRow.AddORB());

                // 帰り用内部リレーとのインターロック
                result.Add(LadderRow.AddANI(_label + (_startNum + 45).ToString()));

                // --- 出力ブロック (見つかったアドレスの数だけOUTを並列で追加) ---
                foreach (var goAddress in goValveAddresses)
                {
                    result.Add(LadderRow.AddOUT(goAddress));
                }
            }
            else
            {
                // goValveAddressesが空の場合、GetSingleAddressが既にエラーを記録しているため
                // ここでの追加エラーは不要かもしれないが、念のため残す場合は以下のようにする
                _errorAggregator.AddError(new OutputError
                {
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"行き方向のバルブが見つかりませんでした。検索文字列: '{_cylinder.Cylinder.Go}' など",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id
                });
            }

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> DoubleValve(List<IO> sensors, int? multiSensorCount)
        {
            var result = new List<LadderCsvRow>();

            // 1. GetAddressRange を使って、"SV" を含むすべてのバルブ候補を取得
            string valveSearchString = SettingsManager.Settings.ValveSearchWord;

            var valveCandidates = _ioAddressService.GetAddressRange(sensors, valveSearchString ?? "SV", _cylinder.Cylinder.CYNum!, _cylinder.Cylinder.Id, errorIfNotFound: true);

            // 2. ダブルバルブには最低2つの候補が必要なため、候補数をチェック
            if (valveCandidates.Count < 2)
            {
                _errorAggregator.AddError(new OutputError
                {
                    Message = $"ダブルバルブを特定できません。バルブ検索文字列 '{valveSearchString}' に一致するIOが2件未満です。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    RecordName = _cylinder.Cylinder.CYNum ?? ""
                });
                // 候補が足りない場合でも、個別のエラーを報告するために処理は続行される想定
            }

            // 3. ヘルパーメソッドを使い、Go/Backバルブの "アドレスリスト" をそれぞれ検索
            var goValveAddresses = FindMultipleValveAddresses(valveCandidates, _cylinder.Cylinder.Go, multiSensorCount, "前進 (Go)");
            var backValveAddresses = FindMultipleValveAddresses(valveCandidates, _cylinder.Cylinder.Back, multiSensorCount, "後退 (Back)");

            // 4. 見つかったアドレスに基づいてラダーを生成

            // ■■■ 行き方向のバルブ出力 ■■■
            if (goValveAddresses.Any()) // 見つかったアドレスが1つ以上あれば
            {
                // --- 条件ブロック (このブロックは共通) ---
                result.Add(LadderRow.AddLD(_label + (_startNum + 12).ToString()));
                result.Add(LadderRow.AddANI(_controlBox.ManualButton));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

                result.Add(LadderRow.AddAND(_label + (_startNum + 15).ToString()));

                result.Add(LadderRow.AddLD(_label + (_startNum + 10).ToString()));
                result.Add(LadderRow.AddAND(_controlBox.ManualButton));
                result.Add(LadderRow.AddAND(_label + (_startNum + 17).ToString()));
                result.Add(LadderRow.AddORB());

                // backValveAddress が見つかっている場合、それとANDNでインターロックを組む
                // 複数ある場合は最初の1つで代表してインターロック
                if (backValveAddresses.Any())
                {
                    result.Add(LadderRow.AddANI(backValveAddresses.First()));
                }

                // --- 出力ブロック (見つかったアドレスの数だけOUTを並列で追加) ---
                foreach (var goAddress in goValveAddresses)
                {
                    result.Add(LadderRow.AddOUT(goAddress));
                }
            }
            // elseの場合、エラーはFindMultipleValveAddresses内で記録済み

            // ■■■ 帰り方向のバルブ出力 ■■■
            if (backValveAddresses.Any()) // 見つかったアドレスが1つ以上あれば
            {
                // --- 条件ブロック (このブロックは共通) ---
                result.Add(LadderRow.AddLD(_label + (_startNum + 13).ToString()));
                result.Add(LadderRow.AddANI(_controlBox.ManualButton));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

                result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));

                result.Add(LadderRow.AddLD(_label + (_startNum + 11).ToString()));
                result.Add(LadderRow.AddAND(_controlBox.ManualButton));
                result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
                result.Add(LadderRow.AddORB());

                // goValveAddress が見つかっている場合、それとANDNでインターロックを組む
                // 複数ある場合は最初の1つで代表してインターロック
                if (goValveAddresses.Any())
                {
                    result.Add(LadderRow.AddANI(goValveAddresses.First()));
                }

                // --- 出力ブロック (見つかったアドレスの数だけOUTを並列で追加) ---
                foreach (var backAddress in backValveAddresses)
                {
                    result.Add(LadderRow.AddOUT(backAddress));
                }
            }
            // elseの場合、エラーはFindMultipleValveAddresses内で記録済み

            return result;
        }

        public List<LadderCsvRow> Motor(List<IO> sensors, int? multiSensorCount)
        {
            var result = new List<LadderCsvRow>();
            string valveSearchString = "CM-";

            // 行き方向のバルブアドレスをリストとして保持する
            var goValveAddresses = new List<string>();

            // multiSensorCount の有無でバルブの検索方法を分岐
            if (multiSensorCount.HasValue && multiSensorCount.Value > 0)
            {
                // multiSensorCountが指定されている場合： "G1", "G2"... のように複数のバルブを検索
                for (int i = 1; i <= multiSensorCount.Value; i++)
                {
                    // シリンダ設定のGoサフィックス（例: "G"）と連番で検索文字列を作成
                    string searchName = _cylinder.Cylinder.Go + i;
                    var foundValve = _ioAddressService.GetSingleAddress(
                        sensors,
                        searchName,
                        true, // errorIfNotFound
                        _cylinder.Cylinder.CYNum!,
                        _cylinder.Cylinder.Id,
                        null);

                    if (foundValve != null)
                    {
                        goValveAddresses.Add(foundValve);
                    }
                    // 見つからない場合のエラーは GetSingleAddress 内で記録される
                }
            }
            else
            {
                // multiSensorCountが指定されていない場合： 従来のロジックで単一のバルブを検索
                string? goValve = _cylinder.Cylinder.CYNameSub != null
                    ? _ioAddressService.GetSingleAddress(
                        sensors,
                        _cylinder.Cylinder.Go + _cylinder.Cylinder.CYNameSub,
                        true, // errorIfNotFound
                        _cylinder.Cylinder.CYNum!,
                        _cylinder.Cylinder.Id,
                        null)
                    : _ioAddressService.GetSingleAddress(
                        sensors,
                        valveSearchString,
                        true, // errorIfNotFound
                        _cylinder.Cylinder.CYNum!,
                        _cylinder.Cylinder.Id,
                        null);
                if (goValve != null)
                {
                    goValveAddresses.Add(goValve);
                }
                // 見つからない場合のエラーは GetSingleAddress 内で記録される
            }

            // 片ソレノイドのため、帰り方向は内部リレーをONする
            result.Add(LadderRow.AddLD(_label + (_startNum + 13).ToString()));
            result.Add(LadderRow.AddANI(_controlBox.ManualButton));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 15).ToString()));

            result.Add(LadderRow.AddLD(_label + (_startNum + 11).ToString()));
            result.Add(LadderRow.AddAND(_controlBox.ManualButton));
            result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
            result.Add(LadderRow.AddORB());
            // 出力は内部リレー
            result.Add(LadderRow.AddOUT(_label + (_startNum + 45).ToString()));

            // ■■■ 行き方向のバルブ出力 (複数対応) ■■■
            if (goValveAddresses.Any()) // 行き方向のバルブが1つでも見つかっていれば
            {
                // --- 条件ブロック (共通) ---
                result.Add(LadderRow.AddLD(_label + (_startNum + 12).ToString()));
                result.Add(LadderRow.AddANI(_controlBox.ManualButton));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 14).ToString()));

                result.Add(LadderRow.AddLD(_label + (_startNum + 10).ToString()));
                result.Add(LadderRow.AddAND(_controlBox.ManualButton));
                result.Add(LadderRow.AddAND(_label + (_startNum + 17).ToString()));
                result.Add(LadderRow.AddORB());

                // 帰り用内部リレーとのインターロック
                result.Add(LadderRow.AddANI(_label + (_startNum + 45).ToString()));

                // --- 出力ブロック (見つかったアドレスの数だけOUTを並列で追加) ---
                foreach (var goAddress in goValveAddresses)
                {
                    result.Add(LadderRow.AddOUT(goAddress));
                }
            }
            else
            {
                // goValveAddressesが空の場合、GetSingleAddressが既にエラーを記録しているため
                // ここでの追加エラーは不要かもしれないが、念のため残す場合は以下のようにする
                _errorAggregator.AddError(new OutputError
                {
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"行き方向のバルブが見つかりませんでした。検索文字列: '{_cylinder.Cylinder.Go}' など",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id
                });
            }

            return result; // 生成されたLadderCsvRowのリストを返す
        }


        public List<LadderCsvRow> FlowValve(List<IO> sensors, string speedDevice)
        {
            var result = new List<LadderCsvRow>();

            string cyNum = _cylinder.Cylinder.CYNum ?? ""; // シリンダー名の取得  
            string cyNumSub = _cylinder.Cylinder.CYNameSub.ToString() ?? ""; // シリンダー名の取得  
            string cyName = cyNum + cyNumSub; // シリンダー名の組み合わせ  

            var stpIO = _ioAddressService.GetSingleAddress(sensors, "STP", true, cyNum + cyName, _cylinder.Cylinder.Id, null);
            var in1IO = _ioAddressService.GetSingleAddress(sensors, "IN1", true, cyNum + cyName, _cylinder.Cylinder.Id, null);
            var in2IO = _ioAddressService.GetSingleAddress(sensors, "IN2", true, cyNum + cyName, _cylinder.Cylinder.Id, null);
            var in3IO = _ioAddressService.GetSingleAddress(sensors, "IN3", true, cyNum + cyName, _cylinder.Cylinder.Id, null);
            var in4IO = _ioAddressService.GetSingleAddress(sensors, "IN4", true, cyNum + cyName, _cylinder.Cylinder.Id, null);
            var in5IO = _ioAddressService.GetSingleAddress(sensors, "IN5", true, cyNum + cyName, _cylinder.Cylinder.Id, null);
            var in6IO = _ioAddressService.GetSingleAddress(sensors, "IN6", true, cyNum + cyName, _cylinder.Cylinder.Id, null);


            if (in1IO != null)
            {
                result.AddRange(LadderRow.AddLDG(speedDevice, "K5"));
                result.AddRange(LadderRow.AddANDN(speedDevice, "K0"));
                result.Add(LadderRow.AddAND(_label + (_startNum + 37).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddOUT(in1IO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    RecordName = _cylinder.Cylinder.CYNum,
                    Message = $"CY{_cylinder.Cylinder.CYNum}のIN1 IOが見つかりません。",
                });
            }

            if (in2IO != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 21).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddOR(_label + (_startNum + 26).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(_label + (_startNum + 37).ToString())); // ラベルのLD命令を追加

                result.Add(LadderRow.AddOUT(in2IO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    RecordName = _cylinder.Cylinder.CYNum,
                    Message = $"CY{_cylinder.Cylinder.CYNum}のIN2 IOが見つかりません。",
                });
            }

            if (in3IO != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 22).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddOR(_label + (_startNum + 27).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(_label + (_startNum + 37).ToString())); // ラベルのLD命令を追加

                result.Add(LadderRow.AddOUT(in3IO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    RecordName = _cylinder.Cylinder.CYNum,
                    Message = $"CY{_cylinder.Cylinder.CYNum}のIN3 IOが見つかりません。",
                });
            }

            if (in4IO != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 23).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddOR(_label + (_startNum + 28).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(_label + (_startNum + 37).ToString())); // ラベルのLD命令を追加

                result.Add(LadderRow.AddOUT(in4IO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    RecordName = _cylinder.Cylinder.CYNum,
                    Message = $"CY{_cylinder.Cylinder.CYNum}のIN4 IOが見つかりません。",
                });
            }

            if (in5IO != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 24).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddOR(_label + (_startNum + 29).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(_label + (_startNum + 37).ToString())); // ラベルのLD命令を追加

                result.Add(LadderRow.AddOUT(in5IO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    RecordName = _cylinder.Cylinder.CYNum,
                    Message = $"CY{_cylinder.Cylinder.CYNum}のIN5 IOが見つかりません。",
                });
            }

            if (in6IO != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 25).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddOR(_label + (_startNum + 30).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(_label + (_startNum + 37).ToString())); // ラベルのLD命令を追加

                result.Add(LadderRow.AddOUT(in6IO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    RecordName = _cylinder.Cylinder.CYNum,
                    Message = $"CY{_cylinder.Cylinder.CYNum}のIN6 IOが見つかりません。",
                });
            }

            return result;
        }

        /// <summary>
        /// 設定されたサフィックスに基づき、バルブのアドレスを検索します。
        /// multiSensorCountが指定されている場合は、複数のバルブを検索します。
        /// </summary>
        /// <param name="valveCandidates">検索対象のIOリスト</param>
        /// <param name="configuredBaseSuffix">設定ファイル上のバルブサフィックスのベース部分 (例: "G", "B")</param>
        /// <param name="multiSensorCount">多連バルブの数。nullの場合は単一バルブとして検索</param>
        /// <param name="valveTypeForErrorMessage">エラーメッセージに表示するバルブの種類 (例: "前進 (Go)")</param>
        /// <returns>見つかったバルブアドレスのリスト</returns>
        private List<string> FindMultipleValveAddresses(
            List<IO> valveCandidates,
            string? configuredBaseSuffix,
            int? multiSensorCount,
            string valveTypeForErrorMessage)
        {
            var foundAddresses = new List<string>();

            // 1. シリンダにバルブの設定自体が存在するかチェック
            if (string.IsNullOrEmpty(configuredBaseSuffix))
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    Message = $"シリンダ「{_cylinder.Cylinder.CYNum}」の{valveTypeForErrorMessage}バルブ出力先が設定されていません。",
                    RecordName = _cylinder.Cylinder.CYNum ?? ""
                });
                return foundAddresses; // 空のリストを返す
            }

            // multiSensorCount に値がある場合は、"G1", "G2"... のように探す
            if (multiSensorCount.HasValue && multiSensorCount.Value > 0)
            {
                for (int i = 1; i <= multiSensorCount.Value; i++)
                {
                    string searchSuffix = configuredBaseSuffix + i;
                    var foundValve = valveCandidates
                        .FirstOrDefault(m => m.IOName != null && m.IOName.EndsWith(searchSuffix));

                    if (foundValve != null)
                    {
                        foundAddresses.Add(!string.IsNullOrEmpty(foundValve.LinkDevice)
                    ? foundValve.LinkDevice
                    : foundValve.Address!);
                    }
                    else
                    {
                        // 見つからなかった場合はエラーを記録
                        _errorAggregator.AddError(new OutputError
                        {
                            MnemonicId = (int)MnemonicType.CY,
                            RecordId = _cylinder.Cylinder.Id,
                            Message = $"IOリスト内に、設定された{valveTypeForErrorMessage}バルブ '{searchSuffix}' が見つかりませんでした。",
                            RecordName = _cylinder.Cylinder.CYNum ?? ""
                        });
                    }
                }
            }
            else // multiSensorCount が null の場合は、従来通り単一のバルブを探す
            {
                var foundValve = valveCandidates
                    .FirstOrDefault(m => m.IOName != null && m.IOName.EndsWith(configuredBaseSuffix));

                if (foundValve != null)
                {
                    foundAddresses.Add(!string.IsNullOrEmpty(foundValve.LinkDevice)
                ? foundValve.LinkDevice
                : foundValve.Address!);
                }
                else
                {
                    _errorAggregator.AddError(new OutputError
                    {
                        MnemonicId = (int)MnemonicType.CY,
                        RecordId = _cylinder.Cylinder.Id,
                        Message = $"IOリスト内に、設定された{valveTypeForErrorMessage}バルブ '{configuredBaseSuffix}' が見つかりませんでした。",
                        RecordName = _cylinder.Cylinder.CYNum ?? ""
                    });
                }
            }

            return foundAddresses;
        }

        /// <summary>
        /// 設定に基づき、単一または複数のセンサーアドレスのリストを取得します。
        /// </summary>
        /// <param name="sensors">検索対象のIOリスト。</param>
        /// <param name="sensorName">検索するセンサー名 (例: "G", "B")。</param>
        /// <param name="sensorCount">複数検索する場合のセンサー数。単一の場合はnull。</param>
        /// <param name="isOutput">検索対象がY(出力)デバイスかどうか。</param>
        /// <returns>見つかったセンサーアドレスのリスト。</returns>
        private List<string> GetSensorAddresses(List<IO> sensors, string? sensorName, int? sensorCount, bool isOutput)
        {
            var foundAddresses = new List<string>();

            if (string.IsNullOrEmpty(sensorName) || sensorName.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return foundAddresses;
            }

            if (sensorCount.HasValue && sensorCount.Value > 0)
            {
                for (int i = 1; i <= sensorCount.Value; i++)
                {
                    string searchName = sensorName + i;

                    // ★★★ 修正箇所 ★★★
                    // isOutput とエラー報告用のコンテキスト情報を引数に追加
                    var sensorAddress = _ioAddressService.GetSingleAddress(
                        sensors,
                        searchName,
                        isOutput,
                        _cylinder.Cylinder.CYNum!, // RecordName
                        _cylinder.Cylinder.Id,     // RecordId
                        null                       // isnotInclude (今回は不要なのでnull)
                    );

                    if (sensorAddress != null)
                    {
                        foundAddresses.Add(sensorAddress);
                    }
                }
            }
            else
            {
                // ★★★ 修正箇所 ★★★
                var sensorAddress = _ioAddressService.GetSingleAddress(
                    sensors,
                    sensorName,
                    isOutput,
                    _cylinder.Cylinder.CYNum!,
                    _cylinder.Cylinder.Id,
                    null
                );

                if (sensorAddress != null)
                {
                    foundAddresses.Add(sensorAddress);
                }
            }

            return foundAddresses;
        }


    }
}
