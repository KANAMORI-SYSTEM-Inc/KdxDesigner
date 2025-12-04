using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Services.ProsTimeData
{
    /// <summary>
    /// ProsTime（工程時間）とMemoryデータを生成するビルダー
    /// ビジネスロジックを担当し、データベース保存は行わない
    /// </summary>
    public class ProsTimeDataBuilder
    {
        private readonly ISupabaseRepository _repository;
        private readonly Dictionary<int, OperationProsTimeConfig> _loadedOperationConfigs;

        /// <summary>
        /// Operationカテゴリごとのコンフィグレーション
        /// </summary>
        public class OperationProsTimeConfig
        {
            public int TotalProsTimeCount { get; set; }
            public Dictionary<int, int> SortIdToCategoryIdMap { get; set; }

            public OperationProsTimeConfig()
            {
                SortIdToCategoryIdMap = new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// ProsTimeデータ生成結果
        /// </summary>
        public class ProsTimeDataResult
        {
            public List<ProsTime> ProsTimes { get; set; } = new();
            public List<Memory> CurrentMemories { get; set; } = new();
            public List<Memory> PreviousMemories { get; set; } = new();
            public List<Memory> CylinderMemories { get; set; } = new();
        }

        // デフォルト設定
        private static readonly OperationProsTimeConfig _defaultOperationConfig =
            new() { TotalProsTimeCount = 0, SortIdToCategoryIdMap = new Dictionary<int, int>() };

        public ProsTimeDataBuilder(ISupabaseRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loadedOperationConfigs = LoadOperationProsTimeConfigsFromDb();
        }

        /// <summary>
        /// ProsTimeDefinitionsテーブルから設定を読み込む
        /// </summary>
        private Dictionary<int, OperationProsTimeConfig> LoadOperationProsTimeConfigsFromDb()
        {
            var configs = new Dictionary<int, OperationProsTimeConfig>();

            try
            {
                var rawConfigData = Task.Run(async () => await _repository.GetProsTimeDefinitionsAsync()).GetAwaiter().GetResult();

                if (rawConfigData == null || !rawConfigData.Any())
                {
                    return configs;
                }

                var groupedData = rawConfigData.GroupBy(r => r.OperationCategoryId);

                foreach (var group in groupedData)
                {
                    var operationCategoryKey = group.Key;
                    var totalCount = group.Count();

                    var map = new Dictionary<int, int>();
                    foreach (var item in group)
                    {
                        map[item.SortOrder] = item.OperationDefinitionsId;
                    }

                    configs[operationCategoryKey] = new OperationProsTimeConfig
                    {
                        TotalProsTimeCount = totalCount,
                        SortIdToCategoryIdMap = map
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ProsTime definitions: {ex.Message}");
                return GetDefaultConfigs();
            }

            if (!configs.Any())
            {
                return GetDefaultConfigs();
            }

            return configs;
        }

        /// <summary>
        /// デフォルトのコンフィグレーションを取得
        /// </summary>
        private Dictionary<int, OperationProsTimeConfig> GetDefaultConfigs()
        {
            var configs = new Dictionary<int, OperationProsTimeConfig>();

            // 各CategoryIdに対してデフォルト設定を生成（5個のProsTime）
            for (int categoryId = 1; categoryId <= 20; categoryId++)
            {
                configs[categoryId] = new OperationProsTimeConfig
                {
                    TotalProsTimeCount = 5,
                    SortIdToCategoryIdMap = new Dictionary<int, int>
                    {
                        {0, 1}, {1, 2}, {2, 3}, {3, 4}, {4, 5}
                    }
                };
            }

            return configs;
        }

        /// <summary>
        /// OperationリストからProsTimeとMemoryデータを生成
        /// </summary>
        /// <param name="operations">Operation一覧</param>
        /// <param name="startCurrent">現在時間の開始デバイス番号</param>
        /// <param name="startPrevious">前回時間の開始デバイス番号</param>
        /// <param name="startCylinder">シリンダ時間の開始デバイス番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <returns>生成されたProsTimeとMemoryデータ</returns>
        public async Task<ProsTimeDataResult> BuildProsTimeDataAsync(
            List<Kdx.Contracts.DTOs.Operation> operations,
            int startCurrent,
            int startPrevious,
            int startCylinder,
            int plcId)
        {
            var result = new ProsTimeDataResult();

            // Operationをソート
            operations.Sort((o1, o2) => o1.Id.CompareTo(o2.Id));

            // 必要なデータを取得
            var prosTimeDefinitions = await _repository.GetProsTimeDefinitionsAsync();
            var cylinders = (await _repository.GetCYsAsync())
                .Where(c => c.PlcId == plcId)
                .ToList();

            int count = 0;

            foreach (var operation in operations)
            {
                if (operation == null || operation.CategoryId == null) continue;

                var operationCategoryValue = operation.CategoryId.Value;

                // 現在のOperationのカテゴリに対応するコンフィグを取得
                OperationProsTimeConfig currentConfig = _loadedOperationConfigs.TryGetValue(operationCategoryValue, out var specificConfig)
                                                        ? specificConfig
                                                        : _defaultOperationConfig;

                // 指定されたOperationのCategoryIdに対応するProsTimeDefinitionsを取得
                var prosTimeDefinitionByCategory = prosTimeDefinitions
                    .Where(d => d.OperationCategoryId == operationCategoryValue)
                    .OrderBy(d => d.SortOrder)
                    .ToList();

                foreach (var definition in prosTimeDefinitionByCategory)
                {
                    string currentDevice = "ZR" + (startCurrent + count).ToString();
                    string previousDevice = "ZR" + (startPrevious + count).ToString();
                    string cylinderDevice = "ZR" + (startCylinder + count).ToString();

                    // ProsTimeオブジェクトを作成
                    ProsTime prosTime = new ProsTime
                    {
                        PlcId = plcId,
                        MnemonicId = (int)MnemonicType.Operation,
                        RecordId = operation.Id,
                        SortId = definition.SortOrder,
                        OutcoilNumber = definition.OperationDefinitionsId,
                        CurrentDevice = currentDevice,
                        PreviousDevice = previousDevice,
                        CylinderDevice = cylinderDevice,
                        CategoryId = operation.CategoryId ?? 0
                    };

                    // Cylinderデータを取得
                    var cylinder = cylinders.FirstOrDefault(c => c.Id == operation.CYId);
                    string row2 = cylinder != null ? cylinder.CYNum ?? "NaN" : "NaN";
                    string row3 = definition.Comment1 ?? "";
                    string row4 = definition.Comment2 ?? "";

                    // 現在時間用Memoryオブジェクトを作成
                    Memory currentMemory = new()
                    {
                        PlcId = plcId,
                        Device = currentDevice,
                        MemoryCategory = 5,     // ZR
                        DeviceNumber = startCurrent + count,
                        DeviceNumber1 = (startPrevious + count).ToString(),
                        DeviceNumber2 = "",
                        Category = "工程ﾀｲﾑ",
                        Row_1 = "ﾀｲﾑ現在",
                        Row_2 = row2,
                        Row_3 = row3,
                        Row_4 = row4,
                        Direct_Input = "",
                        Confirm = "",
                        Note = "",
                        GOT = "true",
                        MnemonicId = (int)MnemonicType.Operation,
                        RecordId = operation.Id,
                        OutcoilNumber = 0
                    };

                    // 前回時間用Memoryオブジェクトを作成
                    Memory previousMemory = new()
                    {
                        PlcId = plcId,
                        Device = previousDevice,
                        MemoryCategory = 5,     // ZR
                        DeviceNumber = startPrevious + count,
                        DeviceNumber1 = (startCurrent + count).ToString(),
                        DeviceNumber2 = "",
                        Category = "前工程ﾀｲﾑ",
                        Row_1 = "ﾀｲﾑ前回",
                        Row_2 = row2,
                        Row_3 = row3,
                        Row_4 = row4,
                        Direct_Input = "",
                        Confirm = "",
                        Note = "",
                        GOT = "true",
                        MnemonicId = (int)MnemonicType.Operation,
                        RecordId = operation.Id,
                        OutcoilNumber = 0
                    };

                    // シリンダ時間用Memoryオブジェクトを作成
                    Memory cylinderMemory = new()
                    {
                        PlcId = plcId,
                        Device = cylinderDevice,
                        MemoryCategory = 5,     // ZR
                        DeviceNumber = startCylinder + count,
                        DeviceNumber1 = "",
                        DeviceNumber2 = "",
                        Category = "ｼﾘﾝﾀﾞ",
                        Row_1 = "CYﾀｲﾑ",
                        Row_2 = row2,
                        Row_3 = row3,
                        Row_4 = row4,
                        Direct_Input = "",
                        Confirm = "",
                        Note = "",
                        GOT = "true",
                        MnemonicId = (int)MnemonicType.Operation,
                        RecordId = operation.Id,
                        OutcoilNumber = 0
                    };

                    result.CurrentMemories.Add(currentMemory);
                    result.PreviousMemories.Add(previousMemory);
                    result.CylinderMemories.Add(cylinderMemory);
                    result.ProsTimes.Add(prosTime);

                    count++;
                }
            }

            // 重複を除去（PlcId, MnemonicId, RecordId, SortIdの組み合わせでユニークにする）
            result.ProsTimes = result.ProsTimes
                .GroupBy(pt => new { pt.PlcId, pt.MnemonicId, pt.RecordId, pt.SortId })
                .Select(g => g.First())
                .ToList();

            return result;
        }
    }
}
