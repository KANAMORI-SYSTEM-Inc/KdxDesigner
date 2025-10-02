using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using System.Diagnostics;

using KdxDesigner.ViewModels;

using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.Services.MemonicTimerDevice
{
    /// <summary>
    /// MnemonicTimerDeviceのデータ操作を行うサービスクラス
    /// </summary>
    public class MnemonicTimerDeviceService : IMnemonicTimerDeviceService
    {
        private readonly string _connectionString;
        private readonly MainViewModel _mainViewModel;
        private readonly IAccessRepository _repository;

        /// <summary>
        /// MnemonicTimerDeviceServiceのコンストラクタ
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="mainViewModel"></param>
        public MnemonicTimerDeviceService(
            IAccessRepository repository,
            MainViewModel mainViewModel)
        {
            _connectionString = repository.ConnectionString;
            _mainViewModel = mainViewModel;
            _repository = repository;
        }

        /// <summary>
        /// PlcIdとCycleIdに基づいてMnemonicTimerDeviceを取得するヘルパーメソッド
        /// </summary>
        /// <param name="plcId"></param>
        /// <param name="cycleId"></param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        public List<MnemonicTimerDevice> GetMnemonicTimerDevice(int plcId, int cycleId)
        {
            var mnemonicTimerDevice = _repository.GetMnemonicTimerDevicesByCycleId(plcId, cycleId);
            return mnemonicTimerDevice;
        }

        /// <summary>
        /// MnemonicTimerDeviceをPlcIdとMnemonicIdで取得するヘルパーメソッド
        /// </summary>
        /// <param name="plcId">PlcId</param>
        /// <param name="cycleId">CycleId</param>
        /// <param name="mnemonicId">MnemonicId</param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByCycle(int plcId, int cycleId, int mnemonicId)
        {
            var mnemonicTimerDevice = _repository.GetMnemonicTimerDevicesByCycleAndMnemonicId(plcId, cycleId, mnemonicId);
            return mnemonicTimerDevice;
        }

        /// <summary>
        /// MnemonicTimerDeviceをPlcIdとMnemonicIdで取得するヘルパーメソッド
        /// </summary>
        /// <param name="plcId">PlcId</param>
        /// <param name="mnemonicId">MnemonicId</param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByMnemonic(int plcId, int mnemonicId)
        {
            Debug.WriteLine($"[GetMnemonicTimerDeviceByMnemonic] 開始 - plcId: {plcId}, mnemonicId: {mnemonicId}");
            var mnemonicTimerDevice = _repository.GetMnemonicTimerDevicesByMnemonicId(plcId, mnemonicId);
            Debug.WriteLine($"[GetMnemonicTimerDeviceByMnemonic] 取得完了 - {mnemonicTimerDevice.Count}件");
            return mnemonicTimerDevice;
        }

        /// <summary>
        /// MnemonicTimerDeviceをPlcIdとTimerIdで取得するヘルパーメソッド
        /// </summary>
        /// <param name="plcId">PlcId</param>
        /// <param name="timerId">TimerId</param>
        /// <returns>単一のMnemonicTimerDevice</returns>
        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByTimerId(int plcId, int timerId)
        {

            var mnemonicTimerDevice = _repository.GetMnemonicTimerDevicesByTimerId(plcId, timerId);
            return mnemonicTimerDevice;
        }

        /// <summary>
        /// MnemonicTimerDeviceを挿入または更新するヘルパーメソッド
        /// </summary>
        /// <param name="deviceToSave"></param>
        /// <param name="existingRecord"></param>
        private void UpsertMnemonicTimerDevice(
            MnemonicTimerDevice deviceToSave,
            MnemonicTimerDevice? existingRecord)
        {
            try
            {
                Debug.WriteLine($"[MnemonicTimerDeviceService.UpsertMnemonicTimerDevice] 開始");
                Debug.WriteLine($"  保存対象: MnemonicId={deviceToSave.MnemonicId}, RecordId={deviceToSave.RecordId}, TimerId={deviceToSave.TimerId}");
                
                // IAccessRepository経由でUpsertを実行
                _repository.UpsertMnemonicTimerDevice(deviceToSave);
                
                Debug.WriteLine($"[MnemonicTimerDeviceService.UpsertMnemonicTimerDevice] 完了");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MnemonicTimerDeviceService.UpsertMnemonicTimerDevice] エラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Detailのリストを受け取り、MnemonicTimerDeviceテーブルに保存する
        /// </summary>
        /// <param name="timers"></param>
        /// <param name="details"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        /// <param name="cycleId"></param>
        /// <param name="count"></param>
        public void SaveWithDetail(
            List<Timer> timers,
            List<ProcessDetail> details, int startNum, int plcId, ref int count)
        {
            try
            {
                Debug.WriteLine($"[SaveWithDetail] 開始 - details: {details.Count}件, timers: {timers.Count}件");
                
                // 1. 既存データを取得し、(MnemonicId, RecordId, TimerId)の複合キーを持つ辞書に変換
                Debug.WriteLine($"[SaveWithDetail] GetMnemonicTimerDeviceByMnemonic呼び出し前");
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.ProcessDetail);
                Debug.WriteLine($"[SaveWithDetail] 既存データ取得完了: {allExisting.Count}件");
                var existingLookup = allExisting.ToDictionary(m => (m.MnemonicId, m.RecordId, m.TimerId), m => m);
                Debug.WriteLine($"[SaveWithDetail] 辞書作成完了");

                // 2. ProcessDetailに関連するタイマーをRecordIdごとに整理した辞書を作成
                var timersByRecordId = new Dictionary<int, List<Timer>>();
                var detailTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.ProcessDetail);

                foreach (var timer in detailTimersSource)
                {
                    // 中間テーブルからRecordIdを取得
                    var recordIds = _repository.GetTimerRecordIds(timer.ID);
                    foreach (var recordId in recordIds)
                    {
                        if (!timersByRecordId.ContainsKey(recordId))
                        {
                            timersByRecordId[recordId] = new List<Timer>();
                        }
                        timersByRecordId[recordId].Add(timer);
                    }
                }

                // 3. ProcessDetailをループし、関連するタイマーを処理
                foreach (ProcessDetail detail in details)
                {
                    if (detail == null)
                    {
                        Debug.WriteLine($"[SaveWithDetail] スキップ - nullのdetail");
                        continue;
                    }

                    // 現在のProcessDetailに対応するタイマーがあるか、辞書から取得
                    if (timersByRecordId.TryGetValue(detail.Id, out var detailTimers))
                    {
                        foreach (Timer timer in detailTimers)
                        {
                            if (timer == null)
                            {
                                Debug.WriteLine($"[SaveWithDetail] スキップ - nullのtimer");
                                continue;
                            }

                            var timerStartWith = "";

                            switch (timer.TimerCategoryId)
                            {
                                case 6: // 異常時BK (EBT)
                                case 7: // 正常時BK (NBT)
                                    timerStartWith = "T";
                                    break;
                                default:
                                    timerStartWith = "ST";
                                    break;

                            }

                            var processTimerDevice = timerStartWith + (count + _mainViewModel.DeviceStartT);
                            var timerDevice = "ZR" + (timer.TimerNum + _mainViewModel.TimerStartZR);

                            // 複合キー (MnemonicId, Detail.Id, Timer.ID) で既存レコードを検索
                            var mnemonicId = (int)MnemonicType.ProcessDetail;
                            existingLookup.TryGetValue((mnemonicId, detail.Id, timer.ID), out var existingRecord);

                            var deviceToSave = new MnemonicTimerDevice
                            {
                                MnemonicId = (int)MnemonicType.ProcessDetail,
                                RecordId = detail.Id, // ★ 現在のdetail.IdをRecordIdとして設定
                                TimerId = timer.ID,
                                TimerCategoryId = timer.TimerCategoryId,
                                TimerDeviceT = processTimerDevice,
                                TimerDeviceZR = timerDevice,
                                PlcId = plcId,
                                CycleId = timer.CycleId,
                                Comment1 = timer.TimerName
                            };

                            UpsertMnemonicTimerDevice(deviceToSave, existingRecord);
                            count++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveWithDetail 失敗: {ex.Message}");
                // エラーログの記録や上位への例外通知など
                // Debug.WriteLine($"SaveWithOperation 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Operationのリストを受け取り、MnemonicTimerDeviceテーブルに保存する
        /// </summary>
        /// <param name="timers"></param>
        /// <param name="operations"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        /// <param name="count"></param>
        /// issued by the user
        public void SaveWithOperation(
            List<Timer> timers,
            List<Operation> operations,
            int startNum, int plcId, ref int count)
        {
            try
            {
                // 1. 既存データを取得し、(MnemonicId, RecordId, TimerId)の複合キーを持つ辞書に変換
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.Operation);
                var existingLookup = allExisting.ToDictionary(m => (m.MnemonicId, m.RecordId, m.TimerId), m => m);

                // 2. タイマーをRecordIdごとに整理した辞書を作成
                var timersByRecordId = new Dictionary<int, List<Timer>>();
                var operationTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.Operation);

                foreach (var timer in operationTimersSource)
                {
                    // 中間テーブルからRecordIdを取得
                    var recordIds = _repository.GetTimerRecordIds(timer.ID);
                    foreach (var recordId in recordIds)
                    {
                        if (!timersByRecordId.ContainsKey(recordId))
                        {
                            timersByRecordId[recordId] = new List<Timer>();
                        }
                        timersByRecordId[recordId].Add(timer);
                    }
                }

                // 3. Operationをループし、関連するタイマーを処理
                foreach (Operation operation in operations)
                {
                    if (operation == null) continue;

                    // 現在のOperationに対応するタイマーがあるか、辞書から取得
                    if (timersByRecordId.TryGetValue(operation.Id, out var operationTimers))
                    {
                        foreach (Timer timer in operationTimers)
                        {
                            if (timer == null)
                            {
                                continue;
                            }

                            // デバイス番号の計算
                            var timerStartWith = "";

                            switch (timer.TimerCategoryId)
                            {
                                case 6: // 異常時BK (EBT)
                                case 7: // 正常時BK (NBT)
                                    timerStartWith = "T";
                                    break;
                                default:
                                    timerStartWith = "ST";
                                    break;

                            }

                            var processTimerDevice = timerStartWith + (count + _mainViewModel.DeviceStartT);
                            var timerDevice = "ZR" + (timer.TimerNum + _mainViewModel.TimerStartZR);

                            // 複合キー (MnemonicId, Operation.Id, Timer.ID) で既存レコードを検索
                            var mnemonicId = (int)MnemonicType.Operation;
                            existingLookup.TryGetValue((mnemonicId, operation.Id, timer.ID), out var existingRecord);

                            var deviceToSave = new MnemonicTimerDevice
                            {
                                MnemonicId = (int)MnemonicType.Operation,
                                RecordId = operation.Id, // ★ TimerのRecordIdsではなく、現在のoperation.Idを使う
                                TimerId = timer.ID,
                                TimerCategoryId = timer.TimerCategoryId,
                                TimerDeviceT = processTimerDevice,
                                TimerDeviceZR = timerDevice,
                                PlcId = plcId,
                                CycleId = timer.CycleId,
                                Comment1 = timer.TimerName
                            };

                            // MnemonicTimerDeviceを挿入または更新
                            UpsertMnemonicTimerDevice(deviceToSave, existingRecord);
                            count++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveWithOperation 失敗: {ex.Message}");
                // エラーログの記録や上位への例外通知など
                // Debug.WriteLine($"SaveWithOperation 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Cylinderのリストを受け取り、MnemonicTimerDeviceテーブルに保存する
        /// </summary>
        /// <param name="timers"></param>
        /// <param name="cylinders"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        /// <param name="count"></param>
        public void SaveWithCY(
            List<Timer> timers,
            List<Cylinder> cylinders,
            int startNum, int plcId, ref int count)
        {
            try
            {
                // 1. 既存データを取得し、(MnemonicId, RecordId, TimerId)の複合キーを持つ辞書に変換
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.CY);
                var existingLookup = allExisting.ToDictionary(m => (m.MnemonicId, m.RecordId, m.TimerId), m => m);

                // 2. CYに関連するタイマーをRecordIdごとに整理した辞書を作成
                var timersByRecordId = new Dictionary<int, List<Timer>>();
                var cylinderTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.CY);

                foreach (var timer in cylinderTimersSource)
                {
                    // 中間テーブルからRecordIdを取得
                    var recordIds = _repository.GetTimerRecordIds(timer.ID);
                    foreach (var recordId in recordIds)
                    {
                        if (!timersByRecordId.ContainsKey(recordId))
                        {
                            timersByRecordId[recordId] = new List<Timer>();
                        }
                        timersByRecordId[recordId].Add(timer);
                    }
                }

                // 3. Cylinderをループし、関連するタイマーを処理
                foreach (Cylinder cylinder in cylinders)
                {
                    if (cylinder == null)
                    {
                        continue;
                    }

                    // 現在のCylinderに対応するタイマーがあるか、辞書から取得
                    if (timersByRecordId.TryGetValue(cylinder.Id, out var cylinderTimers))
                    {
                        foreach (Timer timer in cylinderTimers)
                        {
                            if (timer == null)
                            {
                                continue;
                            }

                            // デバイス番号の計算
                            var timerStartWith = "";

                            switch (timer.TimerCategoryId)
                            {
                                case 6: // 異常時BK (EBT)
                                case 7: // 正常時BK (NBT)
                                    timerStartWith = "T";
                                    break;
                                default:
                                    timerStartWith = "ST";
                                    break;

                            }

                            var processTimerDevice = timerStartWith + (count + _mainViewModel.DeviceStartT);
                            var timerDevice = "ZR" + (timer.TimerNum + _mainViewModel.TimerStartZR);

                            // 複合キー (MnemonicId, Cylinder.Id, Timer.ID) で既存レコードを検索
                            var mnemonicId = (int)MnemonicType.CY;
                            existingLookup.TryGetValue((mnemonicId, cylinder.Id, timer.ID), out var existingRecord);

                            var deviceToSave = new MnemonicTimerDevice
                            {
                                MnemonicId = (int)MnemonicType.CY,
                                RecordId = cylinder.Id, // ★ 現在のcylinder.IdをRecordIdとして設定
                                TimerId = timer.ID,
                                TimerCategoryId = timer.TimerCategoryId,
                                TimerDeviceT = processTimerDevice,
                                TimerDeviceZR = timerDevice,
                                PlcId = plcId,
                                CycleId = timer.CycleId,
                                Comment1 = timer.TimerName
                            };

                            UpsertMnemonicTimerDevice(deviceToSave, existingRecord);
                            count++;
                        }
                    }
                }
                // ★★★ 修正箇所 エンド ★★★
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveWithCY 失敗: {ex.Message}");
                throw;
            }
        }
    }
}
