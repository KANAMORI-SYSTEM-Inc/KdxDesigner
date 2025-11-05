using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace KdxDesigner.ViewModels.Settings
{
    // コンストラクタを async Task に分離
    public class ControlBoxViewModel : INotifyPropertyChanged
    {
        // --- 既存: CylinderViewModel はそのまま（省略可） ---
        public class CylinderViewModel
        {
            public Cylinder Cylinder { get; }
            public Machine? Machine { get; }
            public MachineName? MachineName { get; }
            public CylinderCycle? CylinderCycle { get; }
            public string DisplayMachineName =>
            MachineName?.FullName ?? "-";
            public CylinderViewModel(Cylinder c, Machine? m, MachineName? mn, CylinderCycle? cc)
            {
                Cylinder = c; Machine = m; MachineName = mn;
                CylinderCycle = cc;
            }
        }

        // --- 行アイテム（編集用） ---
        public class MappingItem : INotifyPropertyChanged
        {
            private int? _boxNumber;
            public int? BoxNumber        // ControlBox.BoxNumber を選択
            {
                get => _boxNumber; set { if (Set(ref _boxNumber, value)) { if (value.HasValue) ManualNumber = value.Value; } }
            }

            private int _manualNumber;
            public int ManualNumber      // DB上の ManualNumber（= BoxNumber をデフォルト反映）
            {
                get => _manualNumber; set => Set(ref _manualNumber, value);
            }

            private string? _device;
            public string? Device
            {
                get => _device; set => Set(ref _device, value);
            }

            // 内部識別用
            public int CylinderId { get; set; }
            public int PlcId { get; set; }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return true;
            }
        }

        private readonly ISupabaseRepository _repo;
        private readonly int _plcId;

        private readonly ObservableCollection<CylinderViewModel> _cylindersSource = new();
        public ICollectionView Cylinders { get; }

        public ObservableCollection<ControlBox> ControlBoxes { get; } = new();
        public ObservableCollection<MappingItem> Mappings { get; } = new();

        private CylinderViewModel? _selectedCylinder;
        public CylinderViewModel? SelectedCylinder
        {
            get => _selectedCylinder;
            set { if (Set(ref _selectedCylinder, value)) LoadMappingsForSelection(); }
        }

        public ICommand AddMappingCommand { get; }
        public ICommand RemoveMappingCommand { get; }
        public ICommand SaveAllMappingsCommand { get; }
        public ICommand DeleteAllMappingsCommand { get; }

        public ControlBoxViewModel(ISupabaseRepository repo, int plcId)
        {
            _repo = repo;
            _plcId = plcId;

            // ICollectionViewを作成（複数ソート対応）
            Cylinders = CollectionViewSource.GetDefaultView(_cylindersSource);
            var collectionView = Cylinders as ListCollectionView;
            if (collectionView != null)
            {
                // 複数列でのソートを有効化
                collectionView.CustomSort = null;
                // デフォルトのソートを設定（CycleId → SortNumber）
                collectionView.SortDescriptions.Add(new SortDescription("CylinderCycle.CycleId", ListSortDirection.Ascending));
                collectionView.SortDescriptions.Add(new SortDescription("Cylinder.SortNumber", ListSortDirection.Ascending));
            }

            AddMappingCommand = new RelayCommand(_ => AddMapping(), _ => SelectedCylinder != null);
            RemoveMappingCommand = new RelayCommand(m =>
            {
                if (m is MappingItem item) Mappings.Remove(item);
            }, _ => SelectedCylinder != null);

            SaveAllMappingsCommand = new RelayCommand(_ => SaveAllMappings(), _ => SelectedCylinder != null);
            DeleteAllMappingsCommand = new RelayCommand(_ => DeleteAllMappings(), _ => SelectedCylinder != null && Mappings.Count > 0);
        }

        public static async Task<ControlBoxViewModel> CreateAsync(ISupabaseRepository repo, int plcId)
        {
            var vm = new ControlBoxViewModel(repo, plcId);

            // 左ペイン: シリンダー一覧
            var cy = await repo.GetCyListAsync(plcId);
            var machines = await repo.GetMachinesAsync();
            var mnames = await repo.GetMachineNamesAsync();
            var cycles = await repo.GetCylinderCyclesByPlcIdAsync(plcId);   // ← 追加：このPLCの全Cycle

            // 安全に辞書化（重複キーがあっても落ちないよう GroupBy→First）
            var machineByKey = machines
                .GroupBy(m => (m.MachineNameId, m.DriveSubId))
                .ToDictionary(g => g.Key, g => g.First());

            var mnameById = mnames
                .GroupBy(n => n.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var cycleByKey = cycles
                .Where(x => x.PlcId == plcId)
                .GroupBy(x => (x.CylinderId, x.PlcId))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.CycleId).First()  // 代表選出ルール（必要に応じて変更）
                );

            foreach (var c in cy)
            {
                machineByKey.TryGetValue((c.MachineNameId ?? 0, c.DriveSubId ?? 0), out var m);
                mnameById.TryGetValue(c.MachineNameId ?? -1, out var mn);

                // ここで cc を取り出す
                cycleByKey.TryGetValue((c.Id, plcId), out var cc);

                vm._cylindersSource.Add(new CylinderViewModel(c, m, mn, cc));
            }

            // 右ペイン：ControlBox のコンボ
            var controlBoxes = await repo.GetControlBoxesByPlcIdAsync(plcId);
            foreach (var cb in controlBoxes.OrderBy(x => x.BoxNumber))
                vm.ControlBoxes.Add(cb);

            return vm;
        }

        private async Task LoadMappingsForSelection()
        {
            Mappings.Clear();
            if (SelectedCylinder == null) return;

            // 既存を全件取得（複数行）
            var links = await _repo.GetCylinderControlBoxesAsync(_plcId, SelectedCylinder.Cylinder.Id); // ← List<CylinderControlBox>
            foreach (var link in links.OrderBy(l => l.ManualNumber))
            {
                Mappings.Add(new MappingItem
                {
                    PlcId = link.PlcId,
                    CylinderId = link.CylinderId,
                    BoxNumber = link.ManualNumber,     // 既存値を BoxNumber にも反映
                    ManualNumber = link.ManualNumber,
                    Device = link.Device
                });
            }
        }

        private void AddMapping()
        {
            if (SelectedCylinder == null) return;
            Mappings.Add(new MappingItem
            {
                PlcId = _plcId,
                CylinderId = SelectedCylinder.Cylinder.Id,
                BoxNumber = null,
                ManualNumber = 0,
                Device = string.Empty
            });
        }

        private async Task SaveAllMappings()
        {
            if (SelectedCylinder == null) return;

            // 簡易検証: BoxNumber 重複
            var dup = Mappings
                .Where(m => m.BoxNumber.HasValue)
                .GroupBy(m => m.BoxNumber!.Value)
                .FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
            {
                MessageBox.Show($"ControlBox(BoxNumber={dup.Key}) が重複しています。行を修正してください。", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // DB反映（Upsert 全行、古い残骸は削除）
            var cylId = SelectedCylinder.Cylinder.Id;

            // 1) 現在DBにある全リンク
            var existing = await _repo.GetCylinderControlBoxesAsync(_plcId, cylId);

            // 2) 画面の最新状態を Upsert
            var toKeepKeys = new HashSet<(int plcId, int cylId, int manual)>();
            foreach (var m in Mappings)
            {
                if (!m.BoxNumber.HasValue)
                {
                    MessageBox.Show("未選択の ControlBox 行があります。", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var link = new CylinderControlBox
                {
                    PlcId = _plcId,
                    CylinderId = cylId,
                    ManualNumber = m.ManualNumber, // = BoxNumber で基本OK（手入力上書きも可）
                    Device = m.Device ?? string.Empty
                };
                await _repo.UpsertCylinderControlBoxAsync(link);
                toKeepKeys.Add((link.PlcId, link.CylinderId, link.ManualNumber));
            }

            // 3) 画面に無い行は削除
            foreach (var row in existing)
            {
                var key = (row.PlcId, row.CylinderId, row.ManualNumber);
                if (!toKeepKeys.Contains(key))
                    await _repo.DeleteCylinderControlBoxAsync(row.PlcId, row.CylinderId, row.ManualNumber);
            }

            // 再読込して同期
            await LoadMappingsForSelection();
        }

        private async Task DeleteAllMappings()
        {
            if (SelectedCylinder == null) return;

            // 確認ダイアログ
            var result = MessageBox.Show(
                $"CY#{SelectedCylinder.Cylinder.CYNum} のControlBox設定をすべて削除しますか？",
                "削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var cylId = SelectedCylinder.Cylinder.Id;

            // 現在DBにある全リンクを削除
            var existing = await _repo.GetCylinderControlBoxesAsync(_plcId, cylId);
            foreach (var row in existing)
            {
                await _repo.DeleteCylinderControlBoxAsync(row.PlcId, row.CylinderId, row.ManualNumber);
            }

            // 画面をクリア
            Mappings.Clear();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }

    // 共通：ICommand
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _exec;
        private readonly Predicate<object?>? _can;
        public RelayCommand(Action<object?> exec, Predicate<object?>? can = null) { _exec = exec; _can = can; }
        public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _exec(parameter);
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    }
}
