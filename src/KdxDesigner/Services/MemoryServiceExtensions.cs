using Kdx.Contracts.DTOs;
using Kdx.Contracts.Interfaces;

using System.Diagnostics;

namespace KdxDesigner.Services
{
    /// <summary>
    /// MemoryServiceのKdxDesigner特有の拡張メソッド
    /// UI層に特化したロジックをここに配置
    /// </summary>
    public static class MemoryServiceExtensions
    {
        public static bool SaveMnemonicMemories(
            this IMemoryService memoryService,
            IAccessRepository repository,
            Kdx.Contracts.DTOs.MnemonicDevice device)
        {
            if (device?.PlcId == null) return false; // PlcId が必須

            try
            {
                var existingForPlcIdList = memoryService.GetMemories(device.PlcId);
                var existingLookup = existingForPlcIdList
                    .Where(m => !string.IsNullOrEmpty(m.Device))
                    .ToDictionary(m => m.Device!, m => m); // Deviceで検索 (PlcIdは共通)

                var deviceLabelCategoryId = device.DeviceLabel switch
                {
                    "L" => 1,
                    "M" => 2,
                    "B" => 3,
                    "D" => 4,
                    "ZR" => 5,
                    "W" => 6,
                    "T" => 7,
                    "C" => 8,
                    _ => 1, // TODO: エラー処理または明確なデフォルト値
                };

                var mnemonicTypeBasedCategoryString = device.MnemonicId switch
                {
                    1 => "工程",
                    2 => "工程詳細",
                    3 => "操作",
                    4 => "出力",
                    _ => "なし", // TODO: エラー処理または明確なデフォルト値
                };

                var difinitions = device.MnemonicId switch
                {
                    1 => repository.GetDifinitions("Process"),
                    2 => repository.GetDifinitions("ProcessDetail"),
                    3 => repository.GetDifinitions("Operation"),
                    4 => repository.GetDifinitions("CY"),
                    _ => new List<Definitions>()
                };

                var memoriesToSave = new List<Memory>();

                var startNum = device.StartNum;
                var endNum = device.StartNum + device.OutCoilCount - 1;

                for (var num = startNum; num <= endNum; num++)
                {
                    var deviceString = $"{device.DeviceLabel}{num}";

                    var deviceDefinitionString = "";
                    var numWithinMnemonic = num - startNum;
                    if (numWithinMnemonic < difinitions.Count)
                    {
                        deviceDefinitionString = difinitions[numWithinMnemonic].DefName ?? "";
                    }

                    // 既存レコードをチェック
                    if (existingLookup.TryGetValue(deviceString, out var existing))
                    {
                        // 既存のデータがある場合はスキップ（更新しない）
                        continue;
                    }

                    var memory = new Memory
                    {
                        PlcId = device.PlcId,
                        MemoryCategory = deviceLabelCategoryId,
                        Device = deviceString,
                        Category = mnemonicTypeBasedCategoryString,
                        Note = deviceDefinitionString, // 定義名をNoteフィールドに保存
                        MnemonicId = device.MnemonicId,
                        RecordId = device.RecordId,
                        OutcoilNumber = numWithinMnemonic
                    };

                    memoriesToSave.Add(memory);
                }

                if (memoriesToSave.Any())
                {
                    repository.SaveOrUpdateMemoriesBatch(memoriesToSave);
                    Debug.WriteLine($"保存されたメモリデバイス数: {memoriesToSave.Count} (Device: {device.DeviceLabel}, MnemonicId: {device.MnemonicId}, RecordId: {device.RecordId})");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] SaveMnemonicMemories failed: {ex.Message}");
                return false;
            }
        }
    }
}
