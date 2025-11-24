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
        // その他ボタン処理




        // 出力処理
        /// <summary>
        /// 出力ウィンドウを表示
        /// </summary>
        [RelayCommand]
        private void ProcessOutput()
        {
            var outputWindow = new OutputWindow(this)
            {
                Owner = Application.Current.MainWindow
            };
            outputWindow.ShowDialog();
        }

        /// <summary>
        /// メモリ設定ウィンドウを開く
        /// </summary>
        [RelayCommand]
        private void MemorySetting()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // MemorySettingWindowを開く
            var window = new MemorySettingWindow(_repository, this)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
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
