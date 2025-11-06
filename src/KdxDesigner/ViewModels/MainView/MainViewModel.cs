// ViewModel: PlcSelectionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Services;
using KdxDesigner.Services.Authentication;
using KdxDesigner.Utils;
using KdxDesigner.ViewModels.Managers;
using System.Diagnostics;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        /// <summary>
        /// DIコンテナ用コンストラクタ（推奨）
        /// </summary>
        public MainViewModel(ISupabaseRepository repository, IAuthenticationService authService, SupabaseConnectionHelper? supabaseHelper = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _supabaseHelper = supabaseHelper;

            Initialize();
        }


        /// <summary>
        /// 共通初期化処理
        /// </summary>
        private async void Initialize()
        {
            await InitializeManagers();
            _ = LoadInitialDataAsync();

            if (_supabaseHelper != null)
            {
                _ = InitializeSupabaseAsync();
            }
        }

        /// <summary>
        /// マネージャークラスの初期化
        /// </summary>
        private async Task InitializeManagers()
        {
            // SelectionStateManager の初期化
            _selectionManager = new SelectionStateManager(_repository);

            // SelectionStateManagerのプロパティ変更をMainViewModelに転送
            _selectionManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    OnPropertyChanged(e.PropertyName);

                    // 特定のプロパティ変更時に追加処理を実行
                    switch (e.PropertyName)
                    {
                        case nameof(SelectedCompany):
                            if (_selectionManager.SelectedCompany != null)
                            {
                                SettingsManager.Settings.LastSelectedCompanyId = _selectionManager.SelectedCompany.Id;
                                SettingsManager.Save();
                            }
                            break;
                        case nameof(SelectedModel):
                            if (_selectionManager.SelectedModel != null)
                            {
                                SettingsManager.Settings.LastSelectedModelId = _selectionManager.SelectedModel.Id;
                                SettingsManager.Save();
                            }
                            break;
                        case nameof(SelectedPlc):
                            // メモリ設定状態の更新は今後MemorySettingViewModelで行われます
                            break;
                        case nameof(SelectedCycle):
                            if (_selectionManager.SelectedCycle != null)
                            {
                                SettingsManager.Settings.LastSelectedCycleId = _selectionManager.SelectedCycle.Id;
                                SettingsManager.Save();
                            }
                            // メモリ設定状態の更新は今後MemorySettingViewModelで行われます
                            break;
                    }
                }
            };

            // ServiceInitializer の初期化とサービス生成
            _serviceInitializer = new ServiceInitializer(_repository, this);
            _serviceInitializer.InitializeAll();

            // DeviceConfigurationManager の初期化
            _deviceConfig = new DeviceConfigurationManager();

            // DeviceConfigurationManagerのプロパティ変更をMainViewModelに転送
            _deviceConfig.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    OnPropertyChanged(e.PropertyName);
                }
            };

            // MemoryConfigurationManager の初期化
            if (_serviceInitializer.MemoryStore != null)
            {
                _memoryConfig = new MemoryConfigurationManager(_serviceInitializer.MemoryStore);

                // MemoryConfigurationManagerのプロパティ変更をMainViewModelに転送
                _memoryConfig.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName != null)
                    {
                        OnPropertyChanged(e.PropertyName);
                    }
                };
            }
            else
            {
                throw new InvalidOperationException("MemoryStore が初期化されていません");
            }
        }

        private async Task InitializeSupabaseAsync()
        {
            try
            {
                if (_supabaseHelper == null) return;

                Debug.WriteLine("Starting Supabase initialization from MainViewModel...");
                var success = await _supabaseHelper.InitializeAsync();

                if (success)
                {
                    Debug.WriteLine("Supabase initialized successfully from MainViewModel");
                    // 接続テストを実行
                    var testResult = await _supabaseHelper.TestConnectionAsync();
                    Debug.WriteLine($"Supabase connection test result: {testResult}");
                }
                else
                {
                    Debug.WriteLine("Failed to initialize Supabase from MainViewModel");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Supabase from MainViewModel: {ex.Message}");
            }
        }


        private bool CanExecute()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 初期データの読み込み
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            if (!CanExecute() || _ioSelectorService == null)
            {
                Debug.WriteLine("システムの初期化が不完全なため、初期データの読み込みをスキップします。");
                return;
            }

            try
            {
                // 現在のユーザー情報を設定
                if (_authService?.CurrentSession != null)
                {
                    CurrentUserEmail = _authService.CurrentSession.User?.Email ?? "Unknown User";
                }

                // 初期データの読み込み
                await LoadMasterDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初期データの読み込み中にエラーが発生しました: {ex.Message}");
                MessageBox.Show($"データの読み込みに失敗しました: {ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// マスターデータの読み込み
        /// </summary>
        private async Task LoadMasterDataAsync()
        {
            // SelectionStateManagerを使ってマスターデータを読み込む
            await _selectionManager.LoadMasterDataAsync();

            // 設定とプロファイルの読み込み
            LoadSettings();

            // 前回の選択状態を復元
            RestoreLastSelection();
        }

        /// <summary>
        /// 設定の読み込み
        /// </summary>
        private void LoadSettings()
        {
            SettingsManager.Load();
        }

        /// <summary>
        /// 前回の選択状態を復元
        /// </summary>
        private void RestoreLastSelection()
        {
            _selectionManager.RestoreSelection(
                SettingsManager.Settings.LastSelectedCompanyId,
                SettingsManager.Settings.LastSelectedModelId,
                SettingsManager.Settings.LastSelectedCycleId);
        }

        public void OnProcessDetailSelected(ProcessDetail selected)
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // ProcessDetailが選択されてもSelectedOperationsは変更しない
            // SelectedOperationsはCycleの全Operationを保持する
            if (selected?.OperationId != null)
            {
                // 必要に応じて選択されたOperationの詳細を別途処理
                // ただしSelectedOperationsコレクションは変更しない
            }
        }

    }
}
