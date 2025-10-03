using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using Kdx.Infrastructure.Services;

using KdxDesigner.Services.LinkDevice;
using KdxDesigner.Utils;

using Microsoft.Win32;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace KdxDesigner.ViewModels
{
    public partial class IoEditorViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private readonly LinkDeviceService _linkDeviceService;
        private readonly Kdx.Infrastructure.Services.CylinderIOService _cylinderIOService;
        private readonly OperationIOService _operationIOService;
        private readonly MainViewModel _mainViewModel;

        /// <summary>
        /// データベースから読み込んだ全てのIOレコードをラップしたViewModelのリスト。
        /// </summary>
        private readonly List<IOViewModel> _allIoRecords;

        /// <summary>
        /// DataGridにバインドするための、フィルタリングとソートが可能なIOレコードのビュー。
        /// </summary>
        public ICollectionView IoRecordsView { get; }

        /// <summary>
        /// 全文検索テキストボックスにバインドされるプロパティ。
        /// </summary>
        [ObservableProperty]
        private string? _fullTextSearch;

        /// <summary>
        /// CYリスト
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Cylinder> _cyList;

        /// <summary>
        /// 選択されたCY
        /// </summary>
        [ObservableProperty]
        private Cylinder? _selectedCylinder;

        /// <summary>
        /// 選択されたCYに関連付けられたIOリスト
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Models.CylinderIOViewModel> _associatedIoList;

        /// <summary>
        /// 選択された関連付け済みIO
        /// </summary>
        [ObservableProperty]
        private Models.CylinderIOViewModel? _selectedAssociatedIo;

        /// <summary>
        /// CYリストのフィルタテキスト
        /// </summary>
        [ObservableProperty]
        private string? _cyFilterText;

        /// <summary>
        /// 関連付け済みIOリストのフィルタテキスト
        /// </summary>
        [ObservableProperty]
        private string? _associatedIoFilterText;

        /// <summary>
        /// CYリストのフィルタリングされたビュー
        /// </summary>
        public ICollectionView CyListView { get; }

        /// <summary>
        /// 関連付け済みIOリストのフィルタリングされたビュー
        /// </summary>
        public ICollectionView AssociatedIoListView { get; }

        /// <summary>
        /// Operationリスト
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Operation> _operationList;

        /// <summary>
        /// 選択されたOperation
        /// </summary>
        [ObservableProperty]
        private Operation? _selectedOperation;

        /// <summary>
        /// 選択されたOperationに関連付けられたIOリスト
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<KdxDesigner.Models.OperationIOViewModel> _operationAssociatedIoList;

        /// <summary>
        /// 選択されたOperation関連付け済みIO
        /// </summary>
        [ObservableProperty]
        private KdxDesigner.Models.OperationIOViewModel? _selectedOperationAssociatedIo;

        /// <summary>
        /// Operationリストのフィルタテキスト
        /// </summary>
        [ObservableProperty]
        private string? _operationFilterText;

        /// <summary>
        /// Operation関連付け済みIOリストのフィルタテキスト
        /// </summary>
        [ObservableProperty]
        private string? _operationAssociatedIoFilterText;

        /// <summary>
        /// Operationリストのフィルタリングされたビュー
        /// </summary>
        public ICollectionView OperationListView { get; }

        /// <summary>
        /// Operation関連付け済みIOリストのフィルタリングされたビュー
        /// </summary>
        public ICollectionView OperationAssociatedIoListView { get; }

        public IoEditorViewModel(ISupabaseRepository repository, MainViewModel mainViewModel)
        {
            _repository = repository;
            _mainViewModel = mainViewModel;
            _linkDeviceService = new LinkDeviceService(_repository);
            _cylinderIOService = new Kdx.Infrastructure.Services.CylinderIOService(_repository);
            _operationIOService = new OperationIOService(_repository);

            // ★ IOをIOViewModelでラップしてリストを作成
            _allIoRecords = Task.Run(async () => (await repository.GetIoListAsync()).Select(io => new IOViewModel(io)).ToList()).GetAwaiter().GetResult();

            IoRecordsView = CollectionViewSource.GetDefaultView(_allIoRecords);
            IoRecordsView.Filter = FilterIoRecord;

            // CYリストの初期化
            _cyList = new ObservableCollection<Cylinder>();
            _associatedIoList = new ObservableCollection<Models.CylinderIOViewModel>();

            // Operationリストの初期化
            _operationList = new ObservableCollection<Operation>();
            _operationAssociatedIoList = new ObservableCollection<Models.OperationIOViewModel>();

            // フィルタリング用のCollectionViewを作成
            CyListView = CollectionViewSource.GetDefaultView(_cyList);
            CyListView.Filter = FilterCyRecord;

            AssociatedIoListView = CollectionViewSource.GetDefaultView(_associatedIoList);
            AssociatedIoListView.Filter = FilterAssociatedIoRecord;

            OperationListView = CollectionViewSource.GetDefaultView(_operationList);
            OperationListView.Filter = FilterOperationRecord;

            OperationAssociatedIoListView = CollectionViewSource.GetDefaultView(_operationAssociatedIoList);
            OperationAssociatedIoListView.Filter = FilterOperationAssociatedIoRecord;

            LoadCyList();
            LoadOperationList();
        }

        partial void OnFullTextSearchChanged(string? value)
        {
            IoRecordsView.Refresh();
        }

        partial void OnSelectedCylinderChanged(Cylinder? value)
        {
            LoadAssociatedIoList();
        }

        partial void OnCyFilterTextChanged(string? value)
        {
            CyListView.Refresh();
        }

        partial void OnAssociatedIoFilterTextChanged(string? value)
        {
            AssociatedIoListView.Refresh();
        }

        partial void OnSelectedOperationChanged(Operation? value)
        {
            LoadOperationAssociatedIoList();
        }

        partial void OnOperationFilterTextChanged(string? value)
        {
            OperationListView.Refresh();
        }

        partial void OnOperationAssociatedIoFilterTextChanged(string? value)
        {
            OperationAssociatedIoListView.Refresh();
        }

        private bool FilterIoRecord(object item)
        {
            if (string.IsNullOrWhiteSpace(FullTextSearch))
            {
                return true;
            }

            // ★★★ 修正箇所: itemをIOViewModelとして扱う ★★★
            if (item is IOViewModel ioVm)
            {
                string searchTerm = FullTextSearch.ToLower();
                // IOViewModelのプロパティを検索
                return (ioVm.IOText?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.XComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.YComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.FComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.Address?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOName?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOExplanation?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOSpot?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.UnitName?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.System?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.StationNumber?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IONameNaked?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.LinkDevice?.ToLower().Contains(searchTerm) ?? false);
            }

            return false;
        }

        [RelayCommand]
        private async Task ExportLinkDeviceCsv()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSVファイル (*.csv)|*.csv",
                Title = "リンクデバイスCSVを保存",
                FileName = "LinkDeviceList.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // ★★★ 修正箇所: List<IOViewModel> を List<IO> に変換 ★★★
                    await _linkDeviceService.ExportLinkDeviceCsv(dialog.FileName);

                    MessageBox.Show($"CSVファイルを出力しました。\nパス: {dialog.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"CSVの出力中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        public async Task ExportLinkDeviceLadder()
        {
            try
            {

                var plcList = await _repository.GetPLCsAsync();

                foreach (var plc in plcList)
                {
                    var allOutputRows = new List<LadderCsvRow>();

                    // ★★★ 修正箇所: PLCごとにラダー出力を取得 ★★★
                    var ladderRows = await _linkDeviceService.CreateLadderCsvRows(plc);
                    if (ladderRows.Any())
                    {
                        allOutputRows.AddRange(ladderRows);
                    }
                    ExportLadderCsvFile(allOutputRows, $"LinkDevice_{plc.PlcName}.csv", "全ラダー");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSVの出力中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task SaveChanges()
        {
            try
            {
                // ★★★ 修正箇所: IsDirtyでフィルタリング ★★★
                var changedVms = _allIoRecords.Where(vm => vm.IsDirty).ToList();
                if (!changedVms.Any())
                {
                    MessageBox.Show("変更された項目はありません。", "情報");
                    return;
                }

                var histories = new List<IOHistory>();
                var ioToUpdate = new List<IO>();

                // ★★★ 修正箇所: 変数名を統一 ★★★
                var changedKeys = changedVms.Select(vm => (vm.Address, vm.PlcId)).ToHashSet();
                var originalIos = (await _repository.GetIoListAsync())
                                             .Where(io => changedKeys.Contains((io.Address, io.PlcId)))
                                             .ToDictionary(io => (io.Address, io.PlcId));

                foreach (var changedVm in changedVms)
                {
                    var updatedIo = changedVm.GetModel();
                    ioToUpdate.Add(updatedIo);

                    if (!originalIos.TryGetValue((changedVm.Address, changedVm.PlcId), out var originalIo)) { continue; }

                    var properties = typeof(IO).GetProperties();
                    foreach (var prop in properties)
                    {
                        if (prop.Name == "Id") continue;

                        var oldValue = prop.GetValue(originalIo);
                        var newValue = prop.GetValue(updatedIo);
                        var oldValueStr = oldValue?.ToString() ?? "";
                        var newValueStr = newValue?.ToString() ?? "";

                        if (oldValueStr != newValueStr)
                        {
                            histories.Add(new IOHistory
                            {
                                IoAddress = updatedIo.Address,
                                IoPlcId = updatedIo.PlcId,
                                PropertyName = prop.Name,
                                OldValue = oldValueStr,
                                NewValue = newValueStr,
                                ChangedAt = DateTime.Now.ToString(),
                                ChangedBy = "user"
                            });
                        }
                    }
                }

                // ★★★ 修正箇所: 正しいメソッドを呼び出す ★★★
                if (ioToUpdate.Any())
                {
                    await _repository.UpdateAndLogIoChangesAsync(ioToUpdate, histories);
                }

                changedVms.ForEach(vm => vm.IsDirty = false);

                MessageBox.Show($"{changedVms.Count}件の変更を保存しました。", "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// CSVファイルのエクスポート処理を共通化するヘルパーメソッド
        /// </summary>
        public void ExportLadderCsvFile(List<LadderCsvRow> rows, string fileName, string categoryName)
        {
            if (!rows.Any()) return; // 出力する行がなければ何もしない

            try
            {
                string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                LadderCsvExporter.ExportLadderCsv(rows, csvPath);
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// CYリストを読み込み
        /// </summary>
        private async void LoadCyList()
        {
            if (_mainViewModel.SelectedPlc == null) return;

            var cylinders = await _repository.GetCyListAsync(_mainViewModel.SelectedPlc.Id);
            CyList.Clear();
            foreach (var cy in cylinders.OrderBy(c => c.CYNum))
            {
                CyList.Add(cy);
            }
        }

        /// <summary>
        /// 選択されたCYに関連付けられたIOを読み込み
        /// </summary>
        private async void LoadAssociatedIoList()
        {
            AssociatedIoList.Clear();

            if (SelectedCylinder == null || _mainViewModel.SelectedPlc == null) return;

            try
            {
                var associations = _cylinderIOService.GetCylinderIOs(SelectedCylinder.Id, _mainViewModel.SelectedPlc.Id);
                var ioList = await _repository.GetIoListAsync();

                foreach (var assoc in associations)
                {
                    var io = ioList.FirstOrDefault(i => i.Address == assoc.IOAddress && i.PlcId == assoc.PlcId);
                    if (io != null)
                    {
                        AssociatedIoList.Add(new Models.CylinderIOViewModel
                        {
                            CylinderId = assoc.CylinderId,
                            IOAddress = assoc.IOAddress,
                            PlcId = assoc.PlcId,
                            IOType = assoc.IOType,
                            IOName = io.IOName,
                            IOExplanation = io.IOExplanation,
                            Comment = assoc.Comment
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // テーブルが存在しない場合は、エラーメッセージを表示せずに空のリストを保持
                Debug.WriteLine($"関連付けリスト読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// CYとIOを関連付け
        /// </summary>
        [RelayCommand]
        private void AssociateCyIo(object? parameter)
        {
            try
            {
                if (SelectedCylinder == null || _mainViewModel.SelectedPlc == null)
                {
                    MessageBox.Show("シリンダーを選択してください。", "エラー");
                    return;
                }

                var selectedIo = IoRecordsView.CurrentItem as IOViewModel;
                if (selectedIo == null)
                {
                    MessageBox.Show("IOを選択してください。", "エラー");
                    return;
                }

                var ioType = parameter as string;
                if (string.IsNullOrEmpty(ioType))
                {
                    MessageBox.Show("IOタイプを選択してください。", "エラー");
                    return;
                }

                _cylinderIOService.AddAssociation(
                    SelectedCylinder.Id,
                    selectedIo.Address ?? string.Empty,
                    _mainViewModel.SelectedPlc.Id,
                    ioType);

                LoadAssociatedIoList();
                MessageBox.Show("関連付けが完了しました。", "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"関連付け中にエラーが発生しました: {ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// CYとIOの関連を解除
        /// </summary>
        [RelayCommand]
        private void DisassociateCyIo()
        {
            try
            {
                if (SelectedAssociatedIo == null || _mainViewModel.SelectedPlc == null)
                {
                    MessageBox.Show("解除するIOを選択してください。", "エラー");
                    return;
                }

                var result = MessageBox.Show(
                    $"シリンダー{SelectedCylinder?.CYNum}とIO{SelectedAssociatedIo.IOAddress}の関連を解除しますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _cylinderIOService.RemoveAssociation(
                        SelectedAssociatedIo.CylinderId,
                        SelectedAssociatedIo.IOAddress,
                        _mainViewModel.SelectedPlc.Id);

                    LoadAssociatedIoList();
                    MessageBox.Show("関連を解除しました。", "成功");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"関連解除中にエラーが発生しました: {ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// CYリストのフィルター関数
        /// </summary>
        private bool FilterCyRecord(object item)
        {
            if (string.IsNullOrWhiteSpace(CyFilterText))
            {
                return true;
            }

            if (item is Cylinder cy)
            {
                string searchTerm = CyFilterText.ToLower();
                return (cy.CYNum?.ToLower().Contains(searchTerm) ?? false) ||
                       (cy.PUCO?.ToLower().Contains(searchTerm) ?? false) ||
                       (cy.Go?.ToLower().Contains(searchTerm) ?? false) ||
                       (cy.Back?.ToLower().Contains(searchTerm) ?? false) ||
                       (cy.OilNum?.ToLower().Contains(searchTerm) ?? false);
            }

            return false;
        }

        /// <summary>
        /// 関連付け済みIOリストのフィルター関数
        /// </summary>
        private bool FilterAssociatedIoRecord(object item)
        {
            if (string.IsNullOrWhiteSpace(AssociatedIoFilterText))
            {
                return true;
            }

            if (item is Models.CylinderIOViewModel ioVm)
            {
                string searchTerm = AssociatedIoFilterText.ToLower();
                return (ioVm.IOType?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOAddress?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOName?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOExplanation?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.Comment?.ToLower().Contains(searchTerm) ?? false);
            }

            return false;
        }

        /// <summary>
        /// Operationリストを読み込み
        /// </summary>
        private async void LoadOperationList()
        {
            if (_mainViewModel.SelectedPlc == null) return;

            var operations = await _repository.GetOperationsAsync();
            OperationList.Clear();
            foreach (var operation in operations.OrderBy(o => o.SortNumber).ThenBy(o => o.Id))
            {
                OperationList.Add(operation);
            }
        }

        /// <summary>
        /// 選択されたOperationに関連付けられたIOを読み込み
        /// </summary>
        private async void LoadOperationAssociatedIoList()
        {
            OperationAssociatedIoList.Clear();

            if (SelectedOperation == null || _mainViewModel.SelectedPlc == null) return;

            try
            {
                var associations = _operationIOService.GetOperationIOs(SelectedOperation.Id);
                var ioList = await _repository.GetIoListAsync();

                foreach (var assoc in associations)
                {
                    var io = ioList.FirstOrDefault(i => i.Address == assoc.IOAddress && i.PlcId == assoc.PlcId);
                    if (io != null)
                    {
                        OperationAssociatedIoList.Add(new Models.OperationIOViewModel
                        {
                            OperationId = assoc.OperationId,
                            IOAddress = assoc.IOAddress,
                            PlcId = assoc.PlcId,
                            IOUsage = assoc.IOUsage,
                            IOName = io.IOName,
                            IOExplanation = io.IOExplanation,
                            Comment = assoc.Comment,
                            OperationName = SelectedOperation.OperationName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // テーブルが存在しない場合は、エラーメッセージを表示せずに空のリストを保持
                Debug.WriteLine($"Operation関連付けリスト読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// OperationとIOを関連付け
        /// </summary>
        [RelayCommand]
        private void AssociateOperationIo(object? parameter)
        {
            try
            {
                if (SelectedOperation == null || _mainViewModel.SelectedPlc == null)
                {
                    MessageBox.Show("Operationを選択してください。", "エラー");
                    return;
                }

                var selectedIo = IoRecordsView.CurrentItem as IOViewModel;
                if (selectedIo == null)
                {
                    MessageBox.Show("IOを選択してください。", "エラー");
                    return;
                }

                var ioUsage = parameter as string;
                if (string.IsNullOrEmpty(ioUsage))
                {
                    MessageBox.Show("IO用途を選択してください。", "エラー");
                    return;
                }

                _operationIOService.AddAssociation(
                    SelectedOperation.Id,
                    selectedIo.Address ?? string.Empty,
                    _mainViewModel.SelectedPlc.Id,
                    ioUsage);

                LoadOperationAssociatedIoList();
                MessageBox.Show("関連付けが完了しました。", "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"関連付け中にエラーが発生しました: {ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// OperationとIOの関連を解除
        /// </summary>
        [RelayCommand]
        private void DisassociateOperationIo()
        {
            try
            {
                if (SelectedOperationAssociatedIo == null || _mainViewModel.SelectedPlc == null)
                {
                    MessageBox.Show("解除するIOを選択してください。", "エラー");
                    return;
                }

                var result = MessageBox.Show(
                    $"Operation{SelectedOperation?.OperationName}とIO{SelectedOperationAssociatedIo.IOAddress}の関連を解除しますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _operationIOService.RemoveAssociation(
                        SelectedOperationAssociatedIo.OperationId,
                        SelectedOperationAssociatedIo.IOAddress,
                        _mainViewModel.SelectedPlc.Id);

                    LoadOperationAssociatedIoList();
                    MessageBox.Show("関連を解除しました。", "成功");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"関連解除中にエラーが発生しました: {ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// Operationリストのフィルター関数
        /// </summary>
        private bool FilterOperationRecord(object item)
        {
            if (string.IsNullOrWhiteSpace(OperationFilterText))
            {
                return true;
            }

            if (item is Operation operation)
            {
                string searchTerm = OperationFilterText.ToLower();
                return (operation.OperationName?.ToLower().Contains(searchTerm) ?? false) ||
                       (operation.Id.ToString().Contains(searchTerm)) ||
                       (operation.CYId?.ToString().Contains(searchTerm) ?? false) ||
                       (operation.CategoryId?.ToString().Contains(searchTerm) ?? false);
            }

            return false;
        }

        /// <summary>
        /// Operation関連付け済みIOリストのフィルター関数
        /// </summary>
        private bool FilterOperationAssociatedIoRecord(object item)
        {
            if (string.IsNullOrWhiteSpace(OperationAssociatedIoFilterText))
            {
                return true;
            }

            if (item is Models.OperationIOViewModel ioVm)
            {
                string searchTerm = OperationAssociatedIoFilterText.ToLower();
                return (ioVm.IOUsage?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOAddress?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOName?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOExplanation?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.Comment?.ToLower().Contains(searchTerm) ?? false);
            }

            return false;
        }
    }
}
