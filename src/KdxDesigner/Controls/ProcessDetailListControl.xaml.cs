using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
using KdxDesigner.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KdxDesigner.Controls
{
    /// <summary>
    /// ProcessDetailListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ProcessDetailListControl : UserControl
    {
        public ProcessDetailListControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>
        /// Supabaseリポジトリ
        /// </summary>
        public static readonly DependencyProperty RepositoryProperty =
            DependencyProperty.Register(
                nameof(Repository),
                typeof(ISupabaseRepository),
                typeof(ProcessDetailListControl),
                new PropertyMetadata(null));

        public ISupabaseRepository? Repository
        {
            get => (ISupabaseRepository?)GetValue(RepositoryProperty);
            set => SetValue(RepositoryProperty, value);
        }

        /// <summary>
        /// ProcessDetailのコレクション
        /// </summary>
        public static readonly DependencyProperty ProcessDetailsProperty =
            DependencyProperty.Register(
                nameof(ProcessDetails),
                typeof(ObservableCollection<ProcessDetail>),
                typeof(ProcessDetailListControl),
                new PropertyMetadata(null));

        public ObservableCollection<ProcessDetail>? ProcessDetails
        {
            get => (ObservableCollection<ProcessDetail>?)GetValue(ProcessDetailsProperty);
            set => SetValue(ProcessDetailsProperty, value);
        }

        /// <summary>
        /// 選択されたProcessDetail
        /// </summary>
        public static readonly DependencyProperty SelectedProcessDetailProperty =
            DependencyProperty.Register(
                nameof(SelectedProcessDetail),
                typeof(ProcessDetail),
                typeof(ProcessDetailListControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ProcessDetail? SelectedProcessDetail
        {
            get => (ProcessDetail?)GetValue(SelectedProcessDetailProperty);
            set => SetValue(SelectedProcessDetailProperty, value);
        }

        /// <summary>
        /// Operationのコレクション（ComboBox用）
        /// </summary>
        public static readonly DependencyProperty OperationsProperty =
            DependencyProperty.Register(
                nameof(Operations),
                typeof(ObservableCollection<Operation>),
                typeof(ProcessDetailListControl),
                new PropertyMetadata(null));

        public ObservableCollection<Operation>? Operations
        {
            get => (ObservableCollection<Operation>?)GetValue(OperationsProperty);
            set => SetValue(OperationsProperty, value);
        }

        /// <summary>
        /// ProcessDetailカテゴリのコレクション
        /// </summary>
        public static readonly DependencyProperty ProcessDetailCategoriesProperty =
            DependencyProperty.Register(
                nameof(ProcessDetailCategories),
                typeof(ObservableCollection<ProcessDetailCategory>),
                typeof(ProcessDetailListControl),
                new PropertyMetadata(null));

        public ObservableCollection<ProcessDetailCategory>? ProcessDetailCategories
        {
            get => (ObservableCollection<ProcessDetailCategory>?)GetValue(ProcessDetailCategoriesProperty);
            set => SetValue(ProcessDetailCategoriesProperty, value);
        }

        /// <summary>
        /// 選択されたプロセスID
        /// </summary>
        public static readonly DependencyProperty ProcessIdProperty =
            DependencyProperty.Register(
                nameof(ProcessId),
                typeof(int?),
                typeof(ProcessDetailListControl),
                new PropertyMetadata(null));

        public int? ProcessId
        {
            get => (int?)GetValue(ProcessIdProperty);
            set => SetValue(ProcessIdProperty, value);
        }

        /// <summary>
        /// 選択されたサイクルID（新規追加時のデフォルト値として使用）
        /// </summary>
        public static readonly DependencyProperty CycleIdProperty =
            DependencyProperty.Register(
                nameof(CycleId),
                typeof(int?),
                typeof(ProcessDetailListControl),
                new PropertyMetadata(null));

        public int? CycleId
        {
            get => (int?)GetValue(CycleIdProperty);
            set => SetValue(CycleIdProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// ProcessDetailの選択が変更されたときに発生するイベント
        /// </summary>
        public event EventHandler<ProcessDetail?>? ProcessDetailSelectionChanged;

        /// <summary>
        /// ProcessDetailが削除されたときに発生するイベント
        /// </summary>
        public event EventHandler<ProcessDetail>? ProcessDetailDeleted;

        /// <summary>
        /// ProcessDetailが更新されたときに発生するイベント
        /// </summary>
        public event EventHandler<ProcessDetail>? ProcessDetailUpdated;

        #endregion

        #region Event Handlers

        private void ProcessDetailGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProcessDetailGrid.SelectedItem is ProcessDetail selectedDetail)
            {
                SelectedProcessDetail = selectedDetail;
                ProcessDetailSelectionChanged?.Invoke(this, selectedDetail);
            }
            else
            {
                SelectedProcessDetail = null;
                ProcessDetailSelectionChanged?.Invoke(this, null);
            }
        }

        private void ProcessDetailGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && SelectedProcessDetail != null)
            {
                DeleteProcessDetail_Click(sender, e);
                e.Handled = true;
            }
        }

        private void ProcessDetailGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ヘッダーやスクロールバーをダブルクリックした場合は無視
            if (ProcessDetailGrid.SelectedItem != null && SelectedProcessDetail != null)
            {
                EditProcessDetailProperties_Click(sender, e);
            }
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// 新しいProcessDetailを追加
        /// </summary>
        private async void AddNewProcessDetail_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessId == null || Repository == null)
            {
                MessageBox.Show("工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 新しいProcessDetailオブジェクトを作成
                var newDetail = new ProcessDetail
                {
                    ProcessId = ProcessId.Value,
                    DetailName = "新規詳細",
                    CycleId = CycleId, // MainViewで選択されているCycleIdを設定
                    SortNumber = ProcessDetails?.Count > 0 ? ProcessDetails.Max(d => d.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await Repository.AddProcessDetailAsync(newDetail);
                newDetail.Id = newId;

                // コレクションに追加
                ProcessDetails?.Add(newDetail);

                MessageBox.Show("新しい工程詳細を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程詳細の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 選択したProcessDetailをコピーして新規追加
        /// </summary>
        private async void CopyProcessDetail_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProcessDetail == null || ProcessId == null || Repository == null)
            {
                MessageBox.Show("コピーする工程詳細を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 選択されたProcessDetailをコピー
                var copiedDetail = new ProcessDetail
                {
                    ProcessId = ProcessId.Value,
                    DetailName = SelectedProcessDetail.DetailName + " (コピー)",
                    OperationId = SelectedProcessDetail.OperationId,
                    StartSensor = SelectedProcessDetail.StartSensor,
                    FinishSensor = SelectedProcessDetail.FinishSensor,
                    CategoryId = SelectedProcessDetail.CategoryId,
                    BlockNumber = SelectedProcessDetail.BlockNumber,
                    SkipMode = SelectedProcessDetail.SkipMode,
                    CycleId = SelectedProcessDetail.CycleId,
                    Comment = SelectedProcessDetail.Comment,
                    ILStart = SelectedProcessDetail.ILStart,
                    StartTimerId = SelectedProcessDetail.StartTimerId,
                    SortNumber = ProcessDetails?.Count > 0 ? ProcessDetails.Max(d => d.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await Repository.AddProcessDetailAsync(copiedDetail);
                copiedDetail.Id = newId;

                // コレクションに追加
                ProcessDetails?.Add(copiedDetail);

                MessageBox.Show("工程詳細をコピーして追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程詳細のコピー中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// データベースから最新のProcessDetailリストを再読み込み
        /// </summary>
        private async void ReloadProcessDetails_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessId == null || Repository == null)
            {
                MessageBox.Show("工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // データベースから全てのProcessDetailを取得してProcessIdでフィルタリング
                var allDetails = await Repository.GetProcessDetailsAsync();
                var processDetails = allDetails
                    .Where(d => d.ProcessId == ProcessId.Value)
                    .OrderBy(d => d.SortNumber);

                // コレクションをクリアして再設定
                ProcessDetails?.Clear();
                foreach (var detail in processDetails)
                {
                    ProcessDetails?.Add(detail);
                }

                MessageBox.Show("データを再読み込みしました。", "再読み込み完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"再読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Context Menu Handlers

        private async void DeleteProcessDetail_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProcessDetail == null || Repository == null) return;

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
                    await Repository.DeleteProcessDetailAsync(SelectedProcessDetail.Id);

                    // コレクションから削除
                    ProcessDetails?.Remove(SelectedProcessDetail);

                    // イベントを発火
                    ProcessDetailDeleted?.Invoke(this, SelectedProcessDetail);

                    MessageBox.Show("工程詳細を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditProcessDetailProperties_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProcessDetail == null || Repository == null)
            {
                MessageBox.Show("編集する工程詳細を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ProcessDetailPropertiesViewModelを作成
                var viewModel = new ProcessDetailPropertiesViewModel(
                    Repository,
                    SelectedProcessDetail,
                    SelectedProcessDetail.CycleId
                );

                // ProcessDetailPropertiesWindowを作成
                var window = new ProcessDetailPropertiesWindow
                {
                    DataContext = viewModel,
                    Owner = Window.GetWindow(this)
                };

                // ウィンドウを開く
                window.ShowDialog();

                // 保存された場合はイベントを発火
                if (viewModel.DialogResult)
                {
                    ProcessDetailUpdated?.Invoke(this, SelectedProcessDetail);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"プロパティウィンドウの表示中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
