using Kdx.Contracts.DTOs;
using KdxDesigner.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace KdxDesigner.Services.ErrorService
{
    /// <summary>
    /// エラー情報のデータ操作を行うサービスインターフェース
    /// </summary>
    public interface IErrorService
    {
        /// <summary>
        /// Errorテーブルのすべてのレコードを削除する
        /// </summary>
        Task DeleteErrorTable();

        /// <summary>
        /// 指定された条件のエラー情報を取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">サイクルID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        /// <returns>エラー情報のリスト</returns>
        Task<List<Kdx.Contracts.DTOs.ProcessError>> GetErrors(int plcId, int cycleId, int mnemonicId);

        /// <summary>
        /// 操作リストからエラー情報を生成し保存する
        /// </summary>
        /// <param name="operations">操作リスト</param>
        /// <param name="iOs">IOリスト</param>
        /// <param name="startNum">開始番号</param>
        /// <param name="startNumTimer">タイマー開始番号</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="cycleId">サイクルID</param>
        Task SaveMnemonicDeviceOperation(
            List<Operation> operations,
            List<IO> iOs,
            int startNum,
            int startNumTimer,
            int plcId,
            int cycleId);
    }
}
