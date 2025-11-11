using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using KdxDesigner.Models;

namespace KdxDesigner.Services
{
    /// <summary>
    /// Cycle用メモリプロファイルの管理
    /// ProcessDetail/Operationデバイスなど、Cycleごとに複数回適用可能な設定を管理
    /// </summary>
    public class CycleMemoryProfileManager
    {
        private readonly string _profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MemoryProfiles");
        private readonly string _profilesFile = "cycle_memory_profiles.json";
        private readonly string _profilesFilePath;

        public CycleMemoryProfileManager()
        {
            _profilesFilePath = Path.Combine(_profilesDirectory, _profilesFile);
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_profilesDirectory))
            {
                Directory.CreateDirectory(_profilesDirectory);
            }
        }

        public List<CycleMemoryProfile> LoadProfiles()
        {
            if (!File.Exists(_profilesFilePath))
            {
                return new List<CycleMemoryProfile>();
            }

            try
            {
                var json = File.ReadAllText(_profilesFilePath);
                return JsonSerializer.Deserialize<List<CycleMemoryProfile>>(json) ?? new List<CycleMemoryProfile>();
            }
            catch
            {
                return new List<CycleMemoryProfile>();
            }
        }

        public void SaveProfile(CycleMemoryProfile profile)
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
            profiles.RemoveAll(p => p.Id == profileId);
            SaveAllProfiles(profiles);
        }

        public CycleMemoryProfile? GetProfile(string profileId)
        {
            var profiles = LoadProfiles();
            return profiles.FirstOrDefault(p => p.Id == profileId);
        }

        public CycleMemoryProfile? GetDefaultProfile()
        {
            var profiles = LoadProfiles();
            return profiles.FirstOrDefault(p => p.IsDefault);
        }

        private void SaveAllProfiles(List<CycleMemoryProfile> profiles)
        {
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_profilesFilePath, json);
        }
    }
}
