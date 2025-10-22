using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// ProcessDetailListのViewModel
    /// </summary>
    public partial class ProcessDetailListViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;

        public ProcessDetailListViewModel(ISupabaseRepository repository)
        {
            _repository = repository;
        }

        #region Observable Properties

        /// <summary>
        /// ProcessDetailのコレクション
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ProcessDetail> _processDetails = new();

        /// <summary>
        /// 選択されたProcessDetail
        /// </summary>
        [ObservableProperty]
        private ProcessDetail? _selectedProcessDetail;

        /// <summary>
        /// Operationのコレクション（ComboBox用）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Operation> _operations = new();

        /// <summary>
        /// ProcessDetailカテゴリのコレクション
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ProcessDetailCategory> _processDetailCategories = new();

        /// <summary>
        /// 選択されたプロセスID
        /// </summary>
        [ObservableProperty]
        private int? _processId;

        #endregion

        #region Events

        /// <summary>
        /// ProcessDetailが削除されたときに発生するイベント
        /// </summary>
        public event EventHandler<ProcessDetail>? ProcessDetailDeleted;

        /// <summary>
        /// ProcessDetailが更新されたときに発生するイベント
        /// </summary>
        public event EventHandler<ProcessDetail>? ProcessDetailUpdated;

        /// <summary>
        /// ProcessDetailが追加されたときに発生するイベント
        /// </summary>
        public event EventHandler<ProcessDetail>? ProcessDetailAdded;

        #endregion

        #region Commands

        /// <summary>
        /// 新しいProcessDetailを追加
        /// </summary>
        [RelayCommand]
        private async Task AddNewProcessDetail()
        {
            if (ProcessId == null)
            {
                MessageBox.Show("工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 新しいProcessDetailオブジェクトを作成
                var newDetail = new ProcessDetail
                {
                    DetailName = "新規工程詳細",
                    ProcessId = ProcessId.Value,
                    SortNumber = ProcessDetails.Count > 0 ? ProcessDetails.Max(d => d.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository.AddProcessDetailAsync(newDetail);
                newDetail.Id = newId;

                // コレクションに追加
                ProcessDetails.Add(newDetail);

                // イベントを発火
                ProcessDetailAdded?.Invoke(this, newDetail);

                MessageBox.Show("新しい工程詳細を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程詳細の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 選択されたProcessDetailを削除
        /// </summary>
        [RelayCommand]
        private async Task DeleteSelectedProcessDetail()
        {
            if (SelectedProcessDetail == null) return;

            var result = MessageBox.Show(
                $"工程詳細 '{SelectedProcessDetail.DetailName}' を削除しますか？",
                "削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // データベースから削除
                    await _repository.DeleteProcessDetailAsync(SelectedProcessDetail.Id);

                    // コレクションから削除
                    var deletedDetail = SelectedProcessDetail;
                    ProcessDetails.Remove(SelectedProcessDetail);

                    // イベントを発火
                    ProcessDetailDeleted?.Invoke(this, deletedDetail);

                    MessageBox.Show("工程詳細を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 選択されたProcessDetailのプロパティを編集
        /// </summary>
        [RelayCommand]
        private void EditProcessDetail()
        {
            if (SelectedProcessDetail == null)
            {
                MessageBox.Show("編集する工程詳細を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: ProcessDetailPropertiesWindowを実装
            MessageBox.Show("工程詳細のプロパティ編集機能は今後実装予定です。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ProcessDetailリストを読み込む
        /// </summary>
        public async Task LoadProcessDetailsAsync()
        {
            if (ProcessId == null) return;

            try
            {
                var details = await _repository.GetProcessDetailsAsync();
                var processDetails = details
                    .Where(d => d.ProcessId == ProcessId.Value)
                    .OrderBy(d => d.SortNumber)
                    .ToList();

                ProcessDetails = new ObservableCollection<ProcessDetail>(processDetails);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程詳細の読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ProcessDetailカテゴリを読み込む
        /// </summary>
        public async Task LoadProcessDetailCategoriesAsync()
        {
            try
            {
                var categories = await _repository.GetProcessDetailCategoriesAsync();
                ProcessDetailCategories = new ObservableCollection<ProcessDetailCategory>(categories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"カテゴリの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Operationリストを読み込む
        /// </summary>
        public async Task LoadOperationsAsync(int cycleId)
        {
            try
            {
                var operations = await _repository.GetOperationsAsync();
                var cycleOperations = operations
                    .Where(o => o.CycleId == cycleId)
                    .OrderBy(o => o.SortNumber)
                    .ToList();

                Operations = new ObservableCollection<Operation>(cycleOperations);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作の読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
