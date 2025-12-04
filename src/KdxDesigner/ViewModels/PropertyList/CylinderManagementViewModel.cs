using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// シリンダー表示用のラッパークラス
    /// </summary>
    public class CylinderDisplayModel
    {
        public Cylinder Cylinder { get; set; } = new();
        public string MachineName { get; set; } = "";
        public string DriveSubName { get; set; } = "";
    }

    /// <summary>
    /// シリンダー管理ウィンドウのViewModel
    /// </summary>
    public partial class CylinderManagementViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private readonly int _plcId;
        private Dictionary<int, string> _machineNameMap = new();
        private Dictionary<int, string> _driveSubMap = new();

        [ObservableProperty]
        private ObservableCollection<CylinderDisplayModel> _cylinders = new();

        [ObservableProperty]
        private CylinderDisplayModel? _selectedCylinder;

        public CylinderManagementViewModel(ISupabaseRepository repository, int plcId)
        {
            _repository = repository;
            _plcId = plcId;

            // シリンダーのリストを読み込み
            LoadCylinders();
        }

        /// <summary>
        /// シリンダーのリストを読み込み
        /// </summary>
        private async void LoadCylinders()
        {
            try
            {
                // マスターデータを読み込み
                var machineNames = await _repository.GetMachineNamesAsync();
                _machineNameMap = machineNames.ToDictionary(m => m.Id, m => m.FullName);

                var driveSubs = await _repository.GetDriveSubsAsync();
                _driveSubMap = driveSubs.ToDictionary(d => d.Id, d => d.DriveSubName ?? "");

                // シリンダーを読み込み
                var cylinders = await _repository.GetCYsAsync();
                var filteredCylinders = cylinders
                    .Where(c => c.PlcId == _plcId)
                    .OrderBy(c => c.SortNumber)
                    .Select(c => new CylinderDisplayModel
                    {
                        Cylinder = c,
                        MachineName = c.MachineNameId.HasValue && _machineNameMap.ContainsKey(c.MachineNameId.Value)
                            ? _machineNameMap[c.MachineNameId.Value]
                            : "",
                        DriveSubName = c.DriveSubId.HasValue && _driveSubMap.ContainsKey(c.DriveSubId.Value)
                            ? _driveSubMap[c.DriveSubId.Value]
                            : ""
                    })
                    .ToList();

                Cylinders = new ObservableCollection<CylinderDisplayModel>(filteredCylinders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シリンダーの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// シリンダー追加コマンド
        /// </summary>
        [RelayCommand]
        private async Task AddCylinder()
        {
            try
            {
                // 既存のシリンダー数を取得してSortNumberを設定
                int nextSortNumber = Cylinders.Count > 0 ? Cylinders.Max(c => c.Cylinder.SortNumber ?? 0) + 1 : 1;

                // 新しいCylinderオブジェクトを作成
                var newCylinder = new Cylinder
                {
                    PlcId = _plcId,
                    CYNum = "新規シリンダ",
                    PUCO = "PU",
                    SortNumber = nextSortNumber
                };

                // データベースに追加
                int newId = await _repository.AddCylinderAsync(newCylinder);
                newCylinder.Id = newId;

                // リストを再読み込み
                LoadCylinders();

                MessageBox.Show($"新しいシリンダーを追加しました。(ID: {newId})", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シリンダーの追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// シリンダーコピーコマンド
        /// </summary>
        [RelayCommand]
        private async Task CopyCylinder()
        {
            if (SelectedCylinder == null)
            {
                MessageBox.Show("コピーするシリンダーを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 既存のシリンダー数を取得してSortNumberを設定
                int nextSortNumber = Cylinders.Count > 0 ? Cylinders.Max(c => c.Cylinder.SortNumber ?? 0) + 1 : 1;

                // 選択されたシリンダーをコピー
                var copiedCylinder = new Cylinder
                {
                    PlcId = _plcId,
                    CYNum = SelectedCylinder.Cylinder.CYNum + " (コピー)",
                    PUCO = SelectedCylinder.Cylinder.PUCO,
                    Go = SelectedCylinder.Cylinder.Go,
                    Back = SelectedCylinder.Cylinder.Back,
                    OilNum = SelectedCylinder.Cylinder.OilNum,
                    MachineNameId = SelectedCylinder.Cylinder.MachineNameId,
                    DriveSubId = SelectedCylinder.Cylinder.DriveSubId,
                    PlaceId = SelectedCylinder.Cylinder.PlaceId,
                    CYNameSub = SelectedCylinder.Cylinder.CYNameSub,
                    SensorId = SelectedCylinder.Cylinder.SensorId,
                    FlowType = SelectedCylinder.Cylinder.FlowType,
                    GoSensorCount = SelectedCylinder.Cylinder.GoSensorCount,
                    BackSensorCount = SelectedCylinder.Cylinder.BackSensorCount,
                    RetentionSensorGo = SelectedCylinder.Cylinder.RetentionSensorGo,
                    RetentionSensorBack = SelectedCylinder.Cylinder.RetentionSensorBack,
                    SortNumber = nextSortNumber,
                    FlowCount = SelectedCylinder.Cylinder.FlowCount,
                    FlowCYGo = SelectedCylinder.Cylinder.FlowCYGo,
                    FlowCYBack = SelectedCylinder.Cylinder.FlowCYBack
                };

                // データベースに追加
                int newId = await _repository.AddCylinderAsync(copiedCylinder);
                copiedCylinder.Id = newId;

                // リストを再読み込み
                LoadCylinders();

                MessageBox.Show($"シリンダーをコピーして追加しました。(ID: {newId})", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シリンダーのコピー中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// シリンダー編集コマンド
        /// </summary>
        [RelayCommand]
        private void EditCylinder()
        {
            if (SelectedCylinder == null)
            {
                MessageBox.Show("編集するシリンダーを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var window = new CylinderPropertiesWindow(_repository, SelectedCylinder.Cylinder);
                var mainWindow = Application.Current.Windows.OfType<MainView>().FirstOrDefault();
                if (mainWindow != null)
                {
                    window.Owner = mainWindow;
                }

                if (window.ShowDialog() == true)
                {
                    // シリンダーリストを再読み込み
                    LoadCylinders();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シリンダーの編集中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// シリンダー削除コマンド
        /// </summary>
        [RelayCommand]
        private async Task DeleteCylinder()
        {
            if (SelectedCylinder == null)
            {
                MessageBox.Show("削除するシリンダーを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"シリンダー「{SelectedCylinder.Cylinder.CYNum}」を削除しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository.DeleteCylinderAsync(SelectedCylinder.Cylinder.Id);
                    Cylinders.Remove(SelectedCylinder);
                    MessageBox.Show("シリンダーを削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"シリンダーの削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 更新コマンド
        /// </summary>
        [RelayCommand]
        private void Refresh()
        {
            LoadCylinders();
        }
    }
}
