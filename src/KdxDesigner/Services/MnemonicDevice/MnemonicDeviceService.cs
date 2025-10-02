using Kdx.Contracts.DTOs;
using Kdx.Contracts.Enums;
using Kdx.Contracts.Interfaces;
using KdxDesigner.Models;
using System.Data;

namespace KdxDesigner.Services.MnemonicDevice
{
    /// <summary>
    /// ニーモニックデバイスのデータ操作を行うサービス実装
    /// </summary>
    internal class MnemonicDeviceService : IMnemonicDeviceService
    {
        private readonly IAccessRepository _repository;
        private readonly IMemoryService _memoryService;

        static MnemonicDeviceService()
        {
            // Shift_JIS エンコーディングを有効にする
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public MnemonicDeviceService(IAccessRepository repository, IMemoryService memoryService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        }

        /// <summary>
        /// MnemonicDeviceテーブルから、指定されたPLCに基づいてデータを取得する。
        /// </summary>
        /// <param name="plcId"></param>
        /// <returns></returns>
        public List<Kdx.Contracts.DTOs.MnemonicDevice> GetMnemonicDevice(int plcId)
        {
            // IAccessRepository経由で取得
            var dtoDevices = _repository.GetMnemonicDevices(plcId);
            // DTOからModelsへ変換
            return dtoDevices.Select(d => new Kdx.Contracts.DTOs.MnemonicDevice
            {
                ID = d.ID,
                MnemonicId = d.MnemonicId,
                RecordId = d.RecordId,
                DeviceLabel = d.DeviceLabel,
                StartNum = d.StartNum,
                OutCoilCount = d.OutCoilCount,
                PlcId = d.PlcId,
                Comment1 = d.Comment1,
                Comment2 = d.Comment2
            }).ToList();
        }

        /// <summary>
        /// MnemonicDeviceテーブルから、指定されたPLCとMnemonicIdに基づいてデータを取得する。
        /// </summary>
        /// <param name="plcId"></param>
        /// <param name="mnemonicId"></param>
        /// <returns></returns>
        public List<Kdx.Contracts.DTOs.MnemonicDevice> GetMnemonicDeviceByMnemonic(int plcId, int mnemonicId)
        {
            // IAccessRepository経由で取得
            var dtoDevices = _repository.GetMnemonicDevicesByMnemonic(plcId, mnemonicId);
            // DTOからModelsへ変換
            return dtoDevices.Select(d => new Kdx.Contracts.DTOs.MnemonicDevice
            {
                ID = d.ID,
                MnemonicId = d.MnemonicId,
                RecordId = d.RecordId,
                DeviceLabel = d.DeviceLabel,
                StartNum = d.StartNum,
                OutCoilCount = d.OutCoilCount,
                PlcId = d.PlcId,
                Comment1 = d.Comment1,
                Comment2 = d.Comment2
            }).ToList();
        }

        /// <summary>
        /// MnemonicDevice テーブルから、指定された PLC ID と Mnemonic ID に一致するレコードを削除する。
        /// </summary>
        /// <param name="plcId">削除対象のPLC ID</param>
        /// <param name="mnemonicId">削除対象のMnemonic ID</param>
        public void DeleteMnemonicDevice(int plcId, int mnemonicId)
        {
            // IAccessRepository経由で削除
            _repository.DeleteMnemonicDevice(plcId, mnemonicId);
        }

        /// <summary>
        /// MnemonicDevice テーブルの全レコードを削除する。
        /// </summary>
        public void DeleteAllMnemonicDevices()
        {
            // IAccessRepository経由で削除
            _repository.DeleteAllMnemonicDevices();
        }

        /// <summary>
        /// Processesのリストを受け取り、MnemonicDeviceテーブルに保存する。
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        public void SaveMnemonicDeviceProcess(List<Process> processes, int startNum, int plcId)
        {
            // TODO: Supabase対応実装
            // トランザクション処理をIAccessRepository経由で実装する必要がある
            throw new NotImplementedException("Supabase対応が必要です");
        }

        /// <summary>
        /// processDetailのリストを受け取り、MnemonicDeviceテーブルに保存する。
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        public void SaveMnemonicDeviceProcessDetail(List<ProcessDetail> processes, int startNum, int plcId)
        {
            // TODO: Supabase対応実装
            // トランザクション処理をIAccessRepository経由で実装する必要がある
            throw new NotImplementedException("Supabase対応が必要です");
        }

        // Operationのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceOperation(List<Operation> operations, int startNum, int plcId)
        {
            // TODO: Supabase対応実装
            // トランザクション処理をIAccessRepository経由で実装する必要がある
            throw new NotImplementedException("Supabase対応が必要です");
        }

        // Cylinderのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceCY(List<Cylinder> cylinders, int startNum, int plcId)
        {
            // TODO: Supabase対応実装
            // トランザクション処理をIAccessRepository経由で実装する必要がある
            throw new NotImplementedException("Supabase対応が必要です");
        }
    }
}
