using Kdx.Contracts.DTOs;
using Kdx.Contracts.Interfaces;

namespace KdxDesigner.Services.MnemonicSpeedDevice
{
    /// <summary>
    /// ニーモニック速度デバイスのデータ操作を行うサービス実装
    /// </summary>
    internal class MnemonicSpeedDeviceService : IMnemonicSpeedDeviceService
    {
        private readonly IAccessRepository _repository;

        public MnemonicSpeedDeviceService(IAccessRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public void DeleteSpeedTable()
        {
            // TODO: Supabase対応実装
            throw new NotImplementedException("Supabase対応が必要です");
        }

        // MnemonicDeviceテーブルからPlcIdとCycleIdに基づいてデータを取得する
        public List<Kdx.Contracts.DTOs.MnemonicSpeedDevice> GetMnemonicSpeedDevice(int plcId)
        {
            // TODO: Supabase対応実装
            // IAccessRepositoryにGetMnemonicSpeedDevicesメソッドを追加する必要がある
            throw new NotImplementedException("Supabase対応が必要です");
        }

        public void Save(
            List<Cylinder> cys,
            int startNum, int plcId)
        {
            // TODO: Supabase対応実装
            // トランザクション処理をIAccessRepository経由で実装する必要がある
            throw new NotImplementedException("Supabase対応が必要です");
        }
    }
}
