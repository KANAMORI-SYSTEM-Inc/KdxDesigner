using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Services;
using KdxDesigner.Utils;
using System.Collections.ObjectModel;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class MemoryProfileViewModel : ObservableObject
    {
        private readonly MemoryProfileManager _profileManager;
        private readonly MainViewModel _mainViewModel;
        private readonly ISupabaseRepository _repository;

        [ObservableProperty] private ObservableCollection<KdxDesigner.Models.MemoryProfile> profiles = new();
        [ObservableProperty] private KdxDesigner.Models.MemoryProfile? selectedProfile;
        [ObservableProperty] private string newProfileName = string.Empty;
        [ObservableProperty] private string newProfileDescription = string.Empty;
        [ObservableProperty] private string? memoryRecordSummary;
        [ObservableProperty] private List<DeviceOverlapInfo>? deviceOverlaps;

        public MemoryProfileViewModel(MainViewModel mainViewModel, ISupabaseRepository repository)
        {
            _mainViewModel = mainViewModel;
            _profileManager = new MemoryProfileManager();
            _repository = repository;
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            var profileList = _profileManager.LoadProfiles();
            Profiles = new ObservableCollection<KdxDesigner.Models.MemoryProfile>(profileList);
        }

        [RelayCommand]
        private void LoadProfile()
        {
            if (SelectedProfile == null)
            {
                MessageBox.Show("プロファイルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"プロファイル「{SelectedProfile.Name}」を読み込みますか？\n現在の設定は上書きされます。",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _profileManager.ApplyProfileToViewModel(SelectedProfile, _mainViewModel);
                SettingsManager.Settings.LastUsedMemoryProfileId = SelectedProfile.Id;
                SettingsManager.Save();
                MessageBox.Show("プロファイルを読み込みました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void SaveCurrentAsProfile()
        {
            if (string.IsNullOrWhiteSpace(NewProfileName))
            {
                MessageBox.Show("プロファイル名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 同名のプロファイルが存在するかチェック
            if (Profiles.Any(p => p.Name == NewProfileName && !p.IsDefault))
            {
                var result = MessageBox.Show(
                    $"同名のプロファイル「{NewProfileName}」が既に存在します。上書きしますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var newProfile = _profileManager.CreateProfileFromCurrent(_mainViewModel, NewProfileName, NewProfileDescription);
            _profileManager.SaveProfile(newProfile);

            LoadProfiles();
            NewProfileName = string.Empty;
            NewProfileDescription = string.Empty;

            MessageBox.Show("プロファイルを保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void UpdateProfile()
        {
            if (SelectedProfile == null)
            {
                MessageBox.Show("更新するプロファイルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedProfile.IsDefault)
            {
                MessageBox.Show("デフォルトプロファイルは更新できません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"プロファイル「{SelectedProfile.Name}」を現在の設定で更新しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // TextBoxにバインドされているSelectedProfileの値がすでに更新されているため、
                // そのまま保存するだけで良い
                SelectedProfile.UpdatedAt = DateTime.Now;
                _profileManager.SaveProfile(SelectedProfile);
                LoadProfiles();

                MessageBox.Show("プロファイルを更新しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void DeleteProfile()
        {
            if (SelectedProfile == null)
            {
                MessageBox.Show("削除するプロファイルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedProfile.IsDefault)
            {
                MessageBox.Show("デフォルトプロファイルは削除できません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"プロファイル「{SelectedProfile.Name}」を削除しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _profileManager.DeleteProfile(SelectedProfile.Id);
                LoadProfiles();
                MessageBox.Show("プロファイルを削除しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        partial void OnSelectedProfileChanged(KdxDesigner.Models.MemoryProfile? value)
        {
            if (value != null)
            {
                // 選択されたプロファイルの詳細を表示するなど
                CalculateMemoryUsage(value);
            }
        }

        private async void CalculateMemoryUsage(KdxDesigner.Models.MemoryProfile profile)
        {
            if (_mainViewModel.SelectedPlc == null) return;

            // 各タイプのレコード数を取得
            var processes = await _repository.GetProcessesAsync();
            var processDetails = await _repository.GetProcessDetailsAsync();
            var operations = await _repository.GetOperationsAsync();
            var cylinders = await _repository.GetCYsAsync();

            // 各カテゴリのMemoryレコード数を計算
            int processMemoryCount = processes.Count * 5; // 各プロセスは5レコード
            int processDetailMemoryCount = processDetails.Count * 5; // 各詳細は5レコード
            int operationMemoryCount = operations.Count * 20; // 各操作は20レコード
            int cylinderMemoryCount = cylinders.Count * 50; // 各シリンダーは50レコード

            // 合計レコード数
            int totalMemoryCount = processMemoryCount + processDetailMemoryCount +
                                 operationMemoryCount + cylinderMemoryCount;

            // 重複チェック
            var overlaps = CheckDeviceOverlaps(profile, processes.Count, processDetails.Count,
                                              operations.Count, cylinders.Count);

            // サマリー文字列の生成
            MemoryRecordSummary = $"Memoryテーブル予想レコード数:\n" +
                                $"  工程: {processMemoryCount}件 ({processes.Count} × 5)\n" +
                                $"  工程詳細: {processDetailMemoryCount}件 ({processDetails.Count} × 5)\n" +
                                $"  操作: {operationMemoryCount}件 ({operations.Count} × 20)\n" +
                                $"  出力: {cylinderMemoryCount}件 ({cylinders.Count} × 50)\n" +
                                $"  合計: {totalMemoryCount}件";

            DeviceOverlaps = overlaps;
        }

        private List<DeviceOverlapInfo> CheckDeviceOverlaps(KdxDesigner.Models.MemoryProfile profile,
            int processCount, int processDetailCount, int operationCount, int cylinderCount)
        {
            var overlaps = new List<DeviceOverlapInfo>();

            // 各カテゴリの終了アドレスを計算
            int processEndL = profile.ProcessDeviceStartL + (processCount * 5) - 1;
            int detailEndL = profile.DetailDeviceStartL + (processDetailCount * 5) - 1;
            int operationEndM = profile.OperationDeviceStartM + (operationCount * 20) - 1;
            int cylinderEndM = profile.CylinderDeviceStartM + (cylinderCount * 50) - 1;
            int cylinderEndD = profile.CylinderDeviceStartD + (cylinderCount * 50) - 1;

            // Lデバイスの重複チェック
            if (processEndL >= profile.DetailDeviceStartL)
            {
                overlaps.Add(new DeviceOverlapInfo
                {
                    DeviceType = "L",
                    Category1 = "工程",
                    Category2 = "工程詳細",
                    Range1 = $"L{profile.ProcessDeviceStartL} - L{processEndL}",
                    Range2 = $"L{profile.DetailDeviceStartL} - L{detailEndL}",
                    Message = "工程と工程詳細のLデバイスが重複しています"
                });
            }

            // Mデバイスの重複チェック
            if (operationEndM >= profile.CylinderDeviceStartM)
            {
                overlaps.Add(new DeviceOverlapInfo
                {
                    DeviceType = "M",
                    Category1 = "操作",
                    Category2 = "出力",
                    Range1 = $"M{profile.OperationDeviceStartM} - M{operationEndM}",
                    Range2 = $"M{profile.CylinderDeviceStartM} - M{cylinderEndM}",
                    Message = "操作と出力のMデバイスが重複しています"
                });
            }

            // エラーデバイスとの重複チェック
            if (cylinderEndM >= profile.ErrorDeviceStartM)
            {
                overlaps.Add(new DeviceOverlapInfo
                {
                    DeviceType = "M",
                    Category1 = "出力",
                    Category2 = "エラー",
                    Range1 = $"M{profile.CylinderDeviceStartM} - M{cylinderEndM}",
                    Range2 = $"M{profile.ErrorDeviceStartM} - ",
                    Message = "出力とエラーのMデバイスが重複しています"
                });
            }

            return overlaps;
        }
    }

    public class DeviceOverlapInfo
    {
        public string DeviceType { get; set; } = string.Empty;
        public string Category1 { get; set; } = string.Empty;
        public string Category2 { get; set; } = string.Empty;
        public string Range1 { get; set; } = string.Empty;
        public string Range2 { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
