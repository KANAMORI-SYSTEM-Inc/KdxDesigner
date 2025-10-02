using Kdx.Contracts.DTOs;
using KdxDesigner.Models;

using System.Collections.Generic;
using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.Services.MemonicTimerDevice
{
    /// <summary>
    /// MnemonicTimerDeviceのデータ操作を行うサービスインターフェース
    /// </summary>
    public interface IMnemonicTimerDeviceService
    {
        /// <summary>
        /// PlcIdとCycleIdに基づいてMnemonicTimerDeviceを取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">サイクルID</param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        List<MnemonicTimerDevice> GetMnemonicTimerDevice(int plcId, int cycleId);

        /// <summary>
        /// MnemonicTimerDeviceをPlcId、CycleId、MnemonicIdで取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">サイクルID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        List<MnemonicTimerDevice> GetMnemonicTimerDeviceByCycle(int plcId, int cycleId, int mnemonicId);

        /// <summary>
        /// MnemonicTimerDeviceをPlcIdとMnemonicIdで取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        List<MnemonicTimerDevice> GetMnemonicTimerDeviceByMnemonic(int plcId, int mnemonicId);

        /// <summary>
        /// MnemonicTimerDeviceをPlcIdとTimerIdで取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="timerId">タイマーID</param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        List<MnemonicTimerDevice> GetMnemonicTimerDeviceByTimerId(int plcId, int timerId);

        /// <summary>
        /// ProcessDetailのタイマーデバイスを保存する
        /// </summary>
        /// <param name="timers">タイマーリスト</param>
        /// <param name="details">ProcessDetailのリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="count">カウント（参照渡し）</param>
        void SaveWithDetail(List<Timer> timers, List<ProcessDetail> details, int startNum, int plcId, ref int count);

        /// <summary>
        /// Operationのタイマーデバイスを保存する
        /// </summary>
        /// <param name="timers">タイマーリスト</param>
        /// <param name="operations">Operationのリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="count">カウント（参照渡し）</param>
        void SaveWithOperation(List<Timer> timers, List<Operation> operations, int startNum, int plcId, ref int count);

        /// <summary>
        /// CY（シリンダー）のタイマーデバイスを保存する
        /// </summary>
        /// <param name="timers">タイマーリスト</param>
        /// <param name="cylinders">CYのリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="count">カウント（参照渡し）</param>
        void SaveWithCY(List<Timer> timers, List<Cylinder> cylinders, int startNum, int plcId, ref int count);
    }
}
