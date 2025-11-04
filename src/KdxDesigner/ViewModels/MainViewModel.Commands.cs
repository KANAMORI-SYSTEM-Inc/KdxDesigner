using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using KdxDesigner.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class MainViewModel
    {
        [RelayCommand]
        private async Task MemorySetting()
        {
            // メモリ設定状態を「設定中」に更新
            MemoryConfigurationStatus = "設定中...";
            IsMemoryConfigured = false;
            if (!ValidateMemorySettings()) return;

            // 進捗ウィンドウを作成
            var progressViewModel = new MemoryProgressViewModel();
            var progressWindow = new MemoryProgressWindow
            {
                DataContext = progressViewModel,
                Owner = Application.Current.MainWindow
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

                    // 3. データ準備
                    progressViewModel.UpdateStatus("データを準備しています...");
                    var prepData = await PrepareDataForMemorySetting();

                    // 4. Mnemonic/Timerテーブルへの事前保存
                    if (prepData == null)
                    {
                        // データ準備に失敗した場合、ユーザーに通知して処理を中断
                        progressViewModel.MarkError("データ準備に失敗しました");
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show("データ準備に失敗しました。CycleまたはPLCが選択されているか確認してください。", "エラー"));
                        return;
                    }

                    await SaveMnemonicAndTimerDevices(prepData.Value, progressViewModel);
                    await SaveMemoriesToMemoryTableAsync(prepData.Value, progressViewModel);

                    progressViewModel.MarkCompleted();
                }
                catch (Exception ex)
                {
                    progressViewModel.MarkError(ex.Message);
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"メモリ設定中にエラーが発生しました: {ex.Message}", "エラー"));
                }
            });
        }

        private bool ValidateMemorySettings()
        {
            var errorMessages = new List<string>();
            if (SelectedCycle == null) errorMessages.Add("Cycleが選択されていません。");
            if (SelectedPlc == null) errorMessages.Add("PLCが選択されていません。");

            if (errorMessages.Any())
            {
                MessageBox.Show(string.Join("\n", errorMessages), "入力エラー");
                return false;
            }
            return true;
        }

        [RelayCommand]
        public void OpenControllBoxView()
        {
            if (SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var window = new KdxDesigner.Views.ControlBoxViews(_repository, SelectedPlc.Id)
            {
                Owner = Application.Current.MainWindow // 親ウィンドウ設定
            };
            window.ShowDialog();
        }

        [RelayCommand]
        public void UpdateSelectedProcesses(List<Process> selectedProcesses)
        {
            var selectedIds = selectedProcesses.Select(p => p.Id).ToHashSet();
            var filtered = _selectionManager.GetAllDetails()
                .Where(d => selectedIds.Contains(d.ProcessId))
                .ToList();

            ProcessDetails = new ObservableCollection<ProcessDetail>(filtered);
        }

        [RelayCommand]
        private void OpenIoEditor()
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // Viewにリポジトリのインスタンスを渡して生成
            var view = new IoEditorView(_repository, this);
            view.Show(); // モードレスダイアログとして表示
        }

        [RelayCommand]
        private void OpenIOConversionWindow()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            var window = new IOConversionWindow();
            var viewModel = new IOConversionViewModel(_repository);
            window.DataContext = viewModel;
            window.ShowDialog(); // モーダルダイアログとして表示
        }

        [RelayCommand]
        private async Task AddNewProcess()
        {
            if (!CanExecute() || SelectedCycle == null)
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
                    CycleId = SelectedCycle.Id,
                    SortNumber = Processes.Count > 0 ? Processes.Max(p => p.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository!.AddProcessAsync(newProcess);
                newProcess.Id = newId;

                // コレクションとローカルリストに追加
                Processes.Add(newProcess);
                _selectionManager.AddProcessToCache(newProcess);

                MessageBox.Show("新しい工程を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task AddNewProcessDetail()
        {
            if (!CanExecute() || SelectedProcess == null)
            {
                MessageBox.Show("工程を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 新しいProcessDetailオブジェクトを作成
                var newDetail = new ProcessDetail
                {
                    ProcessId = SelectedProcess.Id,
                    DetailName = "新規詳細",
                    CycleId = SelectedCycle?.Id,
                    SortNumber = ProcessDetails.Count > 0 ? ProcessDetails.Max(d => d.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository!.AddProcessDetailAsync(newDetail);
                newDetail.Id = newId;

                // コレクションとローカルリストに追加
                ProcessDetails.Add(newDetail);
                _selectionManager.AddDetailToCache(newDetail);

                MessageBox.Show("新しい工程詳細を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程詳細の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// 工程プロパティを編集
        /// </summary>
        [RelayCommand]
        private void EditProcess()
        {
            if (!CanExecute() || SelectedProcess == null)
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"工程の編集中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task AddNewOperation()
        {
            if (!CanExecute() || SelectedCycle == null)
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
                    CycleId = SelectedCycle.Id,
                    SortNumber = SelectedOperations.Count > 0 ? SelectedOperations.Max(o => o.SortNumber ?? 0) + 1 : 1
                };

                // データベースに追加
                int newId = await _repository!.AddOperationAsync(newOperation);
                newOperation.Id = newId;

                // コレクションに追加
                SelectedOperations.Add(newOperation);

                MessageBox.Show("新しい操作を追加しました。", "追加完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenCylinderManagement()
        {
            if (!CanExecute() || SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var window = new CylinderManagementWindow(_repository!, SelectedPlc.Id)
                {
                    Owner = Application.Current.MainWindow
                };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シリンダー管理ウィンドウを開く際にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SaveAllChanges()
        {
            if (!CanExecute()) return;

            try
            {
                // Processの保存
                foreach (var process in Processes)
                {
                    _repository!.UpdateProcessAsync(process);
                }

                // ProcessDetailの保存
                foreach (var detail in ProcessDetails)
                {
                    _repository!.UpdateProcessDetailAsync(detail);
                }

                // Operationの保存
                foreach (var operation in SelectedOperations)
                {
                    _repository!.UpdateOperationAsync(operation);
                }

                MessageBox.Show("すべての変更を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteSelectedProcess()
        {
            if (SelectedProcess == null) return;

            var result = MessageBox.Show($"工程 '{SelectedProcess.ProcessName}' を削除しますか？\n関連する工程詳細も削除されます。",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository!.DeleteProcessAsync(SelectedProcess.Id);
                    Processes.Remove(SelectedProcess);
                    _selectionManager.RemoveProcessFromCache(SelectedProcess);

                    // 関連するProcessDetailも削除
                    var detailsToRemove = ProcessDetails.Where(d => d.ProcessId == SelectedProcess.Id).ToList();
                    foreach (var detail in detailsToRemove)
                    {
                        ProcessDetails.Remove(detail);
                        _selectionManager.RemoveDetailFromCache(detail);
                    }

                    SelectedProcess = null;
                    MessageBox.Show("工程を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteSelectedProcessDetail()
        {
            if (SelectedProcessDetail == null) return;

            var result = MessageBox.Show($"工程詳細 '{SelectedProcessDetail.DetailName}' を削除しますか？",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository!.DeleteProcessDetailAsync(SelectedProcessDetail.Id);
                    ProcessDetails.Remove(SelectedProcessDetail);
                    _selectionManager.RemoveDetailFromCache(SelectedProcessDetail);
                    SelectedProcessDetail = null;
                    MessageBox.Show("工程詳細を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteSelectedOperation()
        {
            if (SelectedOperations.Count == 0) return;

            var operation = SelectedOperations.FirstOrDefault();
            if (operation == null) return;

            var result = MessageBox.Show($"操作 '{operation.OperationName}' を削除しますか？",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository!.DeleteOperationAsync(operation.Id);
                    SelectedOperations.Remove(operation);
                    MessageBox.Show("操作を削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void SaveOperation()
        {
            if (!CanExecute()) return;

            foreach (var op in SelectedOperations)
            {
                _repository!.UpdateOperationAsync(op);
            }
            MessageBox.Show("保存しました。");
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var view = new SettingsView();
            view.ShowDialog();
        }

        [RelayCommand]
        private void OpenProcessFlowDetail()
        {
            if (SelectedCycle == null)
            {
                MessageBox.Show("サイクルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // MainViewModelで既に取得しているデータを渡してパフォーマンスを改善
            var allProcesses = _selectionManager.GetAllProcesses();
            var allProcessDetails = _selectionManager.GetAllDetails();
            var categories = ProcessDetailCategories.ToList();
            var plcId = SelectedPlc?.Id;

            // 新しいウィンドウを作成（既存データを渡す）
            var window = new ProcessFlowDetailWindow(
                _repository,
                SelectedCycle.Id,
                SelectedCycle.CycleName ?? $"サイクル{SelectedCycle.Id}",
                allProcesses,
                allProcessDetails,
                categories,
                plcId);

            // ウィンドウが閉じられたときにリストから削除
            window.Closed += (s, e) =>
            {
                if (s is Window w)
                {
                    _openProcessFlowWindows.Remove(w);
                }
            };

            // リストに追加して表示
            _openProcessFlowWindows.Add(window);
            window.Show();
        }

        [RelayCommand]
        private void CloseAllProcessFlowWindows()
        {
            // すべてのProcessFlowDetailWindowを閉じる
            var windowsToClose = _openProcessFlowWindows.ToList();
            foreach (var window in windowsToClose)
            {
                window.Close();
            }
            _openProcessFlowWindows.Clear();
        }

        [RelayCommand]
        private void OpenProcessListWindow()
        {
            if (SelectedCycle == null || _repository == null)
            {
                MessageBox.Show("サイクルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 動的にWindowを生成
            var window = new Window
            {
                Title = $"Process一覧 - Cycle ID: {SelectedCycle.Id}",
                Width = 1000,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var control = new Controls.ProcessListControl
            {
                Repository = _repository,
                Processes = Processes,
                ProcessCategories = ProcessCategories,
                CycleId = SelectedCycle.Id,
                PlcId = SelectedPlc?.Id
            };

            window.Content = control;
            window.Show();
        }

        [RelayCommand]
        private void OpenProcessDetailListWindow()
        {
            if (SelectedProcess == null || _repository == null)
            {
                MessageBox.Show("工程を選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 動的にWindowを生成
            var window = new Window
            {
                Title = $"ProcessDetail一覧 - Process ID: {SelectedProcess.Id}",
                Width = 1200,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var control = new Controls.ProcessDetailListControl
            {
                Repository = _repository,
                ProcessDetails = ProcessDetails,
                Operations = SelectedOperations,
                ProcessDetailCategories = ProcessDetailCategories,
                ProcessId = SelectedProcess.Id,
                CycleId = SelectedCycle?.Id
            };

            window.Content = control;
            window.Show();
        }

        [RelayCommand]
        private void OpenOperationListWindow()
        {
            if (SelectedCycle == null || _repository == null)
            {
                MessageBox.Show("サイクルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 動的にWindowを生成
            var window = new Window
            {
                Title = $"Operation一覧 - Cycle ID: {SelectedCycle.Id}",
                Width = 1000,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var control = new Controls.OperationListControl
            {
                Repository = _repository,
                Operations = SelectedOperations,
                OperationCategories = OperationCategories,
                PlcId = SelectedPlc?.Id,
                CycleId = SelectedCycle.Id
            };

            window.Content = control;
            window.Show();
        }

        [RelayCommand]
        private void OpenInterlockSettings()
        {
            // Supabaseリポジトリを取得


            Kdx.Infrastructure.Supabase.Repositories.SupabaseRepository? supabaseRepo = null;
            try
            {
                if (App.Services != null)
                {
                    var supabaseClient = App.Services.GetService<Supabase.Client>();
                    if (supabaseClient != null)
                    {
                        supabaseRepo = new Kdx.Infrastructure.Supabase.Repositories.SupabaseRepository(supabaseClient);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get SupabaseRepository: {ex.Message}");
                MessageBox.Show("Supabase接続が利用できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (supabaseRepo == null)
            {
                MessageBox.Show("Supabase接続が利用できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 選択されたサイクルを確認
            if (SelectedCycle == null || SelectedPlc == null)
            {
                MessageBox.Show("サイクルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // インターロック設定ウィンドウを表示（シリンダー選択を含む）
            var window = new InterlockSettingsWindow();
            var viewModel = new InterlockSettingsViewModel(supabaseRepo, _repository!, SelectedPlc.Id, SelectedCycle.Id, window);
            window.DataContext = viewModel;
            window.ShowDialog();
        }

        [RelayCommand]
        private void OpenMemoryEditor()
        {
            if (SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。");
                return;
            }

            var view = new MemoryEditorView(SelectedPlc.Id, _repository);
            view.ShowDialog();
        }

        [RelayCommand]
        private void OpenLinkDeviceManager()
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // Viewにリポジトリのインスタンスを渡す
            var view = new LinkDeviceView(_repository);
            view.ShowDialog(); // モーダルダイアログとして表示
        }

        [RelayCommand]
        private void OpenTimerEditor()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            var view = new TimerEditorView(_repository, this);
            view.ShowDialog();
        }

        [RelayCommand]
        private void OpenMemoryProfileManager()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }
            var view = new MemoryProfileView(this, _repository);
            view.ShowDialog();
        }

        // Authentication
        #region Authentication Commands

        [RelayCommand]
        private async Task SignOutAsync()
        {
            try
            {
                await _authService.SignOutAsync();

                // ログイン画面を表示
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var loginWindow = new Views.LoginView();
                    loginWindow.Show();

                    // メインウィンドウを閉じる
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is Views.MainView)
                        {
                            window.Close();
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"サインアウトに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

    }
}
