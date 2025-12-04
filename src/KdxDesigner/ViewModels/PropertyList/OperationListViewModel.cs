using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// OperationListのViewModel
    /// </summary>
    public partial class OperationListViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;

        public OperationListViewModel(ISupabaseRepository repository)
        {
            _repository = repository;
        }

        #region Observable Properties

        /// <summary>
        /// Operationのコレクション
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Operation> _operations = new();

        /// <summary>
        /// 選択されたOperation
        /// </summary>
        [ObservableProperty]
        private Operation? _selectedOperation;

        /// <summary>
        /// Operationカテゴリのコレクション
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<OperationCategory> _operationCategories = new();

        /// <summary>
        /// 選択されたサイクルID
        /// </summary>
        [ObservableProperty]
        private int? _cycleId;

        /// <summary>
        /// 選択されたPLC ID
        /// </summary>
        [ObservableProperty]
        private int? _plcId;

        #endregion

        #region Events

        /// <summary>
        /// Operationが削除されたときに発生するイベント
        /// </summary>
        public event EventHandler<Operation>? OperationDeleted;

        /// <summary>
        /// Operationが更新されたときに発生するイベント
        /// </summary>
        public event EventHandler<Operation>? OperationUpdated;

        /// <summary>
        /// Operationが追加されたときに発生するイベント
        /// </summary>
        public event EventHandler<Operation>? OperationAdded;

        #endregion

        #region Commands

        /// <summary>
        /// 新しいOperationを追加
        /// </summary>
        [RelayCommand]
        private async Task AddNewOperation()
        {
            if (CycleId == null)
            {
                MessageBox.Show("サイクルを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 新しいOperationオブジェクトを作成
                var newOperation = new Operation
                {
                    OperationName = "新規操作",
                    CycleId = CycleId.Value,
                    SortNumber = Operations.Count > 0 ? Operations.Max(o => o.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository.AddOperationAsync(newOperation);
                newOperation.Id = newId;

                // コレクションに追加
                Operations.Add(newOperation);

                // イベントを発火
                OperationAdded?.Invoke(this, newOperation);

                MessageBox.Show("新しい操作を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 選択されたOperationを削除
        /// </summary>
        [RelayCommand]
        private async Task DeleteSelectedOperation()
        {
            if (SelectedOperation == null) return;

            var result = MessageBox.Show(
                $"操作 '{SelectedOperation.OperationName}' を削除しますか？",
                "削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // データベースから削除
                    await _repository.DeleteOperationAsync(SelectedOperation.Id);

                    // コレクションから削除
                    var deletedOperation = SelectedOperation;
                    Operations.Remove(SelectedOperation);

                    // イベントを発火
                    OperationDeleted?.Invoke(this, deletedOperation);

                    MessageBox.Show("操作を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 選択されたOperationのプロパティを編集
        /// </summary>
        [RelayCommand]
        private async Task EditOperation()
        {
            if (SelectedOperation == null)
            {
                MessageBox.Show("編集する操作を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // OperationPropertiesWindowを開く
                var window = new OperationPropertiesWindow(_repository, SelectedOperation, PlcId);
                var mainWindow = Application.Current.Windows.OfType<MainView>().FirstOrDefault();
                if (mainWindow != null)
                {
                    window.Owner = mainWindow;
                }

                if (window.ShowDialog() == true)
                {
                    // Operationの更新をUIに反映
                    await ReloadOperationsAsync();

                    // イベントを発火
                    OperationUpdated?.Invoke(this, SelectedOperation);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作の編集中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Operationリストを読み込む
        /// </summary>
        public async Task LoadOperationsAsync()
        {
            if (CycleId == null) return;

            try
            {
                var operations = await _repository.GetOperationsAsync();
                var cycleOperations = operations
                    .Where(o => o.CycleId == CycleId.Value)
                    .OrderBy(o => o.SortNumber)
                    .ToList();

                Operations = new ObservableCollection<Operation>(cycleOperations);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作の読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Operationリストを再読み込み
        /// </summary>
        public async Task ReloadOperationsAsync()
        {
            if (CycleId == null) return;

            try
            {
                var operations = await _repository.GetOperationsAsync();
                var cycleOperations = operations
                    .Where(o => o.CycleId == CycleId.Value)
                    .OrderBy(o => o.SortNumber)
                    .ToList();

                // ObservableCollectionを新しく作成して確実にUIを更新
                Operations = new ObservableCollection<Operation>(cycleOperations);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作の再読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Operationカテゴリを読み込む
        /// </summary>
        public async Task LoadOperationCategoriesAsync()
        {
            try
            {
                var categories = await _repository.GetOperationCategoriesAsync();
                OperationCategories = new ObservableCollection<OperationCategory>(categories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"カテゴリの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
