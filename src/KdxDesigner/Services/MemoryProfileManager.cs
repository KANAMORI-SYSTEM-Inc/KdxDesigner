using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.IO;

namespace KdxDesigner.Services
{
    /// <summary>
    /// はしご君で利用するラダーメモリプロファイルの管理
    /// </summary>
    public class MemoryProfileManager
    {
        private readonly string _profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MemoryProfiles");
        private readonly string _profilesFile = "memory_profiles.json";
        private readonly string _profilesFilePath;
        private readonly ISupabaseRepository _repository;

        public MemoryProfileManager(ISupabaseRepository repository)
        {
            _repository = repository;
            _profilesFilePath = Path.Combine(_profilesDirectory, _profilesFile);
            EnsureDirectoryExists();
            InitializeDefaultProfiles();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_profilesDirectory))
            {
                Directory.CreateDirectory(_profilesDirectory);
            }
        }

        private void InitializeDefaultProfiles()
        {
            if (!_repository.GetMemoryProfilesAsync().Result.Any())
            {
                // デフォルトプロファイルを作成
                var defaultProfiles = new List<MemoryProfile>
                {
                    new MemoryProfile
                    {
                        CycleId = 1,
                        Name = "ライン",
                        Description = "標準的なライン設定",
                        ProcessDeviceStartL = 14000,
                        DetailDeviceStartL = 15000,
                        OperationDeviceStartM = 20000,
                        CylinderDeviceStartM = 30000,
                        CylinderDeviceStartD = 5000,
                        ErrorDeviceStartM = 120000,
                        ErrorDeviceStartT = 2000,
                        DeviceStartT = 0,
                        TimerStartZR = 3000,
                        ProsTimeStartZR = 12000,
                        ProsTimePreviousStartZR = 24000,
                        CyTimeStartZR = 30000,
                        IsDefault = true
                    },
                    new MemoryProfile
                    {
                        CycleId = 2,
                        Name = "上型造型機",
                        Description = "上型造型機用の設定",
                        ProcessDeviceStartL = 14300,
                        DetailDeviceStartL = 17000,
                        OperationDeviceStartM = 26000,
                        CylinderDeviceStartM = 50000,
                        CylinderDeviceStartD = 5200,
                        ErrorDeviceStartM = 121500,
                        ErrorDeviceStartT = 3500,
                        DeviceStartT = 0,
                        TimerStartZR = 3000,
                        ProsTimeStartZR = 14000,
                        ProsTimePreviousStartZR = 26000,
                        CyTimeStartZR = 32000
                    },
                    new MemoryProfile
                    {
                        CycleId = 3,
                        Name = "下型造型機",
                        Description = "下型造型機用の設定",
                        ProcessDeviceStartL = 14500,
                        DetailDeviceStartL = 18000,
                        OperationDeviceStartM = 28000,
                        CylinderDeviceStartM = 52000,
                        CylinderDeviceStartD = 5300,
                        ErrorDeviceStartM = 121700,
                        ErrorDeviceStartT = 3700,
                        DeviceStartT = 0,
                        TimerStartZR = 3000,
                        ProsTimeStartZR = 15000,
                        ProsTimePreviousStartZR = 27000,
                        CyTimeStartZR = 33000
                    },
                    new MemoryProfile
                    {
                        CycleId = 4,
                        Name = "上型型交換",
                        Description = "上型型交換用の設定",
                        ProcessDeviceStartL = 14700,
                        DetailDeviceStartL = 19000,
                        OperationDeviceStartM = 28000,
                        CylinderDeviceStartM = 52000,
                        CylinderDeviceStartD = 5300,
                        ErrorDeviceStartM = 121700,
                        ErrorDeviceStartT = 3700,
                        DeviceStartT = 0,
                        TimerStartZR = 3000,
                        ProsTimeStartZR = 15000,
                        ProsTimePreviousStartZR = 27000,
                        CyTimeStartZR = 33000
                    },
                    new MemoryProfile
                    {
                        CycleId = 5,
                        Name = "下型型交換",
                        Description = "下型型交換用の設定",
                        ProcessDeviceStartL = 14500,
                        DetailDeviceStartL = 18000,
                        OperationDeviceStartM = 28000,
                        CylinderDeviceStartM = 52000,
                        CylinderDeviceStartD = 5300,
                        ErrorDeviceStartM = 121700,
                        ErrorDeviceStartT = 3700,
                        DeviceStartT = 0,
                        TimerStartZR = 3000,
                        ProsTimeStartZR = 15000,
                        ProsTimePreviousStartZR = 27000,
                        CyTimeStartZR = 33000
                    },
                    new MemoryProfile
                    {
                        CycleId = 6,
                        Name = "中子ｾｯﾄ",
                        Description = "PlcId = 4の中子ｾｯﾄ",
                        ProcessDeviceStartL = 14000,
                        DetailDeviceStartL = 15000,
                        OperationDeviceStartM = 20000,
                        CylinderDeviceStartM = 30000,
                        CylinderDeviceStartD = 5000,
                        ErrorDeviceStartM = 120000,
                        ErrorDeviceStartT = 2000,
                        DeviceStartT = 0,
                        TimerStartZR = 3000,
                        ProsTimeStartZR = 12000,
                        ProsTimePreviousStartZR = 24000,
                        CyTimeStartZR = 30000,
                        IsDefault = true
                    },
                    new MemoryProfile
                    {
                        CycleId = 7,
                        Name = "治具交換",
                        Description = "治具交換用の設定",
                        ProcessDeviceStartL = 14300,
                        DetailDeviceStartL = 17000,
                        OperationDeviceStartM = 26000,
                        CylinderDeviceStartM = 50000,
                        CylinderDeviceStartD = 5200,
                        ErrorDeviceStartM = 121500,
                        ErrorDeviceStartT = 3500,
                        DeviceStartT = 0,
                        TimerStartZR = 3000,
                        ProsTimeStartZR = 14000,
                        ProsTimePreviousStartZR = 26000,
                        CyTimeStartZR = 32000
                    }
                };

                foreach (var profile in defaultProfiles)
                {
                    _repository.AddMemoryProfileAsync(profile).Wait();
                }
            }
        }

    }
}
