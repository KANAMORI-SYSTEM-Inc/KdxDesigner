namespace KdxDesigner.Models
{
    /// <summary>
    /// Cycle用メモリプロファイル
    /// ProcessDetail/Operationデバイスなど、Cycleごとに複数回適用可能な設定
    /// </summary>
    public class CycleMemoryProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int PlcId { get; set; } = 2;
        public int CycleId { get; set; }

        // Process/ProcessDetailデバイス設定
        public int ProcessDeviceStartL { get; set; } = 14000;
        public int DetailDeviceStartL { get; set; } = 15000;

        // Operationデバイス設定
        public int OperationDeviceStartM { get; set; } = 20000;

        // デフォルトプロファイルかどうか
        public bool IsDefault { get; set; }
    }
}
