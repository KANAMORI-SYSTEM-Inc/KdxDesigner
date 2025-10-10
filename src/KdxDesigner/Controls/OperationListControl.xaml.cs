using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KdxDesigner.Controls
{
    /// <summary>
    /// OperationListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class OperationListControl : UserControl
    {
        public OperationListControl()
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
                typeof(OperationListControl),
                new PropertyMetadata(null));

        public ISupabaseRepository? Repository
        {
            get => (ISupabaseRepository?)GetValue(RepositoryProperty);
            set => SetValue(RepositoryProperty, value);
        }

        /// <summary>
        /// Operationのコレクション
        /// </summary>
        public static readonly DependencyProperty OperationsProperty =
            DependencyProperty.Register(
                nameof(Operations),
                typeof(ObservableCollection<Operation>),
                typeof(OperationListControl),
                new PropertyMetadata(null));

        public ObservableCollection<Operation>? Operations
        {
            get => (ObservableCollection<Operation>?)GetValue(OperationsProperty);
            set => SetValue(OperationsProperty, value);
        }

        /// <summary>
        /// 選択されたOperation
        /// </summary>
        public static readonly DependencyProperty SelectedOperationProperty =
            DependencyProperty.Register(
                nameof(SelectedOperation),
                typeof(Operation),
                typeof(OperationListControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Operation? SelectedOperation
        {
            get => (Operation?)GetValue(SelectedOperationProperty);
            set => SetValue(SelectedOperationProperty, value);
        }

        /// <summary>
        /// Operationカテゴリのコレクション
        /// </summary>
        public static readonly DependencyProperty OperationCategoriesProperty =
            DependencyProperty.Register(
                nameof(OperationCategories),
                typeof(ObservableCollection<OperationCategory>),
                typeof(OperationListControl),
                new PropertyMetadata(null));

        public ObservableCollection<OperationCategory>? OperationCategories
        {
            get => (ObservableCollection<OperationCategory>?)GetValue(OperationCategoriesProperty);
            set => SetValue(OperationCategoriesProperty, value);
        }

        /// <summary>
        /// 選択されたPLC ID
        /// </summary>
        public static readonly DependencyProperty PlcIdProperty =
            DependencyProperty.Register(
                nameof(PlcId),
                typeof(int?),
                typeof(OperationListControl),
                new PropertyMetadata(null));

        public int? PlcId
        {
            get => (int?)GetValue(PlcIdProperty);
            set => SetValue(PlcIdProperty, value);
        }

        /// <summary>
        /// 選択されたサイクルID
        /// </summary>
        public static readonly DependencyProperty CycleIdProperty =
            DependencyProperty.Register(
                nameof(CycleId),
                typeof(int?),
                typeof(OperationListControl),
                new PropertyMetadata(null));

        public int? CycleId
        {
            get => (int?)GetValue(CycleIdProperty);
            set => SetValue(CycleIdProperty, value);
        }

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

        #endregion

        #region Event Handlers

        private void OperationGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && SelectedOperation != null)
            {
                DeleteOperation_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void OperationGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ヘッダーやスクロールバーをダブルクリックした場合は無視
            if (OperationGrid.SelectedItem is Operation selectedOperation && Repository != null)
            {
                var window = new OperationPropertiesWindow(Repository, selectedOperation, PlcId)
                {
                    Owner = Window.GetWindow(this)
                };

                if (window.ShowDialog() == true)
                {
                    // 更新されたOperationの情報をUIに反映
                    var index = Operations?.IndexOf(selectedOperation) ?? -1;
                    if (index >= 0 && Operations != null)
                    {
                        Operations[index] = selectedOperation;
                    }

                    // イベントを発火
                    OperationUpdated?.Invoke(this, selectedOperation);
                }
            }
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// 新しいOperationを追加
        /// </summary>
        private async void AddNewOperation_Click(object sender, RoutedEventArgs e)
        {
            if (CycleId == null || Repository == null)
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
                    SortNumber = Operations?.Count > 0 ? Operations.Max(o => o.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await Repository.AddOperationAsync(newOperation);
                newOperation.Id = newId;

                // コレクションに追加
                Operations?.Add(newOperation);

                MessageBox.Show("新しい操作を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 選択したOperationをコピーして新規追加
        /// </summary>
        private async void CopyOperation_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedOperation == null || CycleId == null || Repository == null)
            {
                MessageBox.Show("コピーする操作を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 選択されたOperationをコピー
                var copiedOperation = new Operation
                {
                    OperationName = SelectedOperation.OperationName + " (コピー)",
                    CycleId = CycleId.Value,
                    CategoryId = SelectedOperation.CategoryId,
                    CYId = SelectedOperation.CYId,
                    GoBack = SelectedOperation.GoBack,
                    Start = SelectedOperation.Start,
                    Finish = SelectedOperation.Finish,
                    Valve1 = SelectedOperation.Valve1,
                    S1 = SelectedOperation.S1,
                    S2 = SelectedOperation.S2,
                    S3 = SelectedOperation.S3,
                    S4 = SelectedOperation.S4,
                    S5 = SelectedOperation.S5,
                    SS1 = SelectedOperation.SS1,
                    SS2 = SelectedOperation.SS2,
                    SS3 = SelectedOperation.SS3,
                    SS4 = SelectedOperation.SS4,
                    PIL = SelectedOperation.PIL,
                    SC = SelectedOperation.SC,
                    FC = SelectedOperation.FC,
                    Con = SelectedOperation.Con,
                    SortNumber = Operations?.Count > 0 ? Operations.Max(o => o.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await Repository.AddOperationAsync(copiedOperation);
                copiedOperation.Id = newId;

                // コレクションに追加
                Operations?.Add(copiedOperation);

                MessageBox.Show("操作をコピーして追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作のコピー中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// データベースから最新のOperationリストを再読み込み
        /// </summary>
        private async void ReloadOperations_Click(object sender, RoutedEventArgs e)
        {
            if (CycleId == null || Repository == null)
            {
                MessageBox.Show("サイクルを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // データベースから最新のOperationリストを取得
                var operations = await Repository.GetOperationsByCycleIdAsync(CycleId.Value);

                // コレクションをクリアして再設定
                Operations?.Clear();
                foreach (var operation in operations.OrderBy(o => o.SortNumber))
                {
                    Operations?.Add(operation);
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

        private async void DeleteOperation_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedOperation == null || Repository == null) return;

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
                    await Repository.DeleteOperationAsync(SelectedOperation.Id);

                    // コレクションから削除
                    Operations?.Remove(SelectedOperation);

                    // イベントを発火
                    OperationDeleted?.Invoke(this, SelectedOperation);

                    MessageBox.Show("操作を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditOperationProperties_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedOperation == null || Repository == null)
            {
                MessageBox.Show("編集する操作を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // OperationPropertiesWindowを開く
                var window = new OperationPropertiesWindow(Repository, SelectedOperation, PlcId)
                {
                    Owner = Window.GetWindow(this)
                };

                if (window.ShowDialog() == true)
                {
                    // 更新されたOperationの情報をUIに反映
                    var index = Operations?.IndexOf(SelectedOperation) ?? -1;
                    if (index >= 0 && Operations != null)
                    {
                        Operations[index] = SelectedOperation;
                    }

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
    }
}
