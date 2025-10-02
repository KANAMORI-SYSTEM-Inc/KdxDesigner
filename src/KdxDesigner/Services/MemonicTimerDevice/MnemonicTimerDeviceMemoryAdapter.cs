using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using System.Diagnostics;

using Kdx.Contracts.Interfaces;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.ViewModels;

using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.Services.MemonicTimerDevice
{
    /// <summary>
    /// MnemonicTimerDeviceServiceのメモリストアアダプター
    /// 既存のインターフェースを維持しながら、メモリストアを使用する
    /// </summary>
    public class MnemonicTimerDeviceMemoryAdapter : IMnemonicTimerDeviceService
    {
        private readonly IMnemonicDeviceMemoryStore _memoryStore;
        private readonly MnemonicTimerDeviceService _dbService;
        private bool _useMemoryStoreOnly = false;
        private MainViewModel _mainViewModel;
        private readonly IAccessRepository _repository;
        private readonly IMemoryService _memoryService;


        public MnemonicTimerDeviceMemoryAdapter(
            IAccessRepository repository,
            MainViewModel mainViewModel,
            IMnemonicDeviceMemoryStore memoryStore,
            IMemoryService memoryService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _memoryStore = memoryStore ?? new MnemonicDeviceMemoryStore();
            _dbService = new MnemonicTimerDeviceService(repository, mainViewModel);
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));

        }

        /// <summary>
        /// メモリストアのみを使用するかどうかを設定
        /// </summary>
        public void SetMemoryOnlyMode(bool useMemoryOnly)
        {
            _useMemoryStoreOnly = useMemoryOnly;
        }

        /// <summary>
        /// PlcIdとCycleIdに基づいてMnemonicTimerDeviceを取得
        /// </summary>
        public List<MnemonicTimerDevice> GetMnemonicTimerDevice(int plcId, int cycleId)
        {
            // メモリストアから取得
            var devices = _memoryStore.GetTimerDevices(plcId, cycleId);

            // メモリストアにデータがない場合、データベースから取得
            if (!devices.Any() && !_useMemoryStoreOnly)
            {
                devices = _dbService.GetMnemonicTimerDevice(plcId, cycleId);

                // データベースから取得したデータをメモリストアにキャッシュ
                foreach (var device in devices)
                {
                    _memoryStore.AddOrUpdateTimerDevice(device, plcId, cycleId);
                }
            }

            return devices;
        }

        /// <summary>
        /// ProcessDetailのタイマーデバイスを保存
        /// </summary>
        public void SaveWithDetail(List<Timer> timers, List<ProcessDetail> details, int startNum, int plcId, ref int count)
        {
            if(_mainViewModel == null)
            {
                Debug.WriteLine($"[MemoryAdapter.SaveWithDetail] WARNING: MainViewModel is null");
                return;
            }
            var devices = new List<MnemonicTimerDevice>();

            var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.ProcessDetail);
            var existingLookup = allExisting.ToDictionary(m => (m.MnemonicId, m.RecordId, m.TimerId), m => m);

            // 2. タイマーをRecordIdごとに整理した辞書を作成
            var timersByRecordId = new Dictionary<int, List<Timer>>();
            var detailTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.ProcessDetail);
            
            var allMemoriesToSave = new List<Kdx.Contracts.DTOs.Memory>();


            foreach (var timer in detailTimersSource)
            {
                // 中間テーブルからRecordIdを取得
                var recordIds = _repository.GetTimerRecordIds(timer.ID);
                Debug.WriteLine($"[MemoryAdapter.SaveWithDetail] Timer.ID={timer.ID}, TimerName={timer.TimerName}, RecordIds={recordIds.Count}件");
                
                foreach (var recordId in recordIds)
                {
                    Debug.WriteLine($"[MemoryAdapter.SaveWithDetail]   -> RecordId={recordId}");
                    if (!timersByRecordId.ContainsKey(recordId))
                    {
                        timersByRecordId[recordId] = new List<Timer>();
                    }
                    timersByRecordId[recordId].Add(timer);
                }
            }
            

            foreach (var detail in details)
            {
                if (detail == null) continue;

                // デバッグ: 最初の数件のdetail.Idを出力
                if (devices.Count < 5)
                {
                    Debug.WriteLine($"[MemoryAdapter.SaveWithDetail] Processing detail.Id={detail.Id}, Name={detail.DetailName}");
                }

                if (timersByRecordId.TryGetValue(detail.Id, out var detailTimers))
                {
                    
                    foreach (Timer timer in detailTimers)
                    {
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

                        var device = new MnemonicTimerDevice
                        {
                            MnemonicId = (int)MnemonicType.ProcessDetail,
                            RecordId = detail.Id, // ★ 現在のdetail.IdをRecordIdとして設定
                            TimerId = timer.ID,
                            TimerCategoryId = timer.TimerCategoryId,
                            TimerDeviceT = processTimerDevice,
                            TimerDeviceZR = timerDevice,
                            PlcId = plcId,
                            CycleId = timer.CycleId,
                            Comment1 = timer.TimerName,
                            Comment2 = timer.TimerName

                        };

                        devices.Add(device);

                        // メモリストアに保存（CycleIdは必ずMainViewModelから取得）
                        var cycleId = _mainViewModel.SelectedCycle?.Id ?? timer.CycleId ?? 1;
                        _memoryStore.AddOrUpdateTimerDevice(device, plcId, cycleId);

                        // 出力タイマのメモリレコードを生成
                        var memoryT = new Kdx.Contracts.DTOs.Memory
                        {
                            PlcId = plcId,
                            Device = device.TimerDeviceT,
                            MemoryCategory = 7, // T
                            DeviceNumber = count + _mainViewModel.DeviceStartT,
                            DeviceNumber1 = device.TimerDeviceT,
                            DeviceNumber2 = "",
                            Category = "操作タイマ",
                            Row_1 = "操作ﾀｲﾏ",
                            Row_2 = timer.TimerName,
                            Row_3 = "",
                            Row_4 = "",
                            Direct_Input = timer.TimerName,
                            Confirm = "",
                            Note = "",
                            GOT = "false",
                            MnemonicId = (int)MnemonicType.CY,
                            RecordId = device.RecordId,
                            OutcoilNumber = 0
                        };
                        allMemoriesToSave.Add(memoryT);

                        var memoryZR = new Kdx.Contracts.DTOs.Memory
                        {
                            PlcId = plcId,
                            Device = device.TimerDeviceZR,
                            MemoryCategory = 5, // ZR
                            DeviceNumber = timer.TimerNum + _mainViewModel.TimerStartZR,
                            DeviceNumber1 = device.TimerDeviceZR,
                            DeviceNumber2 = "",
                            Category = "操作タイマ",
                            Row_1 = "操作ﾀｲﾏ",
                            Row_2 = timer.TimerName,
                            Row_3 = "",
                            Row_4 = "",
                            Direct_Input = timer.TimerName,
                            Confirm = "",
                            Note = "",
                            GOT = "false",
                            MnemonicId = (int)MnemonicType.CY,
                            RecordId = device.RecordId,
                            OutcoilNumber = 0
                        };
                        allMemoriesToSave.Add(memoryZR);

                        count++;

                    }
                }
            }

            // --- 3. ループ完了後、蓄積した全Kdx.Contracts.DTOs.Memoryレコードを同じトランザクションで一括保存 ---
            if (allMemoriesToSave.Any())
            {
                var distinctOrderedMemories = allMemoriesToSave
                    .GroupBy(m => m.Device)                  // Device ごとにグループ化
                    .Select(g => g.First())                  // 最初の1件を採用（重複排除）
                    .OrderBy(m => m.DeviceNumber) // Device 順にソート
                    .ToList();

                _memoryService.SaveMemories(plcId, distinctOrderedMemories);
            }

            // データベースにも保存（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                _dbService.SaveWithDetail(timers, details, startNum, plcId, ref count);
            }
            
            Debug.WriteLine($"[MemoryAdapter.SaveWithDetail] 完了 - 保存したデバイス数: {devices.Count}");
        }

        /// <summary>
        /// Operationのタイマーデバイスを保存
        /// </summary>
        /// issued by the user
        public void SaveWithOperation(List<Timer> timers, List<Operation> operations, int startNum, int plcId, ref int count)
        {
            var devices = new List<MnemonicTimerDevice>();

            var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.Operation);
            var existingLookup = allExisting.ToDictionary(m => (m.MnemonicId, m.RecordId, m.TimerId), m => m);

            // 2. タイマーをRecordIdごとに整理した辞書を作成
            var timersByRecordId = new Dictionary<int, List<Timer>>();
            var operationTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.Operation);
            var allMemoriesToSave = new List<Kdx.Contracts.DTOs.Memory>();

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

            foreach (Operation operation in operations)
            {
                if (operation == null) continue;

                if (timersByRecordId.TryGetValue(operation.Id, out var operationTimers))
                {
                    foreach (Timer timer in operationTimers)
                    {
                        if (timer == null)
                        {
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

                        // 複合キー (MnemonicId, Operation.Id, Timer.ID) で既存レコードを検索
                        var mnemonicId = (int)MnemonicType.Operation;
                        existingLookup.TryGetValue((mnemonicId, operation.Id, timer.ID), out var existingRecord);

                        var device = new MnemonicTimerDevice
                        {
                            MnemonicId = (int)MnemonicType.Operation,
                            RecordId = operation.Id, // ★ 現在のdetail.IdをRecordIdとして設定
                            TimerId = timer.ID,
                            TimerCategoryId = timer.TimerCategoryId,
                            TimerDeviceT = processTimerDevice,
                            TimerDeviceZR = timerDevice,
                            PlcId = plcId,
                            CycleId = timer.CycleId,
                            Comment1 = timer.TimerName
                        };

                        // メモリストアに保存（CycleIdは必ずMainViewModelから取得）
                        var cycleId = _mainViewModel.SelectedCycle?.Id ?? timer.CycleId ?? 1;
                        Debug.WriteLine($"[MemoryAdapter.SaveWithDetail/Operation/CY] メモリストアに保存 - cycleId: {cycleId}, MnemonicId: {device.MnemonicId}, RecordId: {device.RecordId}, TimerId: {device.TimerId}");
                        _memoryStore.AddOrUpdateTimerDevice(device, plcId, cycleId);
                        count++;

                        // --- 2. 対応するKdx.Contracts.DTOs.Memoryレコードを生成し、リストに蓄積 ---
                        int mnemonicStartNum = count * 5 + startNum;

                        // 出力タイマのメモリレコードを生成
                        var memoryT = new Kdx.Contracts.DTOs.Memory
                        {
                            PlcId = plcId,
                            Device = device.TimerDeviceT,
                            MemoryCategory = 7, // T
                            DeviceNumber = count + _mainViewModel.DeviceStartT,
                            DeviceNumber1 = device.TimerDeviceT,
                            DeviceNumber2 = "",
                            Category = "操作タイマ",
                            Row_1 = "操作ﾀｲﾏ",
                            Row_2 = timer.TimerName,
                            Row_3 = "",
                            Row_4 = "",
                            Direct_Input = timer.TimerName,
                            Confirm = "",
                            Note = "",
                            GOT = "false",
                            MnemonicId = (int)MnemonicType.CY,
                            RecordId = device.RecordId,
                            OutcoilNumber = 0
                        };
                        allMemoriesToSave.Add(memoryT);

                        var memoryZR = new Kdx.Contracts.DTOs.Memory
                        {
                            PlcId = plcId,
                            Device = device.TimerDeviceZR,
                            MemoryCategory = 5, // ZR
                            DeviceNumber = timer.TimerNum + _mainViewModel.TimerStartZR,
                            DeviceNumber1 = device.TimerDeviceZR,
                            DeviceNumber2 = "",
                            Category = "操作タイマ",
                            Row_1 = "操作ﾀｲﾏ",
                            Row_2 = timer.TimerName,
                            Row_3 = "",
                            Row_4 = "",
                            Direct_Input = timer.TimerName,
                            Confirm = "",
                            Note = "",
                            GOT = "false",
                            MnemonicId = (int)MnemonicType.CY,
                            RecordId = device.RecordId,
                            OutcoilNumber = 0
                        };
                        allMemoriesToSave.Add(memoryZR);
                    }
                }
            }

            // --- 3. ループ完了後、蓄積した全Kdx.Contracts.DTOs.Memoryレコードを同じトランザクションで一括保存 ---
            if (allMemoriesToSave.Any())
            {
                var distinctOrderedMemories = allMemoriesToSave
                    .GroupBy(m => m.Device)                  // Device ごとにグループ化
                    .Select(g => g.First())                  // 最初の1件を採用（重複排除）
                    .OrderBy(m => m.Device, StringComparer.Ordinal) // Device 順にソート
                    .ToList();

                _memoryService.SaveMemories(plcId, distinctOrderedMemories);
            }

            // データベースにも保存（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                _dbService.SaveWithOperation(timers, operations, startNum, plcId, ref count);
            }
        }

        /// <summary>
        /// CY（シリンダー）のタイマーデバイスを保存
        /// </summary>
        public void SaveWithCY(List<Timer> timers, List<Cylinder> cylinders, int startNum, int plcId, ref int count)
        {
            var devices = new List<MnemonicTimerDevice>();

            // 1. 既存データを取得し、(MnemonicId, RecordId, TimerId)の複合キーを持つ辞書に変換
            var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.CY);
            var existingLookup = allExisting.ToDictionary(m => (m.MnemonicId, m.RecordId, m.TimerId), m => m);

            // 2. CYに関連するタイマーをRecordIdごとに整理した辞書を作成
            var timersByRecordId = new Dictionary<int, List<Timer>>();
            var cylinderTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.CY);
            var allMemoriesToSave = new List<Kdx.Contracts.DTOs.Memory>();

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

            foreach (Cylinder cylinder in cylinders)
            {
                if (cylinder == null)
                {
                    continue;
                }

                // CYに関連するタイマーを検索
                // MnemonicIdで関連付け（CYはCycleIdを持つ）
                var relevantTimers = timers.Where(t => t.MnemonicId == (int)MnemonicType.CY).ToList();

                // 現在のCylinderに対応するタイマーがあるか、辞書から取得
                if (timersByRecordId.TryGetValue(cylinder.Id, out var cylinderTimers))
                {
                    foreach (Timer timer in cylinderTimers)
                    {

                        if (timer == null) continue;

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

                        var device = new MnemonicTimerDevice
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

                        // メモリストアに保存（CycleIdは必ずMainViewModelから取得）
                        var cycleId = _mainViewModel.SelectedCycle?.Id ?? timer.CycleId ?? 1;
                        Debug.WriteLine($"[MemoryAdapter.SaveWithDetail/Operation/CY] メモリストアに保存 - cycleId: {cycleId}, MnemonicId: {device.MnemonicId}, RecordId: {device.RecordId}, TimerId: {device.TimerId}");
                        _memoryStore.AddOrUpdateTimerDevice(device, plcId, cycleId);
                        count++;

                        // --- 2. 対応するKdx.Contracts.DTOs.Memoryレコードを生成し、リストに蓄積 ---
                        int mnemonicStartNum = count * 5 + startNum;

                        // 出力タイマのメモリレコードを生成
                        var memoryT = new Kdx.Contracts.DTOs.Memory
                        {
                            PlcId = plcId,
                            Device = device.TimerDeviceT,
                            MemoryCategory = 7, // T
                            DeviceNumber = count + _mainViewModel.DeviceStartT,
                            DeviceNumber1 = device.TimerDeviceT,
                            DeviceNumber2 = "",
                            Category = "出力タイマ",
                            Row_1 = "出力ﾀｲﾏ",
                            Row_2 = timer.TimerName,
                            Row_3 = "",
                            Row_4 = "",
                            Direct_Input = timer.TimerName,
                            Confirm = "",
                            Note = "",
                            GOT = "false",
                            MnemonicId = (int)MnemonicType.CY,
                            RecordId = device.RecordId,
                            OutcoilNumber = 0
                        };
                        allMemoriesToSave.Add(memoryT);

                        var memoryZR = new Kdx.Contracts.DTOs.Memory
                        {
                            PlcId = plcId,
                            Device = device.TimerDeviceZR,
                            MemoryCategory = 5, // ZR
                            DeviceNumber = timer.TimerNum + _mainViewModel.TimerStartZR,
                            DeviceNumber1 = device.TimerDeviceZR,
                            DeviceNumber2 = "",
                            Category = "出力タイマ",
                            Row_1 = "出力ﾀｲﾏ",
                            Row_2 = timer.TimerName,
                            Row_3 = "",
                            Row_4 = "",
                            Direct_Input = timer.TimerName,
                            Confirm = "",
                            Note = "",
                            GOT = "false",
                            MnemonicId = (int)MnemonicType.CY,
                            RecordId = device.RecordId,
                            OutcoilNumber = 0
                        };
                        allMemoriesToSave.Add(memoryZR);
                        
                    }
                }
            }

            // --- 3. ループ完了後、蓄積した全Kdx.Contracts.DTOs.Memoryレコードを同じトランザクションで一括保存 ---
            // データベースにも保存（メモリオンリーモードでない場合）
            // --- 3. ループ完了後、蓄積した全Kdx.Contracts.DTOs.Memoryレコードを同じトランザクションで一括保存 ---
            if (allMemoriesToSave.Any())
            {
                var distinctOrderedMemories = allMemoriesToSave
                    .GroupBy(m => m.Device)                  // Device ごとにグループ化
                    .Select(g => g.First())                  // 最初の1件を採用（重複排除）
                    .OrderBy(m => m.Device, StringComparer.Ordinal) // Device 順にソート
                    .ToList();

                _memoryService.SaveMemories(plcId, distinctOrderedMemories);
            }


            // データベースにも保存（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                _dbService.SaveWithCY(timers, cylinders, startNum, plcId, ref count);
            }
        }

        // その他のインターフェースメソッドの実装
        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByCycle(int plcId, int cycleId, int mnemonicId)
        {
            var devices = _memoryStore.GetTimerDevices(plcId, cycleId);
            return devices.Where(d => d.MnemonicId == mnemonicId).ToList();
        }

        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByMnemonic(int plcId, int mnemonicId)
        {
            Debug.WriteLine($"[MemoryAdapter.GetMnemonicTimerDeviceByMnemonic] 開始 - plcId: {plcId}, mnemonicId: {mnemonicId}");
            
            // メモリストアから全サイクルのタイマーデバイスを取得してフィルタリング
            var allDevices = new List<MnemonicTimerDevice>();
            
            // MainViewModelからCycleIdを取得（通常はSelectedCycle.Idを使用）
            if (_mainViewModel?.SelectedCycle != null)
            {
                var cycleId = _mainViewModel.SelectedCycle.Id;
                var devices = _memoryStore.GetTimerDevices(plcId, cycleId);
                allDevices = devices.Where(d => d.MnemonicId == mnemonicId).ToList();
            }
            else
            {
                Debug.WriteLine($"[MemoryAdapter.GetMnemonicTimerDeviceByMnemonic] SelectedCycleがnull");
            }
            
            // メモリストアにデータがない場合、データベースから取得
            if (!allDevices.Any() && !_useMemoryStoreOnly)
            {
                allDevices = _dbService.GetMnemonicTimerDeviceByMnemonic(plcId, mnemonicId);
                
                // 取得したデータをメモリストアにキャッシュ
                foreach (var device in allDevices)
                {
                    _memoryStore.AddOrUpdateTimerDevice(device, plcId, device.CycleId ?? 1);
                }
            }
            
            Debug.WriteLine($"[MemoryAdapter.GetMnemonicTimerDeviceByMnemonic] 完了 - 返却: {allDevices.Count}件");
            return allDevices;
        }

        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByTimerId(int plcId, int timerId)
        {
            // タイマーIDでの検索
            // 現在のメモリストアの制限により、データベースから取得
            return !_useMemoryStoreOnly ? _dbService.GetMnemonicTimerDeviceByTimerId(plcId, timerId) : new List<MnemonicTimerDevice>();
        }
    }
}
