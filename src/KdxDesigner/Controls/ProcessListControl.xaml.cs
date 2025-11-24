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
    /// ProcessListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ProcessListControl : UserControl
    {
        public ProcessListControl()
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
                typeof(ProcessListControl),
                new PropertyMetadata(null));

        public ISupabaseRepository? Repository
        {
            get => (ISupabaseRepository?)GetValue(RepositoryProperty);
            set => SetValue(RepositoryProperty, value);
        }

        /// <summary>
        /// Processのコレクション
        /// </summary>
        public static readonly DependencyProperty ProcessesProperty =
            DependencyProperty.Register(
                nameof(Processes),
                typeof(ObservableCollection<Process>),
                typeof(ProcessListControl),
                new PropertyMetadata(null));

        public ObservableCollection<Process>? Processes
        {
            get => (ObservableCollection<Process>?)GetValue(ProcessesProperty);
            set => SetValue(ProcessesProperty, value);
        }

        /// <summary>
        /// 選択されたProcess
        /// </summary>
        public static readonly DependencyProperty SelectedProcessProperty =
            DependencyProperty.Register(
                nameof(SelectedProcess),
                typeof(Process),
                typeof(ProcessListControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Process? SelectedProcess
        {
            get => (Process?)GetValue(SelectedProcessProperty);
            set => SetValue(SelectedProcessProperty, value);
        }

        /// <summary>
        /// Processカテゴリのコレクション
        /// </summary>
        public static readonly DependencyProperty ProcessCategoriesProperty =
            DependencyProperty.Register(
                nameof(ProcessCategories),
                typeof(ObservableCollection<ProcessCategory>),
                typeof(ProcessListControl),
                new PropertyMetadata(null));

        public ObservableCollection<ProcessCategory>? ProcessCategories
        {
            get => (ObservableCollection<ProcessCategory>?)GetValue(ProcessCategoriesProperty);
            set => SetValue(ProcessCategoriesProperty, value);
        }

        /// <summary>
        /// 選択されたサイクルID
        /// </summary>
        public static readonly DependencyProperty CycleIdProperty =
            DependencyProperty.Register(
                nameof(CycleId),
                typeof(int?),
                typeof(ProcessListControl),
                new PropertyMetadata(null));

        public int? CycleId
        {
            get => (int?)GetValue(CycleIdProperty);
            set => SetValue(CycleIdProperty, value);
        }

        /// <summary>
        /// 選択されたPLC ID
        /// </summary>
        public static readonly DependencyProperty PlcIdProperty =
            DependencyProperty.Register(
                nameof(PlcId),
                typeof(int?),
                typeof(ProcessListControl),
                new PropertyMetadata(null));

        public int? PlcId
        {
            get => (int?)GetValue(PlcIdProperty);
            set => SetValue(PlcIdProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Processの選択が変更されたときに発生するイベント
        /// </summary>
        public event EventHandler<Process?>? ProcessSelectionChanged;

        /// <summary>
        /// Processが削除されたときに発生するイベント
        /// </summary>
        public event EventHandler<Process>? ProcessDeleted;

        /// <summary>
        /// Processが更新されたときに発生するイベント
        /// </summary>
        public event EventHandler<Process>? ProcessUpdated;

        /// <summary>
        /// プロセスフロー詳細を開くリクエストが発生したときのイベント
        /// </summary>
        public event EventHandler? OpenProcessFlowDetailRequested;

        #endregion

        #region Event Handlers

        private void ProcessGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProcessGrid.SelectedItem is Process selectedProcess)
            {
                SelectedProcess = selectedProcess;
                ProcessSelectionChanged?.Invoke(this, selectedProcess);
            }
            else
            {
                SelectedProcess = null;
                ProcessSelectionChanged?.Invoke(this, null);
            }
        }

        private void ProcessGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && SelectedProcess != null)
            {
                DeleteProcess_Click(sender, e);
                e.Handled = true;
            }
        }

        private void ProcessGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ヘッダーやスクロールバーをダブルクリックした場合は無視
            if (ProcessGrid.SelectedItem != null && SelectedProcess != null)
            {
                EditProcessProperties_Click(sender, e);
            }
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// 新しいProcessを追加
        /// </summary>
        private async void AddNewProcess_Click(object sender, RoutedEventArgs e)
        {
            if (CycleId == null || Repository == null)
            {
                MessageBox.Show("サイクルを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 新しいProcessオブジェクトを作成
                var newProcess = new Process
                {
                    ProcessName = "新規工程",
                    CycleId = CycleId.Value,
                    SortNumber = Processes?.Count > 0 ? Processes.Max(p => p.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await Repository.AddProcessAsync(newProcess);
                newProcess.Id = newId;

                // コレクションに追加
                Processes?.Add(newProcess);

                MessageBox.Show("新しい工程を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 選択したProcessをコピーして新規追加
        /// </summary>
        private async void CopyProcess_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProcess == null || CycleId == null || Repository == null)
            {
                MessageBox.Show("コピーする工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 選択されたProcessをコピー
                var copiedProcess = new Process
                {
                    ProcessName = SelectedProcess.ProcessName + " (コピー)",
                    CycleId = CycleId.Value,
                    TestStart = SelectedProcess.TestStart,
                    TestCondition = SelectedProcess.TestCondition,
                    TestMode = SelectedProcess.TestMode,
                    AutoMode = SelectedProcess.AutoMode,
                    AutoStart = SelectedProcess.AutoStart,
                    ProcessCategoryId = SelectedProcess.ProcessCategoryId,
                    ILStart = SelectedProcess.ILStart,
                    Comment1 = SelectedProcess.Comment1,
                    Comment2 = SelectedProcess.Comment2,
                    SortNumber = Processes?.Count > 0 ? Processes.Max(p => p.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await Repository.AddProcessAsync(copiedProcess);
                copiedProcess.Id = newId;

                // コレクションに追加
                Processes?.Add(copiedProcess);

                MessageBox.Show("工程をコピーして追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程のコピー中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// データベースから最新のProcessリストを再読み込み
        /// </summary>
        private async void ReloadProcesses_Click(object sender, RoutedEventArgs e)
        {
            if (CycleId == null || Repository == null)
            {
                MessageBox.Show("サイクルを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // データベースから全てのProcessを取得してCycleIdでフィルタリング
                var allProcesses = await Repository.GetProcessesAsync();
                var processes = allProcesses
                    .Where(p => p.CycleId == CycleId.Value)
                    .OrderBy(p => p.SortNumber);

                // コレクションをクリアして再設定
                Processes?.Clear();
                foreach (var process in processes)
                {
                    Processes?.Add(process);
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

        private async void DeleteProcess_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProcess == null || Repository == null) return;

            var result = MessageBox.Show(
                $"工程 '{SelectedProcess.ProcessName}' を削除しますか？\n関連する工程詳細も削除されます。",
                "削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // データベースから削除
                    await Repository.DeleteProcessAsync(SelectedProcess.Id);

                    // コレクションから削除
                    Processes?.Remove(SelectedProcess);

                    // イベントを発火
                    ProcessDeleted?.Invoke(this, SelectedProcess);

                    MessageBox.Show("工程を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditProcessProperties_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProcess == null || Repository == null)
            {
                MessageBox.Show("編集する工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // DataGridの編集トランザクションをコミット（編集モードを終了）
                ProcessGrid.CommitEdit(DataGridEditingUnit.Row, true);

                // 編集前のIDを保存
                var selectedProcessId = SelectedProcess.Id;

                // ProcessPropertiesWindowを開く
                var window = new ProcessPropertiesWindow(Repository, SelectedProcess)
                {
                    Owner = Window.GetWindow(this)
                };

                if (window.ShowDialog() == true)
                {
                    // イベントを発火
                    ProcessUpdated?.Invoke(this, SelectedProcess);

                    MessageBox.Show("工程を更新しました。", "更新完了", MessageBoxButton.OK, MessageBoxImage.Information);

                    // データベースから最新のデータを再読み込みしてUIを更新
                    if (CycleId != null)
                    {
                        var allProcesses = await Repository.GetProcessesAsync();
                        var processes = allProcesses
                            .Where(p => p.CycleId == CycleId.Value)
                            .OrderBy(p => p.SortNumber)
                            .ToList();

                        // 新しいObservableCollectionを作成して置き換え（編集トランザクションの競合を回避）
                        Processes = new ObservableCollection<Process>(processes);

                        // 編集したプロセスを再選択
                        SelectedProcess = Processes?.FirstOrDefault(p => p.Id == selectedProcessId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の編集中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenProcessFlowDetail_Click(object sender, RoutedEventArgs e)
        {
            // イベントを発火してMainViewModelに処理を委譲
            OpenProcessFlowDetailRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
