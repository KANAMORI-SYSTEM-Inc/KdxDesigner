using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using KdxDesigner.Models;
using KdxDesigner.ViewModels;

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

        public MemoryProfileManager()
        {
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
            var profiles = LoadProfiles();
            if (!profiles.Any())
            {
                // デフォルトプロファイルを作成
                var defaultProfiles = new List<MemoryProfile>
                {
                    new MemoryProfile
                    {
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
                    }
                };

                foreach (var profile in defaultProfiles)
                {
                    SaveProfile(profile);
                }
            }
        }

        public List<MemoryProfile> LoadProfiles()
        {
            if (!File.Exists(_profilesFilePath))
            {
                return new List<MemoryProfile>();
            }

            try
            {
                var json = File.ReadAllText(_profilesFilePath);
                return JsonSerializer.Deserialize<List<MemoryProfile>>(json) ?? new List<MemoryProfile>();
            }
            catch
            {
                return new List<MemoryProfile>();
            }
        }

        public void SaveProfile(MemoryProfile profile)
        {
            var profiles = LoadProfiles();
            
            // 既存のプロファイルを更新または新規追加
            var existingProfile = profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existingProfile != null)
            {
                profile.UpdatedAt = DateTime.Now;
                profiles.Remove(existingProfile);
            }
            
            profiles.Add(profile);
            SaveAllProfiles(profiles);
        }

        public void DeleteProfile(string profileId)
        {
            var profiles = LoadProfiles();
            profiles.RemoveAll(p => p.Id == profileId && !p.IsDefault);
            SaveAllProfiles(profiles);
        }

        public MemoryProfile? GetProfile(string profileId)
        {
            var profiles = LoadProfiles();
            return profiles.FirstOrDefault(p => p.Id == profileId);
        }

        public MemoryProfile? GetDefaultProfile()
        {
            var profiles = LoadProfiles();
            return profiles.FirstOrDefault(p => p.IsDefault);
        }

        private void SaveAllProfiles(List<MemoryProfile> profiles)
        {
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_profilesFilePath, json);
        }

        public MemoryProfile CreateProfileFromCurrent(MainViewModel mainViewModel, string name, string description)
        {
            return new MemoryProfile
            {
                Name = name,
                Description = description,
                ProcessDeviceStartL = mainViewModel.ProcessDeviceStartL,
                DetailDeviceStartL = mainViewModel.DetailDeviceStartL,
                OperationDeviceStartM = mainViewModel.OperationDeviceStartM,
                CylinderDeviceStartM = mainViewModel.CylinderDeviceStartM,
                CylinderDeviceStartD = mainViewModel.CylinderDeviceStartD,
                ErrorDeviceStartM = mainViewModel.ErrorDeviceStartM,
                ErrorDeviceStartT = mainViewModel.ErrorDeviceStartT,
                DeviceStartT = mainViewModel.DeviceStartT,
                TimerStartZR = mainViewModel.TimerStartZR,
                ProsTimeStartZR = mainViewModel.ProsTimeStartZR,
                ProsTimePreviousStartZR = mainViewModel.ProsTimePreviousStartZR,
                CyTimeStartZR = mainViewModel.CyTimeStartZR
            };
        }

        public MemoryProfile UpdateProfileFromWindow(MemoryProfile memoryProfile, string name, string description)
        {
            return new MemoryProfile
            {
                Name = name,
                Description = description,
                ProcessDeviceStartL = memoryProfile.ProcessDeviceStartL,
                DetailDeviceStartL = memoryProfile.DetailDeviceStartL,
                OperationDeviceStartM = memoryProfile.OperationDeviceStartM,
                CylinderDeviceStartM = memoryProfile.CylinderDeviceStartM,
                CylinderDeviceStartD = memoryProfile.CylinderDeviceStartD,
                ErrorDeviceStartM = memoryProfile.ErrorDeviceStartM,
                ErrorDeviceStartT = memoryProfile.ErrorDeviceStartT,
                DeviceStartT = memoryProfile.DeviceStartT,
                TimerStartZR = memoryProfile.TimerStartZR,
                ProsTimeStartZR = memoryProfile.ProsTimeStartZR,
                ProsTimePreviousStartZR = memoryProfile.ProsTimePreviousStartZR,
                CyTimeStartZR = memoryProfile.CyTimeStartZR
            };
        }

        public void ApplyProfileToViewModel(MemoryProfile profile, MainViewModel mainViewModel)
        {
            mainViewModel.ProcessDeviceStartL = profile.ProcessDeviceStartL;
            mainViewModel.DetailDeviceStartL = profile.DetailDeviceStartL;
            mainViewModel.OperationDeviceStartM = profile.OperationDeviceStartM;
            mainViewModel.CylinderDeviceStartM = profile.CylinderDeviceStartM;
            mainViewModel.CylinderDeviceStartD = profile.CylinderDeviceStartD;
            mainViewModel.ErrorDeviceStartM = profile.ErrorDeviceStartM;
            mainViewModel.ErrorDeviceStartT = profile.ErrorDeviceStartT;
            mainViewModel.DeviceStartT = profile.DeviceStartT;
            mainViewModel.TimerStartZR = profile.TimerStartZR;
            mainViewModel.ProsTimeStartZR = profile.ProsTimeStartZR;
            mainViewModel.ProsTimePreviousStartZR = profile.ProsTimePreviousStartZR;
            mainViewModel.CyTimeStartZR = profile.CyTimeStartZR;
        }
    }
}
