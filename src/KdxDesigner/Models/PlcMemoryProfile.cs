namespace KdxDesigner.Models
{
    /// <summary>
    /// PLC用メモリプロファイル
    /// Cylinderデバイスなど、PLC全体に対して1回のみ適用される設定
    /// </summary>
    public class PlcMemoryProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int PlcId { get; set; } = 2;

        // Cylinderデバイス設定（PLC全体で1回のみ）
        public int CylinderDeviceStartM { get; set; } = 30000;
        public int CylinderDeviceStartD { get; set; } = 5000;

        // Errorデバイス設定（PLC全体で1回のみ）
        public int ErrorDeviceStartM { get; set; } = 120000;
        public int ErrorDeviceStartT { get; set; } = 2000;

        // Timerデバイス設定（PLC全体で1回のみ）
        public int DeviceStartT { get; set; } = 0;
        public int TimerStartZR { get; set; } = 3000;

        // 工程時間デバイス設定（PLC全体で1回のみ）
        public int ProsTimeStartZR { get; set; } = 12000;
        public int ProsTimePreviousStartZR { get; set; } = 24000;
        public int CyTimeStartZR { get; set; } = 30000;

        // デフォルトプロファイルかどうか
        public bool IsDefault { get; set; }
    }
}
