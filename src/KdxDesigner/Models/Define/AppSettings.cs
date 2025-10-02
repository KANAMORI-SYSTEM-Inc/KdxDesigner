using System.ComponentModel;

namespace KdxDesigner.Models.Define
{
    public class AppSettings
    {
        [Description("エラーリセット信号")]
        public string ErrorResetSignal { get; set; } = "M880";

        [Description("リセット信号")]
        public string ResetSignal { get; set; } = "M880";

        [Description("一時停止信号")]
        public string PauseSignal { get; set; } = "M207";

        [Description("一時停止遅延信号")]
        public string PauseDelaySignal { get; set; } = "M208";

        [Description("ソフトリセット信号")]
        public string SoftResetSignal { get; set; } = "M100";

        [Description("アドレスオフセット")]
        public int AddressOffset { get; set; } = 1000;

        [Description("タイマーアドレスオフセット")]
        public int TimerAddressOffset { get; set; } = 2000;

        [Description("OFF確認信号")]
        public string OffConfirmSignal { get; set; } = "M4000";

        [Description("常時ON信号")]
        public string AlwaysON { get; set; } = "SM400";

        [Description("常時OFF信号")]
        public string AlwaysOFF { get; set; } = "SM401";

        [Description("0.1秒クロック信号")]
        public string Clock01 { get; set; } = "SM410";

        [Description("サイクルタイムデバイス")]
        public string CycleTime { get; set; } = "D400";

        [Description("エラー格納デバイス")]
        public string ErrorDevice { get; set; } = "D222";

        [Description("出力エラーデバイス")]
        public string OutErrorDevice { get; set; } = "M9999";

        [Description("デバッグパルス信号")]
        public string DebugPulse { get; set; } = "M500";

        [Description("デバッグテスト")]
        public string DebugTest { get; set; } = "M501";

        [Description("バルブ検索文字列")]
        public string ValveSearchWord { get; set; } = "SV";

        [Description("前回選択した会社ID")]
        public int? LastSelectedCompanyId { get; set; }

        [Description("前回選択したモデルID")]
        public int? LastSelectedModelId { get; set; }

        [Description("前回選択したサイクルID")]
        public int? LastSelectedCycleId { get; set; }

        [Description("前回使用したメモリプロファイルID")]
        public string? LastUsedMemoryProfileId { get; set; }
    }
}
