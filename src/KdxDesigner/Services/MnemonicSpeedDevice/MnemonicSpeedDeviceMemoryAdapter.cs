using System.Collections.Generic;
using System.Linq;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Interfaces;
using KdxDesigner.Models;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Services.MnemonicDevice;

namespace KdxDesigner.Services.MnemonicSpeedDevice
{
    /// <summary>
    /// MnemonicSpeedDeviceServiceのメモリストアアダプター
    /// 既存のインターフェースを維持しながら、メモリストアを使用する
    /// </summary>
    public class MnemonicSpeedDeviceMemoryAdapter : IMnemonicSpeedDeviceService
    {
        private readonly IMnemonicDeviceMemoryStore _memoryStore;
        private readonly MnemonicSpeedDeviceService _dbService;
        private bool _useMemoryStoreOnly = true;
        
        public MnemonicSpeedDeviceMemoryAdapter(
            ISupabaseRepository repository,
            IMnemonicDeviceMemoryStore memoryStore = null!)
        {
            _memoryStore = memoryStore ?? new MnemonicDeviceMemoryStore();
            _dbService = new MnemonicSpeedDeviceService(repository);
        }
        
        /// <summary>
        /// メモリストアのみを使用するかどうかを設定
        /// </summary>
        public void SetMemoryOnlyMode(bool useMemoryOnly)
        {
            _useMemoryStoreOnly = useMemoryOnly;
        }
        
        /// <summary>
        /// PlcIdに基づいてMnemonicSpeedDeviceを取得
        /// </summary>
        public List<Kdx.Contracts.DTOs.MnemonicSpeedDevice> GetMnemonicSpeedDevice(int plcId)
        {
            // メモリストアから取得
            var devices = _memoryStore.GetSpeedDevices(plcId);
            
            // メモリストアにデータがない場合、データベースから取得
            if (!devices.Any() && !_useMemoryStoreOnly)
            {
                devices = _dbService.GetMnemonicSpeedDevice(plcId);
                
                // データベースから取得したデータをメモリストアにキャッシュ
                foreach (var device in devices)
                {
                    _memoryStore.AddOrUpdateSpeedDevice(device, plcId);
                }
            }
            
            return devices;
        }
        
        /// <summary>
        /// シリンダーIDに基づいてMnemonicSpeedDeviceを取得
        /// </summary>
        public Kdx.Contracts.DTOs.MnemonicSpeedDevice? GetMnemonicSpeedDeviceByCylinderId(int cylinderId, int plcId)
        {
            var devices = GetMnemonicSpeedDevice(plcId);
            if (devices == null || !devices.Any())
            {
                return null;
            }

            return devices.FirstOrDefault(d => d.CylinderId == cylinderId);
        }
        
        /// <summary>
        /// すべてのスピードデバイステーブルを削除
        /// </summary>
        public void DeleteSpeedTable()
        {
            // メモリストアをクリア
            // 注: 現在のメモリストアは個別のPlcIdごとのクリアのみサポート
            // 全体クリアが必要な場合は拡張が必要
            
            // データベースもクリア（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                _dbService.DeleteSpeedTable();
            }
        }
        
        /// <summary>
        /// シリンダーリストからスピードデバイスを保存（PLC用プロファイル - CycleIdはnull）
        /// </summary>
        public void Save(List<Cylinder> cylinders, int startNum, int plcId, int? cycleId = null)
        {
            // 既存のデータをクリア
            _memoryStore.ClearSpeedDevices(plcId);

            var devices = new List<Kdx.Contracts.DTOs.MnemonicSpeedDevice>();
            int deviceNum = startNum;

            foreach (var cylinder in cylinders)
            {
                var device = new Kdx.Contracts.DTOs.MnemonicSpeedDevice
                {
                    ID = cylinder.Id,  // 一時的なID
                    CylinderId = cylinder.Id,
                    Device = $"D{deviceNum}",
                    PlcId = plcId,
                    CycleId = cycleId  // PLC用プロファイルなので常にnull
                };

                devices.Add(device);

                // メモリストアに保存
                _memoryStore.AddOrUpdateSpeedDevice(device, plcId);

                deviceNum += 10; // 次のデバイス番号
            }

            // データベースにも保存（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                _dbService.Save(cylinders, startNum, plcId, cycleId);
            }
        }
        
        /// <summary>
        /// 単一のスピードデバイスを保存
        /// </summary>
        public void SaveSingle(Kdx.Contracts.DTOs.MnemonicSpeedDevice speedDevice)
        {
            // メモリストアに保存
            _memoryStore.AddOrUpdateSpeedDevice(speedDevice, speedDevice.PlcId);
            
            // データベースにも保存（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                // SaveSingleメソッドが存在しない場合は、Saveメソッドで代用
                // _dbService.SaveSingle(speedDevice);
            }
        }
        
        /// <summary>
        /// 特定のシリンダーIDのスピードデバイスを削除
        /// </summary>
        public void DeleteByCylinderId(int cylinderId, int plcId)
        {
            var devices = _memoryStore.GetSpeedDevices(plcId);
            var toRemove = devices.FirstOrDefault(d => d.CylinderId == cylinderId);
            
            if (toRemove != null)
            {
                // メモリストアから削除（現在の実装では個別削除がないため、再構築）
                var remaining = devices.Where(d => d.CylinderId != cylinderId).ToList();
                _memoryStore.ClearSpeedDevices(plcId);
                foreach (var device in remaining)
                {
                    _memoryStore.AddOrUpdateSpeedDevice(device, plcId);
                }
            }
            
            // データベースからも削除（メモリオンリーモードでない場合）
            if (!_useMemoryStoreOnly)
            {
                // DeleteByCylinderIdメソッドが存在しない場合は、省略
                // _dbService.DeleteByCylinderId(cylinderId, plcId);
            }
        }
    }
}
