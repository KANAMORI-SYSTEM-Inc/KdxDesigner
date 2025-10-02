using System;
using System.Collections.Generic;
using System.Linq;
using KdxDesigner.Models;
using Kdx.Contracts.DTOs;

namespace KdxDesigner.Services.MnemonicDevice
{
    /// <summary>
    /// MnemonicDevice関連データのメモリ内ストア
    /// データベースではなくアプリケーションメモリで管理することで
    /// パフォーマンスを向上させ、データベース依存を軽減する
    /// </summary>
    public class MnemonicDeviceMemoryStore : IMnemonicDeviceMemoryStore
    {
        private readonly object _lock = new object();
        
        // MnemonicDeviceのメモリストア（PlcId -> List<MnemonicDevice>）
        private readonly Dictionary<int, List<Kdx.Contracts.DTOs.MnemonicDevice>> _mnemonicDevices;
        
        // MnemonicTimerDeviceのメモリストア（PlcId -> CycleId -> List<MnemonicTimerDevice>）
        private readonly Dictionary<int, Dictionary<int, List<MnemonicTimerDevice>>> _timerDevices;
        
        // MnemonicSpeedDeviceのメモリストア（PlcId -> List<MnemonicSpeedDevice>）
        private readonly Dictionary<int, List<Kdx.Contracts.DTOs.MnemonicSpeedDevice>> _speedDevices;
        
        // 生成されたメモリデータのキャッシュ（PlcId -> List<Memory>）
        private readonly Dictionary<int, List<Kdx.Contracts.DTOs.Memory>> _generatedMemories;
        
        private long _nextDeviceId = 1;
        
        public MnemonicDeviceMemoryStore()
        {
            _mnemonicDevices = new Dictionary<int, List<Kdx.Contracts.DTOs.MnemonicDevice>>();
            _timerDevices = new Dictionary<int, Dictionary<int, List<MnemonicTimerDevice>>>();
            _speedDevices = new Dictionary<int, List<Kdx.Contracts.DTOs.MnemonicSpeedDevice>>();
            _generatedMemories = new Dictionary<int, List<Kdx.Contracts.DTOs.Memory>>();
        }
        
        #region MnemonicDevice Operations
        
        /// <summary>
        /// MnemonicDeviceを追加または更新
        /// </summary>
        public void AddOrUpdateMnemonicDevice(Kdx.Contracts.DTOs.MnemonicDevice device, int plcId)
        {
            lock (_lock)
            {
                if (!_mnemonicDevices.ContainsKey(plcId))
                {
                    _mnemonicDevices[plcId] = new List<Kdx.Contracts.DTOs.MnemonicDevice>();
                }
                
                // IDが未設定の場合は新規作成
                if (!device.ID.HasValue)
                {
                    device.ID = _nextDeviceId++;
                }
                
                // 既存のデバイスを検索（MnemonicIdとRecordIdで識別）
                var existing = _mnemonicDevices[plcId]
                    .FirstOrDefault(d => d.MnemonicId == device.MnemonicId && 
                                       d.RecordId == device.RecordId);
                
                if (existing != null)
                {
                    // 更新
                    _mnemonicDevices[plcId].Remove(existing);
                }
                
                _mnemonicDevices[plcId].Add(device);
            }
        }
        
        /// <summary>
        /// 複数のMnemonicDeviceを一括追加
        /// </summary>
        public void BulkAddMnemonicDevices(List<Kdx.Contracts.DTOs.MnemonicDevice> devices, int plcId)
        {
            lock (_lock)
            {
                if (!_mnemonicDevices.ContainsKey(plcId))
                {
                    _mnemonicDevices[plcId] = new List<Kdx.Contracts.DTOs.MnemonicDevice>();
                }
                
                // 新しいデバイスを追加
                foreach (var device in devices)
                {
                    if (!device.ID.HasValue)
                    {
                        device.ID = _nextDeviceId++;
                    }
                    _mnemonicDevices[plcId].Add(device);
                }
            }
        }
        
        /// <summary>
        /// MnemonicDeviceを取得
        /// </summary>
        public List<Kdx.Contracts.DTOs.MnemonicDevice> GetMnemonicDevices(int plcId)
        {
            lock (_lock)
            {
                if (_mnemonicDevices.ContainsKey(plcId))
                {
                    // 防御的コピーを返す
                    return new List<Kdx.Contracts.DTOs.MnemonicDevice>(_mnemonicDevices[plcId]);
                }
                return new List<Kdx.Contracts.DTOs.MnemonicDevice>();
            }
        }
        
        /// <summary>
        /// MnemonicIdでフィルタリングして取得
        /// </summary>
        public List<Kdx.Contracts.DTOs.MnemonicDevice> GetMnemonicDevicesByMnemonic(int plcId, int mnemonicId)
        {
            lock (_lock)
            {
                if (_mnemonicDevices.ContainsKey(plcId))
                {
                    return _mnemonicDevices[plcId]
                        .Where(d => d.MnemonicId == mnemonicId)
                        .ToList();
                }
                return new List<Kdx.Contracts.DTOs.MnemonicDevice>();
            }
        }
        
        /// <summary>
        /// すべてのMnemonicDeviceをクリア
        /// </summary>
        public void ClearAllMnemonicDevices(int plcId)
        {
            lock (_lock)
            {
                if (_mnemonicDevices.ContainsKey(plcId))
                {
                    _mnemonicDevices[plcId].Clear();
                }
                
                // 関連するメモリキャッシュもクリア
                if (_generatedMemories.ContainsKey(plcId))
                {
                    _generatedMemories[plcId].Clear();
                }
            }
        }
        
        #endregion
        
        #region MnemonicTimerDevice Operations
        
        /// <summary>
        /// MnemonicTimerDeviceを追加または更新
        /// </summary>
        public void AddOrUpdateTimerDevice(MnemonicTimerDevice device, int plcId, int cycleId)
        {
            // AddOrUpdateTimerDevice: plcId={plcId}, cycleId={cycleId}
            
            lock (_lock)
            {
                if (!_timerDevices.ContainsKey(plcId))
                {
                    _timerDevices[plcId] = new Dictionary<int, List<MnemonicTimerDevice>>();
                    // PlcId {plcId} の辞書を新規作成
                }
                
                if (!_timerDevices[plcId].ContainsKey(cycleId))
                {
                    _timerDevices[plcId][cycleId] = new List<MnemonicTimerDevice>();
                    // CycleId {cycleId} のリストを新規作成
                }
                
                // 既存のデバイスを検索（複合キーで識別）
                var existing = _timerDevices[plcId][cycleId]
                    .FirstOrDefault(d => d.MnemonicId == device.MnemonicId && 
                                       d.RecordId == device.RecordId &&
                                       d.TimerId == device.TimerId);
                
                if (existing != null)
                {
                    _timerDevices[plcId][cycleId].Remove(existing);
                    // 既存デバイスを削除
                }
                
                _timerDevices[plcId][cycleId].Add(device);
                // デバイスを追加
            }
        }
        
        /// <summary>
        /// MnemonicTimerDeviceを取得
        /// </summary>
        public List<MnemonicTimerDevice> GetTimerDevices(int plcId, int cycleId)
        {
            // GetTimerDevices: plcId={plcId}, cycleId={cycleId}
            
            lock (_lock)
            {
                if (_timerDevices.ContainsKey(plcId))
                {
                    if (_timerDevices[plcId].ContainsKey(cycleId))
                    {
                        return new List<MnemonicTimerDevice>(_timerDevices[plcId][cycleId]);
                    }
                }
                
                return new List<MnemonicTimerDevice>();
            }
        }
        
        /// <summary>
        /// タイマーデバイスをクリア
        /// </summary>
        public void ClearTimerDevices(int plcId, int cycleId)
        {
            lock (_lock)
            {
                if (_timerDevices.ContainsKey(plcId) && 
                    _timerDevices[plcId].ContainsKey(cycleId))
                {
                    _timerDevices[plcId][cycleId].Clear();
                }
            }
        }
        
        #endregion
        
        #region MnemonicSpeedDevice Operations
        
        /// <summary>
        /// MnemonicSpeedDeviceを追加または更新
        /// </summary>
        public void AddOrUpdateSpeedDevice(Kdx.Contracts.DTOs.MnemonicSpeedDevice device, int plcId)
        {
            lock (_lock)
            {
                if (!_speedDevices.ContainsKey(plcId))
                {
                    _speedDevices[plcId] = new List<Kdx.Contracts.DTOs.MnemonicSpeedDevice>();
                }
                
                // 既存のデバイスを検索
                var existing = _speedDevices[plcId]
                    .FirstOrDefault(d => d.CylinderId == device.CylinderId);
                
                if (existing != null)
                {
                    _speedDevices[plcId].Remove(existing);
                }
                
                _speedDevices[plcId].Add(device);
            }
        }
        
        /// <summary>
        /// MnemonicSpeedDeviceを取得
        /// </summary>
        public List<Kdx.Contracts.DTOs.MnemonicSpeedDevice> GetSpeedDevices(int plcId)
        {
            lock (_lock)
            {
                if (_speedDevices.ContainsKey(plcId))
                {
                    return new List<Kdx.Contracts.DTOs.MnemonicSpeedDevice>(_speedDevices[plcId]);
                }
                return new List<Kdx.Contracts.DTOs.MnemonicSpeedDevice>();
            }
        }
        
        /// <summary>
        /// スピードデバイスをクリア
        /// </summary>
        public void ClearSpeedDevices(int plcId)
        {
            lock (_lock)
            {
                if (_speedDevices.ContainsKey(plcId))
                {
                    _speedDevices[plcId].Clear();
                }
            }
        }
        
        #endregion
        
        #region Generated Memory Cache
        
        /// <summary>
        /// 生成されたメモリデータをキャッシュ
        /// </summary>
        public void CacheGeneratedMemories(List<Kdx.Contracts.DTOs.Memory> memories, int plcId)
        {
            lock (_lock)
            {
                _generatedMemories[plcId] = new List<Kdx.Contracts.DTOs.Memory>(memories);
            }
        }
        
        /// <summary>
        /// キャッシュされたメモリデータを取得
        /// </summary>
        public List<Kdx.Contracts.DTOs.Memory> GetCachedMemories(int plcId)
        {
            lock (_lock)
            {
                if (_generatedMemories.ContainsKey(plcId))
                {
                    return new List<Kdx.Contracts.DTOs.Memory>(_generatedMemories[plcId]);
                }
                return new List<Kdx.Contracts.DTOs.Memory>();
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// すべてのデータをクリア
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _mnemonicDevices.Clear();
                _timerDevices.Clear();
                _speedDevices.Clear();
                _generatedMemories.Clear();
                _nextDeviceId = 1;
            }
        }
        
        /// <summary>
        /// 特定のPLCのすべてのデータをクリア
        /// </summary>
        public void ClearByPlc(int plcId)
        {
            lock (_lock)
            {
                _mnemonicDevices.Remove(plcId);
                _timerDevices.Remove(plcId);
                _speedDevices.Remove(plcId);
                _generatedMemories.Remove(plcId);
            }
        }
        
        /// <summary>
        /// データが存在するかチェック
        /// </summary>
        public bool HasData(int plcId)
        {
            lock (_lock)
            {
                return (_mnemonicDevices.ContainsKey(plcId) && _mnemonicDevices[plcId].Any()) ||
                       (_timerDevices.ContainsKey(plcId) && _timerDevices[plcId].Any()) ||
                       (_speedDevices.ContainsKey(plcId) && _speedDevices[plcId].Any());
            }
        }
        
        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public MnemonicDeviceStatistics GetStatistics(int plcId)
        {
            lock (_lock)
            {
                return new MnemonicDeviceStatistics
                {
                    MnemonicDeviceCount = _mnemonicDevices.ContainsKey(plcId) 
                        ? _mnemonicDevices[plcId].Count : 0,
                    TimerDeviceCount = _timerDevices.ContainsKey(plcId) 
                        ? _timerDevices[plcId].Values.SelectMany(v => v).Count() : 0,
                    SpeedDeviceCount = _speedDevices.ContainsKey(plcId) 
                        ? _speedDevices[plcId].Count : 0,
                    CachedMemoryCount = _generatedMemories.ContainsKey(plcId) 
                        ? _generatedMemories[plcId].Count : 0
                };
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 統計情報クラス
    /// </summary>
    public class MnemonicDeviceStatistics
    {
        public int MnemonicDeviceCount { get; set; }
        public int TimerDeviceCount { get; set; }
        public int SpeedDeviceCount { get; set; }
        public int CachedMemoryCount { get; set; }
    }
}
