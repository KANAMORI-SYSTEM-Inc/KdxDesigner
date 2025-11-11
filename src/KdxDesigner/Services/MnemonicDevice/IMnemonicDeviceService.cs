using Kdx.Contracts.DTOs;
using KdxDesigner.Models;

using System.Collections.Generic;

namespace KdxDesigner.Services.MnemonicDevice
{
    /// <summary>
    /// ニーモニックデバイスのデータ操作を行うサービスインターフェース
    /// </summary>
    public interface IMnemonicDeviceService
    {
        /// <summary>
        /// 指定されたPLC IDのニーモニックデバイスを取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <returns>ニーモニックデバイスのリスト</returns>
        Task<List<Kdx.Contracts.DTOs.MnemonicDevice>> GetMnemonicDevice(int plcId);

        /// <summary>
        /// 指定されたPLC IDとニーモニックIDのニーモニックデバイスを取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        /// <returns>ニーモニックデバイスのリスト</returns>
        Task<List<Kdx.Contracts.DTOs.MnemonicDevice>> GetMnemonicDeviceByMnemonic(int plcId, int mnemonicId);

        /// <summary>
        /// 指定されたPLC IDとニーモニックIDのニーモニックデバイスを削除する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        Task DeleteMnemonicDevice(int plcId, int mnemonicId);

        /// <summary>
        /// すべてのニーモニックデバイスを削除する
        /// </summary>
        Task DeleteAllMnemonicDevices();

        /// <summary>
        /// プロセスのニーモニックデバイスを保存する（Cycle用プロファイル）
        /// </summary>
        /// <param name="processes">プロセスリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">Cycle ID（Cycle用プロファイル使用時に設定）</param>
        void SaveMnemonicDeviceProcess(List<Process> processes, int startNum, int plcId, int? cycleId = null);

        /// <summary>
        /// プロセス詳細のニーモニックデバイスを保存する（Cycle用プロファイル）
        /// </summary>
        /// <param name="processes">プロセス詳細リスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">Cycle ID（Cycle用プロファイル使用時に設定）</param>
        Task SaveMnemonicDeviceProcessDetail(List<ProcessDetail> processes, int startNum, int plcId, int? cycleId = null);

        /// <summary>
        /// 操作のニーモニックデバイスを保存する（Cycle用プロファイル）
        /// </summary>
        /// <param name="operations">操作リスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">Cycle ID（Cycle用プロファイル使用時に設定）</param>
        void SaveMnemonicDeviceOperation(List<Operation> operations, int startNum, int plcId, int? cycleId = null);

        /// <summary>
        /// シリンダーのニーモニックデバイスを保存する（PLC用プロファイル - CycleIdはnull）
        /// </summary>
        /// <param name="cylinders">シリンダーリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">Cycle ID（PLC用プロファイルなので常にnull）</param>
        Task SaveMnemonicDeviceCY(List<Cylinder> cylinders, int startNum, int plcId, int? cycleId = null);
    }
}
