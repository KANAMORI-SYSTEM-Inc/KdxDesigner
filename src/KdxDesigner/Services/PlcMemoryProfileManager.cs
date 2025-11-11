using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using KdxDesigner.Models;

namespace KdxDesigner.Services
{
    /// <summary>
    /// PLC用メモリプロファイルの管理
    /// Cylinderデバイスなど、PLC全体に対して1回のみ適用される設定を管理
    /// </summary>
    public class PlcMemoryProfileManager
    {
        private readonly string _profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MemoryProfiles");
        private readonly string _profilesFile = "plc_memory_profiles.json";
        private readonly string _profilesFilePath;

        public PlcMemoryProfileManager()
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

        public List<PlcMemoryProfile> LoadProfiles()
        {
            if (!File.Exists(_profilesFilePath))
            {
                return new List<PlcMemoryProfile>();
            }

            try
            {
                var json = File.ReadAllText(_profilesFilePath);
                return JsonSerializer.Deserialize<List<PlcMemoryProfile>>(json) ?? new List<PlcMemoryProfile>();
            }
            catch
            {
                return new List<PlcMemoryProfile>();
            }
        }

        public void SaveProfile(PlcMemoryProfile profile)
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

        public PlcMemoryProfile? GetProfile(string profileId)
        {
            var profiles = LoadProfiles();
            return profiles.FirstOrDefault(p => p.Id == profileId);
        }

        public PlcMemoryProfile? GetDefaultProfile()
        {
            var profiles = LoadProfiles();
            return profiles.FirstOrDefault(p => p.IsDefault);
        }

        private void SaveAllProfiles(List<PlcMemoryProfile> profiles)
        {
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_profilesFilePath, json);
        }
    }
}
