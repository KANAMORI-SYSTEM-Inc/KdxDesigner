using Kdx.Contracts.DTOs;
using KdxDesigner.Models;
using KdxDesigner.ViewModels;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace KdxDesigner.Services.LinkDevice
{
    /// <summary>
    /// リンクデバイスのデータ操作を行うサービスインターフェース
    /// </summary>
    public interface ILinkDeviceService
    {
        /// <summary>
        /// リンクデバイスレコードを作成する
        /// </summary>
        /// <param name="mainPlc">メインPLC</param>
        /// <param name="selectedSettings">選択されたPLCリンク設定のリスト</param>
        /// <returns>処理成功の場合はtrue</returns>
        Task<bool> CreateLinkDeviceRecords(PLC mainPlc, List<PlcLinkSettingViewModel> selectedSettings);

        /// <summary>
        /// リンクデバイス情報をCSVファイルにエクスポートする
        /// </summary>
        /// <param name="filePath">出力ファイルパス</param>
        Task ExportLinkDeviceCsv(string filePath);
    }
}