using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KdxDesigner.Models;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Linq;
using System.Windows;

namespace KdxDesigner.ViewModels.Settings
{
    /// <summary>
    /// メモリ設定ウィンドウのViewModel
    /// プロファイル管理とメモリ設定実行機能を提供
    /// </summary>
    public partial class MemorySettingViewModel : ObservableObject
    {
        /// <summary>
        /// 新規PLC用プロファイル作成
        /// </summary>
        [RelayCommand]
        private void CreateNewPlcProfile()
        {
            // プロファイル入力ダイアログを表示
            var dialog = new ProfileInputDialog
            {
                DialogTitle = "PLC用プロファイル作成",
                Owner = Application.Current.Windows.OfType<MemorySettingWindow>().FirstOrDefault()
            };

            if (dialog.ShowDialog() == true)
            {
                // 現在の設定値を使用して新しいプロファイルを作成
                var newProfile = new PlcMemoryProfile
                {
                    Name = dialog.ProfileName,
                    Description = dialog.ProfileDescription,
                    PlcId = SelectedPlc?.Id ?? 2,
                    CylinderDeviceStartM = CylinderDeviceStartM,
                    CylinderDeviceStartD = CylinderDeviceStartD,
                    ErrorDeviceStartM = ErrorDeviceStartM,
                    ErrorDeviceStartT = ErrorDeviceStartT,
                    DeviceStartT = DeviceStartT,
                    TimerStartZR = TimerStartZR,
                    ProsTimeStartZR = ProsTimeStartZR,
                    ProsTimePreviousStartZR = ProsTimePreviousStartZR,
                    CyTimeStartZR = CyTimeStartZR,
                    IsDefault = false
                };

                // プロファイルを保存
                _plcProfileManager.SaveProfile(newProfile);

                // プロファイル一覧を再読み込み
                LoadPlcProfiles();

                // 作成したプロファイルを選択
                SelectedPlcProfile = PlcProfiles.FirstOrDefault(p => p.Id == newProfile.Id);

                MessageBox.Show($"PLC用プロファイル '{dialog.ProfileName}' を作成しました。", "作成完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 新規Cycle用プロファイル作成
        /// </summary>
        [RelayCommand]
        private void CreateNewCycleProfile()
        {
            // PLCが選択されているか確認
            if (SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // プロファイル入力ダイアログを表示（Cycle選択対応、デバイス設定も渡す）
            var dialog = new ProfileInputDialog(
                _repository,
                SelectedPlc.Id,
                existingCycleId: 0,
                processDeviceStartL: ProcessDeviceStartL,
                detailDeviceStartL: DetailDeviceStartL,
                operationDeviceStartM: OperationDeviceStartM)
            {
                DialogTitle = "Cycle用プロファイル作成",
                Owner = Application.Current.Windows.OfType<MemorySettingWindow>().FirstOrDefault()
            };

            if (dialog.ShowDialog() == true)
            {
                // ダイアログで入力された設定値を使用して新しいプロファイルを作成
                var newProfile = new CycleMemoryProfile
                {
                    Name = dialog.ProfileName,
                    Description = dialog.ProfileDescription,
                    PlcId = SelectedPlc.Id,
                    CycleId = dialog.ProfileCycleIdValue, // Cycle選択から取得
                    ProcessDeviceStartL = dialog.ProcessDeviceStartL, // ダイアログから取得
                    DetailDeviceStartL = dialog.DetailDeviceStartL, // ダイアログから取得
                    OperationDeviceStartM = dialog.OperationDeviceStartM, // ダイアログから取得
                    IsDefault = false
                };

                // プロファイルを保存
                _cycleProfileManager.SaveProfile(newProfile);

                // プロファイル一覧を再読み込み
                LoadCycleProfiles();

                // 作成したプロファイルを選択（複数選択なのでSelectedCycleProfilesに追加）
                var createdProfile = CycleProfiles.FirstOrDefault(p => p.Id == newProfile.Id);
                if (createdProfile != null)
                {
                    SelectedCycleProfiles.Add(createdProfile);
                }

                MessageBox.Show($"Cycle用プロファイル '{dialog.ProfileName}' を作成しました。", "作成完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// メモリ設定を実行（PLC/Cycle プロファイル対応版）
        /// </summary>
        [RelayCommand]
        private async Task ExecuteMemorySetting()
        {
            // 新プロファイルシステムの検証
            if (!ValidateNewProfileSystem())
            {
                return;
            }

            // MainViewModelに設定を反映
            ApplySettingsToMainViewModel();

            // メモリ設定状態を「設定中」に更新
            if (_memoryConfig == null) return;
            _memoryConfig.MemoryConfigurationStatus = "設定中...";
            _memoryConfig.IsMemoryConfigured = false;

            // 進捗ウィンドウを作成
            var progressViewModel = new MemoryProgressViewModel();
            var progressWindow = new Views.MemoryProgressWindow
            {
                DataContext = progressViewModel,
                Owner = Application.Current.Windows.OfType<MemorySettingWindow>().FirstOrDefault()
            };

            // 進捗ウィンドウを非モーダルで表示
            progressWindow.Show();

            // UIスレッドをブロックしないようにTask.Runで実行
            await Task.Run(async () =>
            {
                try
                {
                    progressViewModel.UpdateStatus("メモリ設定を開始しています...");
                    await Task.Delay(100); // UIの更新を待つ

                    // 既存のMemoryテーブルデータを削除（PLCID単位で一括削除）
                    if (SelectedPlc != null)
                    {
                        progressViewModel.UpdateStatus($"既存のメモリデータを削除中... (PlcId={SelectedPlc.Id})");
                        await _repository.DeleteMemoriesByPlcIdAsync(SelectedPlc.Id);
                        progressViewModel.AddLog($"既存のメモリデータを削除しました (PlcId={SelectedPlc.Id})");
                    }

                    // 新プロファイルシステム対応の実装
                    // 1. PLC用プロファイルの設定を適用（1回のみ）
                    if (SelectedPlcProfile != null)
                    {
                        progressViewModel.UpdateStatus("PLC用プロファイルを適用中...");
                        ApplyPlcProfile(SelectedPlcProfile);
                    }

                    // 2. Cycle用プロファイルをループして各Cycleの設定を適用
                    int totalTimerCount = 0;
                    int totalErrorCount = 0;
                    if (SelectedCycleProfiles != null && SelectedCycleProfiles.Any())
                    {
                        int cycleIndex = 0;
                        foreach (var cycleProfile in SelectedCycleProfiles)
                        {
                            cycleIndex++;
                            bool isFirstCycle = (cycleIndex == 1);
                            progressViewModel.UpdateStatus($"Cycle用プロファイル '{cycleProfile.Name}' を適用中... ({cycleIndex}/{SelectedCycleProfiles.Count})");

                            // Cycleプロファイルの設定を適用
                            ApplyCycleProfile(cycleProfile);

                            // Cycleごとのデバイスを保存し、タイマーカウントとエラーカウントを累積
                            (totalTimerCount, totalErrorCount) = await SaveCycleDevices(cycleProfile, progressViewModel, isFirstCycle, totalTimerCount, totalErrorCount);
                        }
                    }

                    // 3. PLC全体のデバイスを保存（Cylinder, Error, Timer, ProsTime, Speed）（1回のみ）
                    // Cycle処理で使用されたタイマー数を引数として渡す
                    progressViewModel.UpdateStatus($"PLC全体のデバイスを保存中... (Cycleタイマー数: {totalTimerCount})");
                    await SavePlcDevices(progressViewModel, totalTimerCount);

                    // メモリ設定状態を更新
                    await UpdateMemoryConfigurationStatus();

                    progressViewModel.MarkCompleted();

                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show("すべてのメモリ設定が完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information));
                }
                catch (Exception ex)
                {
                    progressViewModel.MarkError(ex.Message);
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"メモリ設定中にエラーが発生しました: {ex.Message}", "エラー"));
                }
            });
        }

        [RelayCommand]
        private void ShowMemoryDeviceList()
        {
            try
            {
                // メモリストアを取得
                var memoryStore = App.Services?.GetService<IMnemonicDeviceMemoryStore>()
                    ?? new MnemonicDeviceMemoryStore();

                // 現在選択中のPLCとCycleを渡してウィンドウを開く
                var window = new MemoryDeviceListWindow(
                    memoryStore,
                    SelectedPlc?.Id,
                    SelectedCycle?.Id);

                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"メモリデバイス一覧の表示に失敗しました。\n{ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// PLC用プロファイルを編集
        /// </summary>
        [RelayCommand]
        private void EditPlcProfile()
        {
            if (SelectedPlcProfile == null)
            {
                MessageBox.Show("編集するプロファイルを選択してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // プロファイル入力ダイアログを表示（既存の値を設定）
            var dialog = new ProfileInputDialog
            {
                DialogTitle = "PLC用プロファイル編集",
                ProfileName = SelectedPlcProfile.Name,
                ProfileDescription = SelectedPlcProfile.Description,
                Owner = Application.Current.Windows.OfType<MemorySettingWindow>().FirstOrDefault()
            };

            if (dialog.ShowDialog() == true)
            {
                // プロファイルを更新
                SelectedPlcProfile.Name = dialog.ProfileName;
                SelectedPlcProfile.Description = dialog.ProfileDescription;
                SelectedPlcProfile.CylinderDeviceStartM = CylinderDeviceStartM;
                SelectedPlcProfile.CylinderDeviceStartD = CylinderDeviceStartD;
                SelectedPlcProfile.ErrorDeviceStartM = ErrorDeviceStartM;
                SelectedPlcProfile.ErrorDeviceStartT = ErrorDeviceStartT;
                SelectedPlcProfile.DeviceStartT = DeviceStartT;
                SelectedPlcProfile.TimerStartZR = TimerStartZR;
                SelectedPlcProfile.ProsTimeStartZR = ProsTimeStartZR;
                SelectedPlcProfile.ProsTimePreviousStartZR = ProsTimePreviousStartZR;
                SelectedPlcProfile.CyTimeStartZR = CyTimeStartZR;

                // プロファイルを保存
                _plcProfileManager.SaveProfile(SelectedPlcProfile);

                // プロファイル一覧を再読み込み
                LoadPlcProfiles();

                MessageBox.Show($"PLC用プロファイル '{dialog.ProfileName}' を更新しました。", "更新完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Cycle用プロファイルを編集
        /// </summary>
        [RelayCommand]
        private void EditCycleProfile()
        {
            // 複数選択の場合、最初の選択項目を編集対象とする
            var profileToEdit = SelectedCycleProfiles?.FirstOrDefault();
            if (profileToEdit == null)
            {
                MessageBox.Show("編集するプロファイルを選択してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // PLCが選択されているか確認
            if (SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // プロファイル入力ダイアログを表示（既存の値を設定、Cycle選択対応、デバイス設定も含む）
            var dialog = new ProfileInputDialog(
                _repository,
                SelectedPlc.Id,
                existingCycleId: profileToEdit.CycleId,
                processDeviceStartL: profileToEdit.ProcessDeviceStartL,
                detailDeviceStartL: profileToEdit.DetailDeviceStartL,
                operationDeviceStartM: profileToEdit.OperationDeviceStartM)
            {
                DialogTitle = "Cycle用プロファイル編集",
                ProfileName = profileToEdit.Name,
                ProfileDescription = profileToEdit.Description,
                Owner = Application.Current.Windows.OfType<MemorySettingWindow>().FirstOrDefault()
            };

            if (dialog.ShowDialog() == true)
            {
                // プロファイルを更新（ダイアログで入力された値を使用）
                profileToEdit.Name = dialog.ProfileName;
                profileToEdit.Description = dialog.ProfileDescription;
                profileToEdit.CycleId = dialog.ProfileCycleIdValue; // Cycle選択から取得
                profileToEdit.ProcessDeviceStartL = dialog.ProcessDeviceStartL; // ダイアログから取得
                profileToEdit.DetailDeviceStartL = dialog.DetailDeviceStartL; // ダイアログから取得
                profileToEdit.OperationDeviceStartM = dialog.OperationDeviceStartM; // ダイアログから取得

                // プロファイルを保存
                _cycleProfileManager.SaveProfile(profileToEdit);

                // プロファイル一覧を再読み込み
                LoadCycleProfiles();

                MessageBox.Show($"Cycle用プロファイル '{dialog.ProfileName}' を更新しました。", "更新完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// PLC用プロファイルを削除
        /// </summary>
        [RelayCommand]
        private void DeletePlcProfile()
        {
            if (SelectedPlcProfile == null)
            {
                MessageBox.Show("削除するプロファイルを選択してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 確認ダイアログ
            var result = MessageBox.Show(
                $"プロファイル '{SelectedPlcProfile.Name}' を削除しますか？\nこの操作は取り消せません。",
                "削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var profileName = SelectedPlcProfile.Name;
                var deletedProfileId = SelectedPlcProfile.Id;

                _plcProfileManager.DeleteProfile(deletedProfileId);

                // プロファイル一覧を再読み込み
                LoadPlcProfiles();

                // 削除後、残っているプロファイルがあればデフォルトを選択、なければ最初のプロファイルを選択
                if (PlcProfiles.Any())
                {
                    SelectedPlcProfile = PlcProfiles.FirstOrDefault(p => p.IsDefault)
                                        ?? PlcProfiles.FirstOrDefault();
                }

                MessageBox.Show($"PLC用プロファイル '{profileName}' を削除しました。", "削除完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Cycle用プロファイルを削除
        /// </summary>
        [RelayCommand]
        private void DeleteCycleProfile()
        {
            // 複数選択の場合、最初の選択項目を削除対象とする
            var profileToDelete = SelectedCycleProfiles?.FirstOrDefault();
            if (profileToDelete == null)
            {
                MessageBox.Show("削除するプロファイルを選択してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 確認ダイアログ
            var result = MessageBox.Show(
                $"プロファイル '{profileToDelete.Name}' を削除しますか？\nこの操作は取り消せません。",
                "削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var profileName = profileToDelete.Name;
                var deletedProfileId = profileToDelete.Id;

                _cycleProfileManager.DeleteProfile(deletedProfileId);

                // プロファイル一覧を再読み込み（自動的にデフォルトプロファイルが選択される）
                LoadCycleProfiles();

                MessageBox.Show($"Cycle用プロファイル '{profileName}' を削除しました。", "削除完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

    }
}
