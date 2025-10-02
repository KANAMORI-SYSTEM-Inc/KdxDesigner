using Kdx.Contracts.DTOs;

namespace KdxDesigner.Services.MnemonicSpeedDevice
{
    /// <summary>
    /// ニーモニック速度デバイスのデータ操作を行うサービスインターフェース
    /// </summary>
    public interface IMnemonicSpeedDeviceService
    {
        /// <summary>
        /// MnemonicSpeedDeviceテーブルのすべてのレコードを削除する
        /// </summary>
        void DeleteSpeedTable();

        /// <summary>
        /// 指定されたPLC IDのニーモニック速度デバイスを取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <returns>MnemonicSpeedDeviceレコードのリスト</returns>
        List<Kdx.Contracts.DTOs.MnemonicSpeedDevice> GetMnemonicSpeedDevice(int plcId);

        /// <summary>
        /// シリンダーリストから速度デバイス情報を生成し保存する
        /// </summary>
        /// <param name="cys">シリンダーリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        void Save(List<Cylinder> cys, int startNum, int plcId);
    }
}
