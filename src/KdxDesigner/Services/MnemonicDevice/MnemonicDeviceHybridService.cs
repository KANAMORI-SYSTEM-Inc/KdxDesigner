using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Services.MnemonicDevice
{
    /// <summary>
    /// MnemonicDeviceのハイブリッドサービス
    /// メモリストアを優先的に使用し、必要に応じてデータベースにも保存する
    /// </summary>
    public class MnemonicDeviceHybridService : IMnemonicDeviceService
    {
        private readonly IMnemonicDeviceMemoryStore _memoryStore;
        private readonly MnemonicDeviceService _dbService;
        private readonly IMemoryService _memoryService;
        private readonly ISupabaseRepository _repository;

        // メモリストアのみを使用するかどうかのフラグ
        private bool _useMemoryStoreOnly = true;

        public MnemonicDeviceHybridService(
            ISupabaseRepository repository,
            IMemoryService memoryService,
            IMnemonicDeviceMemoryStore memoryStore)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _memoryStore = memoryStore ?? new MnemonicDeviceMemoryStore();
            _dbService = new MnemonicDeviceService(repository, memoryService);
            _memoryService = memoryService;
        }

        /// <summary>
        /// メモリストアのみを使用するかどうかを設定
        /// </summary>
        /// <param name="useMemoryOnly">メモリストアのみを使用する場合はtrue</param>
        public void SetMemoryOnlyMode(bool useMemoryOnly)
        {
            _useMemoryStoreOnly = useMemoryOnly;
        }

        /// <summary>
        /// PlcIdに基づいてニーモニックデバイスのリストを取得
        /// </summary>
        public async Task<List<Kdx.Contracts.DTOs.MnemonicDevice>> GetMnemonicDevice(int plcId)
        {
            // まずメモリストアから取得を試みる
            var devices = _memoryStore.GetMnemonicDevices(plcId);

            // メモリストアにデータがない場合、データベースから取得
            if (!devices.Any() && !_useMemoryStoreOnly)
            {
                devices = await _dbService.GetMnemonicDevice(plcId);

                // データベースから取得したデータをメモリストアにキャッシュ
                if (devices.Any())
                {
                    _memoryStore.BulkAddMnemonicDevices(devices, plcId);
                }
            }

            return devices;
        }

        /// <summary>
        /// PlcIdとMnemonicIdに基づいてニーモニックデバイスのリストを取得
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        /// <returns>ニーモニックデバイスのリスト</returns>
        public async Task<List<Kdx.Contracts.DTOs.MnemonicDevice>> GetMnemonicDeviceByMnemonic(int plcId, int mnemonicId)
        {
            // メモリストアから取得
            var devices = _memoryStore.GetMnemonicDevicesByMnemonic(plcId, mnemonicId);

            // メモリストアにデータがない場合、データベースから取得
            if (!devices.Any() && !_useMemoryStoreOnly)
            {
                devices = await _dbService.GetMnemonicDeviceByMnemonic(plcId, mnemonicId);

                // データベースから取得したデータをメモリストアに追加
                foreach (var device in devices)
                {
                    _memoryStore.AddOrUpdateMnemonicDevice(device, plcId);
                }
            }

            return devices;
        }

        /// <summary>
        /// すべてのニーモニックデバイスを削除
        /// </summary>
        public async Task DeleteAllMnemonicDevices()
        {
            // メモリストアをクリア
            _memoryStore.ClearAll();

            // データベースもクリア（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                await _dbService.DeleteAllMnemonicDevices();
            }
        }

        /// <summary>
        /// 特定のニーモニックデバイスを削除
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">削除するMnemonic ID</param>
        public async Task DeleteMnemonicDevice(int plcId, int mnemonicId)
        {
            // メモリストアから特定のMnemonicIdのデバイスを削除
            _memoryStore.DeleteMnemonicDevicesByMnemonicId(plcId, mnemonicId);

            // データベースからも削除（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                await _dbService.DeleteMnemonicDevice(plcId, mnemonicId);
            }
        }

        /// <summary>
        /// Process用のニーモニックデバイスを保存（Cycle用プロファイル）
        /// </summary>
        /// <param name="processes">プロセスのリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">サイクルID</param>
        public void SaveMnemonicDeviceProcess(List<Process> processes, int startNum, int plcId, int? cycleId = null)
        {
            var devices = new List<Kdx.Contracts.DTOs.MnemonicDevice>();
            var memories = new List<Kdx.Contracts.DTOs.Memory>();

            foreach (var process in processes)
            {
                var device = new Kdx.Contracts.DTOs.MnemonicDevice
                {
                    MnemonicId = (int)MnemonicType.Process,
                    RecordId = process.Id,
                    DeviceLabel = "L",
                    StartNum = startNum,
                    OutCoilCount = 5,
                    PlcId = plcId,
                    CycleId = cycleId,  // Cycle用プロファイル使用時に設定
                    Comment1 = process.Comment1,
                    Comment2 = process.Comment2
                };

                devices.Add(device);

                // メモリデータも生成
                for (int i = 0; i < device.OutCoilCount; i++)
                {
                    var memory = GenerateMemoryForDevice(device, i);
                    memories.Add(memory);
                }

                startNum += 5; // 次のプロセス用にオフセット
            }

            // メモリストアに保存
            _memoryStore.BulkAddMnemonicDevices(devices, plcId);
            _memoryStore.CacheGeneratedMemories(memories, plcId);
            _memoryService.SaveMemories(plcId, memories);

            // データベースにも保存（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                _dbService.SaveMnemonicDeviceProcess(processes, startNum, plcId, cycleId);
            }
        }

        /// <summary>
        /// ProcessDetail用のニーモニックデバイスを保存（Cycle用プロファイル）
        /// </summary>
        /// <param name="details">プロセス詳細のリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">サイクルID</param>
        public async Task SaveMnemonicDeviceProcessDetail(List<ProcessDetail> details, int startNum, int plcId, int? cycleId = null)
        {
            var devices = new List<Kdx.Contracts.DTOs.MnemonicDevice>();
            var memories = new List<Kdx.Contracts.DTOs.Memory>();



            foreach (var detail in details)
            {

                // 2. OperationId がある場合のみ、Operation情報を取得
                Operation? operation = null;
                if (detail.OperationId.HasValue)
                {
                    // ★ パフォーマンス改善: new せずに、フィールドの _repository を使う
                    operation = await _repository.GetOperationByIdAsync(detail.OperationId.Value);
                }

                // 結果を保持する変数を先に宣言し、デフォルト値を設定
                string comment1 = "";
                string comment2 = detail.Comment ?? "";

                string shortName = "";
                if (detail.CategoryId.HasValue)
                {
                    // 3. CategoryId がある場合のみ、カテゴリ情報を取得
                    var category = await _repository.GetProcessDetailCategoryByIdAsync(detail.CategoryId.Value);
                    if (category != null)
                    {
                        shortName = category.ShortName ?? "";
                    }
                }

                // 1. CYIdが存在するかどうかを正しくチェック
                if (operation != null)
                {
                    // 2. IDを使ってCYオブジェクトを取得
                    Cylinder? cY = await _repository.GetCYByIdAsync(operation.CYId!.Value);

                    // 3. CYオブジェクトが取得でき、かつその中にMachineIdが存在するかチェック
                    if (cY != null && cY.MachineNameId.HasValue)
                    {
                        MachineName? machineName = await _repository.GetMachineNameByIdAsync(cY.MachineNameId!.Value);

                        // 5. Machineオブジェクトが取得できたことを確認してから、コメントを生成
                        if (machineName != null)
                        {
                            // CYNumやShortNameがnullの場合も考慮し、空文字列として結合
                            comment1 = (cY.CYNum ?? "") + (machineName.ShortName ?? "");
                        }
                        else
                        {
                            // Machineが見つからなかった場合のエラーハンドリング（任意）
                            // 例: _errorAggregator.AddError(...);
                        }
                    }
                    else if (cY != null)
                    {
                        // CYが見つからなかったか、CYにMachineIdがなかった場合のエラーハンドリング（任意）

                        comment1 = (cY.CYNum ?? "");

                    }
                }
                else
                {
                    comment1 = shortName; // Operationがない場合は、カテゴリのショートネームを使用
                }


                var device = new Kdx.Contracts.DTOs.MnemonicDevice
                {
                    MnemonicId = (int)MnemonicType.ProcessDetail,
                    RecordId = detail.Id,
                    DeviceLabel = "L",
                    StartNum = startNum,
                    OutCoilCount = 5,
                    PlcId = plcId,
                    CycleId = cycleId,  // Cycle用プロファイル使用時に設定
                    Comment1 = comment1,
                    Comment2 = comment2
                };

                devices.Add(device);

                // メモリデータも生成
                for (int i = 0; i < device.OutCoilCount; i++)
                {
                    var memory = GenerateMemoryForDevice(device, i);
                    memories.Add(memory);
                }

                startNum += 5;
            }

            // メモリストアに保存
            _memoryStore.BulkAddMnemonicDevices(devices, plcId);
            _memoryService.SaveMemories(plcId, memories);

            // 既存のキャッシュに追加
            var existingMemories = _memoryStore.GetCachedMemories(plcId);
            existingMemories.AddRange(memories);
            _memoryStore.CacheGeneratedMemories(existingMemories, plcId);
        }

        /// <summary>
        /// Operation用のニーモニックデバイスを保存（Cycle用プロファイル）
        /// </summary>
        public void SaveMnemonicDeviceOperation(List<Operation> operations, int startNum, int plcId, int? cycleId = null)
        {
            var devices = new List<Kdx.Contracts.DTOs.MnemonicDevice>();
            var memories = new List<Kdx.Contracts.DTOs.Memory>();

            foreach (var operation in operations)
            {
                var device = new Kdx.Contracts.DTOs.MnemonicDevice
                {
                    MnemonicId = (int)MnemonicType.Operation,
                    RecordId = operation.Id,
                    DeviceLabel = "M",
                    StartNum = startNum,
                    OutCoilCount = 20,
                    PlcId = plcId,
                    CycleId = cycleId,  // Cycle用プロファイル使用時に設定
                    Comment1 = operation.OperationName,
                    Comment2 = operation.GoBack?.ToString() ?? ""
                };

                devices.Add(device);

                // メモリデータも生成
                for (int i = 0; i < device.OutCoilCount; i++)
                {
                    var memory = GenerateMemoryForDevice(device, i);
                    memories.Add(memory);
                }

                startNum += 20;
            }

            // メモリストアに保存
            _memoryStore.BulkAddMnemonicDevices(devices, plcId);

            // 既存のキャッシュに追加
            var existingMemories = _memoryStore.GetCachedMemories(plcId);
            existingMemories.AddRange(memories);
            _memoryStore.CacheGeneratedMemories(existingMemories, plcId);
            _memoryService.SaveMemories(plcId, memories);

            // データベースにも保存（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                _dbService.SaveMnemonicDeviceOperation(operations, startNum, plcId, cycleId);
            }
        }

        /// <summary>
        /// CY用のニーモニックデバイスを保存（PLC用プロファイル - CycleIdはnull）
        /// </summary>
        public async Task SaveMnemonicDeviceCY(List<Cylinder> cylinders, int startNum, int plcId, int? cycleId = null)
        {
            var devices = new List<Kdx.Contracts.DTOs.MnemonicDevice>();
            var memories = new List<Kdx.Contracts.DTOs.Memory>();

            foreach (var cylinder in cylinders)
            {
                if (cylinder == null)
                {
                    // Cylinderがnullの場合はスキップ
                    continue;
                }

                string? comment2 = string.Empty;
                if (cylinder.MachineNameId != null)
                {
                    // MachineIdがnullの場合はスキップ
                    var machineName = await _repository.GetMachineNameByIdAsync((int)cylinder.MachineNameId);
                    comment2 = machineName?.ShortName ?? "未設定";
                }

                var device = new Kdx.Contracts.DTOs.MnemonicDevice
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Id,
                    DeviceLabel = "M",
                    StartNum = startNum,
                    OutCoilCount = 50,
                    PlcId = plcId,
                    CycleId = cycleId,  // PLC用プロファイルなので常にnull
                    Comment1 = cylinder.CYNum,
                    Comment2 = comment2 ?? "未設定"
                };

                devices.Add(device);

                // メモリデータも生成
                for (int i = 0; i < device.OutCoilCount; i++)
                {
                    var memory = GenerateMemoryForDevice(device, i);
                    memories.Add(memory);
                }

                startNum += 50;
            }

            // メモリストアに保存
            _memoryStore.BulkAddMnemonicDevices(devices, plcId);

            // 既存のキャッシュに追加
            var existingMemories = _memoryStore.GetCachedMemories(plcId);
            existingMemories.AddRange(memories);
            _memoryStore.CacheGeneratedMemories(existingMemories, plcId);
            _memoryService.SaveMemories(plcId, memories);

            // データベースにも保存（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                await _dbService.SaveMnemonicDeviceCY(cylinders, startNum, plcId, cycleId);

            }
        }

        /// <summary>
        /// メモリストアから直接メモリデータを取得
        /// データベースアクセスを避けてパフォーマンスを向上
        /// </summary>
        public List<Kdx.Contracts.DTOs.Memory> GetGeneratedMemories(int plcId)
        {
            return _memoryStore.GetCachedMemories(plcId);
        }

        /// <summary>
        /// デバイス情報からメモリデータを生成
        /// </summary>
        /// <param name="device">ニーモニックデバイス情報</param>
        /// <param name="outcoilIndex">アウトコイルインデックス</param>
        private Kdx.Contracts.DTOs.Memory GenerateMemoryForDevice(Kdx.Contracts.DTOs.MnemonicDevice device, int outcoilIndex)
        {
            var deviceNum = device.StartNum + outcoilIndex;
            var deviceString = device.DeviceLabel + deviceNum.ToString();

            var categoryString = device.MnemonicId switch
            {
                1 => "工程",
                2 => "工程詳細",
                3 => "操作",
                4 => "出力",
                _ => "なし"
            };

            var row1 = device.MnemonicId switch
            {
                1 => "工程" + device.RecordId,
                2 => "詳細" + device.RecordId,
                3 => "操作" + device.RecordId,
                4 => "出力" + device.RecordId,
                _ => "なし"
            };

            var row2 = device.MnemonicId switch
            {
                1 => device.Comment1 ?? "未設定",
                2 => device.Comment1 ?? "未設定",
                3 => device.Comment1 ?? "未設定",
                4 => device.Comment1 ?? "未設定",
                _ => ""
            };

            string row3 = device.MnemonicId switch
            {
                1 => device.Comment2 ?? "",
                2 => device.Comment2 ?? "",
                3 => device.Comment2 ?? "",
                4 => outcoilIndex switch
                {
                    0 => "行き方向",
                    1 => "帰り方向",
                    2 => "行き方向",
                    3 => "帰り方向",
                    4 => "初回",
                    5 => "行き方向",
                    6 => "帰り方向",
                    7 => "行き手動",
                    8 => "帰り手動",
                    9 => "シングル",
                    10 => "行き指令",
                    11 => "帰り指令",
                    12 => "行き指令",
                    13 => "帰り指令",
                    14 => "指令",
                    15 => "行き自動",
                    16 => "帰り自動",
                    17 => "行き手動",
                    18 => "帰り手動",
                    19 => "保持出力",
                    20 => "保持出力",
                    21 => "速度指令",
                    22 => "速度指令",
                    23 => "速度指令",
                    24 => "速度指令",
                    25 => "速度指令",
                    26 => "速度指令",
                    27 => "速度指令",
                    28 => "速度指令",
                    29 => "速度指令",
                    30 => "速度指令",
                    31 => "強制減速",
                    32 => "予備",
                    33 => "高速停止",
                    34 => "停止時　",
                    35 => "行きOK",
                    36 => "帰りOK",
                    37 => "指令OK",
                    38 => "予備",
                    39 => "予備",
                    40 => "ｻｰﾎﾞ軸",
                    41 => "ｻｰﾎﾞ作動",
                    42 => "ｻｰﾎﾞJOG",
                    43 => "ｻｰﾎﾞJOG",
                    44 => "",
                    45 => "",
                    46 => "",
                    47 => "",
                    48 => "行きﾁｪｯｸ",
                    49 => "帰りﾁｪｯｸ",
                    _ => ""
                },
                _ => "未定義"
            };

            string row4 = device.MnemonicId switch
            {
                1 => outcoilIndex switch
                {
                    0 => "開始条件",
                    1 => "開始",
                    2 => "実行中",
                    3 => "終了条件",
                    4 => "終了",
                    _ => ""
                },
                2 => outcoilIndex switch
                {
                    0 => "開始条件",
                    1 => "開始",
                    2 => "実行中",
                    3 => "操作釦",
                    4 => "終了",
                    _ => ""
                },
                3 => outcoilIndex switch
                {
                    0 => "自動運転",
                    1 => "操作ｽｲｯﾁ",
                    2 => "手動運転",
                    3 => "ｶｳﾝﾀ",
                    4 => "個別ﾘｾｯﾄ",
                    5 => "操作開始",
                    6 => "出力可",
                    7 => "開始",
                    8 => "切指令",
                    9 => "制御ｾﾝｻ",
                    10 => "速度1",
                    11 => "速度2",
                    12 => "速度3",
                    13 => "速度4",
                    14 => "速度5",
                    15 => "強制減速",
                    16 => "終了位置",
                    17 => "出力切",
                    18 => "BK作動",
                    19 => "完了",
                    _ => ""
                },
                4 => outcoilIndex switch
                {
                    0 => "自動指令",
                    1 => "自動指令",
                    2 => "手動指令",
                    3 => "手動指令",
                    4 => "帰り指令",
                    5 => "自動保持",
                    6 => "自動保持",
                    7 => "JOG",
                    8 => "JOG",
                    9 => "OFF指令",
                    10 => "手動",
                    11 => "手動",
                    12 => "自動",
                    13 => "自動",
                    14 => "",
                    15 => "ILOK",
                    16 => "ILOK",
                    17 => "ILOK",
                    18 => "ILOK",
                    19 => "行き",
                    20 => "帰り",
                    21 => "1",
                    22 => "2",
                    23 => "3",
                    24 => "4",
                    25 => "5",
                    26 => "6",
                    27 => "7",
                    28 => "8",
                    29 => "9",
                    30 => "10",
                    31 => "",
                    32 => "",
                    33 => "記憶",
                    34 => "ﾌﾞﾚｰｷ待ち",
                    35 => "",
                    36 => "",
                    37 => "",
                    38 => "",
                    39 => "",
                    40 => "停止",
                    41 => "ｴﾗｰ発生",
                    42 => "行きOK",
                    43 => "帰りOK",
                    44 => "",
                    45 => "",
                    46 => "",
                    47 => "",
                    48 => "",
                    49 => "",

                    _ => ""
                },
                _ => "未定義"
            };

            return new Kdx.Contracts.DTOs.Memory
            {
                PlcId = device.PlcId,
                Device = deviceString,
                DeviceNumber = deviceNum,
                DeviceNumber1 = deviceString,
                Category = categoryString,
                Row_1 = row1,
                Row_2 = row2,
                Row_3 = row3,
                Row_4 = row4,
                MnemonicId = device.MnemonicId,
                RecordId = device.RecordId,
                OutcoilNumber = outcoilIndex
            };
        }

        /// <summary>
        /// メモリストアの統計情報を取得
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        public MnemonicDeviceStatistics GetStatistics(int plcId)
        {
            return _memoryStore.GetStatistics(plcId);
        }

        /// <summary>
        /// メモリストアのデータをデータベースに永続化
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        public void PersistToDatabase(int plcId)
        {
            if (_useMemoryStoreOnly)
            {
                // メモリストアのデータをデータベースに保存
                var devices = _memoryStore.GetMnemonicDevices(plcId);

                // MnemonicIdごとにグループ化して保存
                var processDevices = devices.Where(d => d.MnemonicId == (int)MnemonicType.Process).ToList();
                var detailDevices = devices.Where(d => d.MnemonicId == (int)MnemonicType.ProcessDetail).ToList();
                var operationDevices = devices.Where(d => d.MnemonicId == (int)MnemonicType.Operation).ToList();
                var cyDevices = devices.Where(d => d.MnemonicId == (int)MnemonicType.CY).ToList();

                // TODO: 各タイプごとにデータベースに保存する処理を実装
                // 現在はメモリストアのみで動作するため、必要に応じて実装
            }
        }
    }

}
