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
        List<Kdx.Contracts.DTOs.MnemonicDevice> GetMnemonicDevice(int plcId);

        /// <summary>
        /// 指定されたPLC IDとニーモニックIDのニーモニックデバイスを取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        /// <returns>ニーモニックデバイスのリスト</returns>
        List<Kdx.Contracts.DTOs.MnemonicDevice> GetMnemonicDeviceByMnemonic(int plcId, int mnemonicId);

        /// <summary>
        /// 指定されたPLC IDとニーモニックIDのニーモニックデバイスを削除する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        void DeleteMnemonicDevice(int plcId, int mnemonicId);

        /// <summary>
        /// すべてのニーモニックデバイスを削除する
        /// </summary>
        void DeleteAllMnemonicDevices();

        /// <summary>
        /// プロセスのニーモニックデバイスを保存する
        /// </summary>
        /// <param name="processes">プロセスリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        void SaveMnemonicDeviceProcess(List<Process> processes, int startNum, int plcId);

        /// <summary>
        /// プロセス詳細のニーモニックデバイスを保存する
        /// </summary>
        /// <param name="processes">プロセス詳細リスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        void SaveMnemonicDeviceProcessDetail(List<ProcessDetail> processes, int startNum, int plcId);

        /// <summary>
        /// 操作のニーモニックデバイスを保存する
        /// </summary>
        /// <param name="operations">操作リスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        void SaveMnemonicDeviceOperation(List<Operation> operations, int startNum, int plcId);

        /// <summary>
        /// シリンダーのニーモニックデバイスを保存する
        /// </summary>
        /// <param name="cylinders">シリンダーリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        void SaveMnemonicDeviceCY(List<Cylinder> cylinders, int startNum, int plcId);
    }
}
