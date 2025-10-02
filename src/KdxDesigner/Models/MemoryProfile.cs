using System;

namespace KdxDesigner.Models
{
    public class MemoryProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // デバイス開始アドレス設定
        public int ProcessDeviceStartL { get; set; }
        public int DetailDeviceStartL { get; set; }
        public int OperationDeviceStartM { get; set; }
        public int CylinderDeviceStartM { get; set; }
        public int CylinderDeviceStartD { get; set; }
        public int ErrorDeviceStartM { get; set; }
        public int ErrorDeviceStartT { get; set; }
        public int DeviceStartT { get; set; }
        public int TimerStartZR { get; set; }
        public int ProsTimeStartZR { get; set; }
        public int ProsTimePreviousStartZR { get; set; }
        public int CyTimeStartZR { get; set; }

        // デフォルトプロファイルかどうか
        public bool IsDefault { get; set; }
    }
}