using CommunityToolkit.Mvvm.ComponentModel;
using KdxDesigner.Services.MnemonicDevice;

namespace KdxDesigner.ViewModels.Managers
{
    /// <summary>
    /// メモリ設定状態を管理するマネージャークラス
    /// メモリデバイス数やメモリ設定状態の追跡を行う
    /// </summary>
    public partial class MemoryConfigurationManager : ObservableObject
    {
        private readonly IMnemonicDeviceMemoryStore _memoryStore;

        [ObservableProperty] private int _totalMemoryDeviceCount = 0;
        [ObservableProperty] private string _memoryConfigurationStatus = "未設定";
        [ObservableProperty] private bool _isMemoryConfigured = false;
        [ObservableProperty] private string _lastMemoryConfigTime = string.Empty;

        public MemoryConfigurationManager(IMnemonicDeviceMemoryStore memoryStore)
        {
            _memoryStore = memoryStore ?? throw new ArgumentNullException(nameof(memoryStore));
        }

        /// <summary>
        /// メモリ設定状態を更新
        /// </summary>
        public void UpdateConfigurationStatus(int plcId, int? cycleId)
        {
            if (cycleId == null)
            {
                MemoryConfigurationStatus = "未設定";
                IsMemoryConfigured = false;
                TotalMemoryDeviceCount = 0;
                LastMemoryConfigTime = string.Empty;
                return;
            }

            // メモリストアからキャッシュされたメモリ数を取得
            var cachedMemories = _memoryStore.GetCachedMemories(plcId);
            TotalMemoryDeviceCount = cachedMemories.Count;

            if (TotalMemoryDeviceCount > 0)
            {
                MemoryConfigurationStatus = $"設定済み ({TotalMemoryDeviceCount}件)";
                IsMemoryConfigured = true;
                LastMemoryConfigTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            }
            else
            {
                MemoryConfigurationStatus = "未設定";
                IsMemoryConfigured = false;
                LastMemoryConfigTime = string.Empty;
            }
        }

        /// <summary>
        /// メモリ設定をクリア
        /// </summary>
        public void ClearConfiguration()
        {
            MemoryConfigurationStatus = "未設定";
            IsMemoryConfigured = false;
            TotalMemoryDeviceCount = 0;
            LastMemoryConfigTime = string.Empty;
        }

        /// <summary>
        /// メモリストアからデバイス数を取得
        /// </summary>
        public int GetDeviceCount(int plcId, int cycleId)
        {
            var devices = _memoryStore.GetMnemonicDevices(plcId);
            var timerDevices = _memoryStore.GetTimerDevices(plcId, cycleId);
            var speedDevices = _memoryStore.GetSpeedDevices(plcId);

            return devices.Count + timerDevices.Count + speedDevices.Count;
        }

        /// <summary>
        /// キャッシュされたメモリ数を取得
        /// </summary>
        public int GetCachedMemoryCount(int plcId)
        {
            return _memoryStore.GetCachedMemories(plcId).Count;
        }
    }
}
