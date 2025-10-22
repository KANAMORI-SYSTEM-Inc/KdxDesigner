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
    /// ProcessListのViewModel
    /// </summary>
    public partial class ProcessListViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;

        public ProcessListViewModel(ISupabaseRepository repository)
        {
            _repository = repository;
        }

        #region Observable Properties

        /// <summary>
        /// Processのコレクション
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Process> _processes = new();

        /// <summary>
        /// 選択されたProcess
        /// </summary>
        [ObservableProperty]
        private Process? _selectedProcess;

        /// <summary>
        /// Processカテゴリのコレクション
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ProcessCategory> _processCategories = new();

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
        /// Processが削除されたときに発生するイベント
        /// </summary>
        public event EventHandler<Process>? ProcessDeleted;

        /// <summary>
        /// Processが更新されたときに発生するイベント
        /// </summary>
        public event EventHandler<Process>? ProcessUpdated;

        /// <summary>
        /// Processが追加されたときに発生するイベント
        /// </summary>
        public event EventHandler<Process>? ProcessAdded;

        /// <summary>
        /// プロセスフロー詳細を開くリクエストが発生したときのイベント
        /// </summary>
        public event EventHandler? OpenProcessFlowDetailRequested;

        #endregion

        #region Commands

        /// <summary>
        /// 新しいProcessを追加
        /// </summary>
        [RelayCommand]
        private async Task AddNewProcess()
        {
            if (CycleId == null)
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
                    SortNumber = Processes.Count > 0 ? Processes.Max(p => p.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository.AddProcessAsync(newProcess);
                newProcess.Id = newId;

                // コレクションに追加
                Processes.Add(newProcess);

                // イベントを発火
                ProcessAdded?.Invoke(this, newProcess);

                MessageBox.Show("新しい工程を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 選択されたProcessを削除
        /// </summary>
        [RelayCommand]
        private async Task DeleteSelectedProcess()
        {
            if (SelectedProcess == null) return;

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
                    await _repository.DeleteProcessAsync(SelectedProcess.Id);

                    // コレクションから削除
                    var deletedProcess = SelectedProcess;
                    Processes.Remove(SelectedProcess);

                    // イベントを発火
                    ProcessDeleted?.Invoke(this, deletedProcess);

                    MessageBox.Show("工程を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 選択されたProcessのプロパティを編集
        /// </summary>
        [RelayCommand]
        private void EditProcess()
        {
            if (SelectedProcess == null)
            {
                MessageBox.Show("編集する工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ProcessPropertiesWindowを開く
                var window = new ProcessPropertiesWindow(_repository, SelectedProcess)
                {
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    // プロセスの更新をUIに反映
                    var index = Processes.IndexOf(SelectedProcess);
                    if (index >= 0)
                    {
                        Processes[index] = SelectedProcess;
                        OnPropertyChanged(nameof(Processes));
                    }

                    // イベントを発火
                    ProcessUpdated?.Invoke(this, SelectedProcess);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の編集中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// プロセスフロー詳細を開く
        /// </summary>
        [RelayCommand]
        private void OpenProcessFlowDetail()
        {
            // イベントを発火してMainViewModelに処理を委譲
            OpenProcessFlowDetailRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Processリストを読み込む
        /// </summary>
        public async Task LoadProcessesAsync()
        {
            if (CycleId == null) return;

            try
            {
                var processes = await _repository.GetProcessesAsync();
                var cycleProcesses = processes
                    .Where(p => p.CycleId == CycleId.Value)
                    .OrderBy(p => p.SortNumber)
                    .ToList();

                Processes = new ObservableCollection<Process>(cycleProcesses);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Processカテゴリを読み込む
        /// </summary>
        public async Task LoadProcessCategoriesAsync()
        {
            try
            {
                var categories = await _repository.GetProcessCategoriesAsync();
                ProcessCategories = new ObservableCollection<ProcessCategory>(categories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"カテゴリの読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
