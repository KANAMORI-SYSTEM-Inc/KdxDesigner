using Kdx.Contracts.DTOs;

namespace KdxDesigner.Services.MnemonicDevice
{
    /// <summary>
    /// MnemonicDevice関連データのメモリストアインターフェース
    /// </summary>
    public interface IMnemonicDeviceMemoryStore
    {
        #region MnemonicDevice Operations
        
        /// <summary>
        /// MnemonicDeviceを追加または更新
        /// </summary>
        void AddOrUpdateMnemonicDevice(Kdx.Contracts.DTOs.MnemonicDevice device, int plcId);
        
        /// <summary>
        /// 複数のMnemonicDeviceを一括追加
        /// </summary>
        void BulkAddMnemonicDevices(List<Kdx.Contracts.DTOs.MnemonicDevice> devices, int plcId);
        
        /// <summary>
        /// MnemonicDeviceを取得
        /// </summary>
        List<Kdx.Contracts.DTOs.MnemonicDevice> GetMnemonicDevices(int plcId);
        
        /// <summary>
        /// MnemonicIdでフィルタリングして取得
        /// </summary>
        List<Kdx.Contracts.DTOs.MnemonicDevice> GetMnemonicDevicesByMnemonic(int plcId, int mnemonicId);
        
        /// <summary>
        /// すべてのMnemonicDeviceをクリア
        /// </summary>
        void ClearAllMnemonicDevices(int plcId);

        /// <summary>
        /// 特定のMnemonicIdのデバイスを削除
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">削除するMnemonic ID</param>
        void DeleteMnemonicDevicesByMnemonicId(int plcId, int mnemonicId);

        #endregion
        
        #region MnemonicTimerDevice Operations
        
        /// <summary>
        /// MnemonicTimerDeviceを追加または更新
        /// </summary>
        void AddOrUpdateTimerDevice(MnemonicTimerDevice device, int plcId, int cycleId);
        
        /// <summary>
        /// MnemonicTimerDeviceを取得
        /// </summary>
        List<MnemonicTimerDevice> GetTimerDevices(int plcId, int cycleId);
        
        /// <summary>
        /// タイマーデバイスをクリア
        /// </summary>
        void ClearTimerDevices(int plcId, int cycleId);
        
        #endregion
        
        #region MnemonicSpeedDevice Operations
        
        /// <summary>
        /// MnemonicSpeedDeviceを追加または更新
        /// </summary>
        void AddOrUpdateSpeedDevice(Kdx.Contracts.DTOs.MnemonicSpeedDevice device, int plcId);
        
        /// <summary>
        /// MnemonicSpeedDeviceを取得
        /// </summary>
        List<Kdx.Contracts.DTOs.MnemonicSpeedDevice> GetSpeedDevices(int plcId);
        
        /// <summary>
        /// スピードデバイスをクリア
        /// </summary>
        void ClearSpeedDevices(int plcId);
        
        #endregion
        
        #region Generated Memory Cache
        
        /// <summary>
        /// 生成されたメモリデータをキャッシュ
        /// </summary>
        void CacheGeneratedMemories(List<Kdx.Contracts.DTOs.Memory> memories, int plcId);
        
        /// <summary>
        /// キャッシュされたメモリデータを取得
        /// </summary>
        List<Kdx.Contracts.DTOs.Memory> GetCachedMemories(int plcId);
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// すべてのデータをクリア
        /// </summary>
        void ClearAll();
        
        /// <summary>
        /// 特定のPLCのすべてのデータをクリア
        /// </summary>
        void ClearByPlc(int plcId);
        
        /// <summary>
        /// データが存在するかチェック
        /// </summary>
        bool HasData(int plcId);
        
        /// <summary>
        /// 統計情報を取得
        /// </summary>
        MnemonicDeviceStatistics GetStatistics(int plcId);
        
        #endregion
    }
}
