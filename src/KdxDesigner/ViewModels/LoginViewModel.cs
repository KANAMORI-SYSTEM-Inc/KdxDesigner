using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KdxDesigner.Services.Authentication;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthenticationService _authService;
        private readonly IOAuthCallbackListener _callbackListener;
        private bool _mainWindowOpened = false;
        private readonly object _mainWindowLock = new object();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        public LoginViewModel(IAuthenticationService authService, IOAuthCallbackListener callbackListener)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _callbackListener = callbackListener ?? throw new ArgumentNullException(nameof(callbackListener));
            
            // 認証状態変更イベントをリッスン
            _authService.AuthStateChanged += OnAuthStateChanged;
            
            // 既存のセッションをチェック（少し遅延させて復元処理を待つ）
            Task.Run(async () =>
            {
                await Task.Delay(500); // セッション復元を待つ
                await CheckExistingSession();
            });
        }

        private void OnAuthStateChanged(object? sender, Supabase.Gotrue.Session? session)
        {
            if (session != null && !_mainWindowOpened)
            {
                System.Diagnostics.Debug.WriteLine("OnAuthStateChanged: Session detected, opening MainWindow...");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "自動ログインに成功しました";
                    OpenMainWindow();
                });
            }
        }

        private async Task CheckExistingSession()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = true;
                    StatusMessage = "保存されたセッションを確認しています...";
                });

                var session = await _authService.GetSessionAsync();
                if (session != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = "自動ログインに成功しました";
                        // 既にサインイン済みの場合、メインウィンドウを開く
                        OpenMainWindow();
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = "";
                        IsLoading = false;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Session check failed: {ex.Message}");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "";
                    IsLoading = false;
                });
            }
        }

        [RelayCommand]
        private async Task SignInWithGitHubAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = "GitHub認証を開始しています...";

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting GitHub sign-in process...");
                
                // ローカルHTTPリスナーを起動してコールバックを待機
                var cancellationTokenSource = new CancellationTokenSource();
                StatusMessage = "認証リスナーを起動しています...";
                
                // まず、リスナーを起動
                var listenerStarted = await _callbackListener.StartListenerAsync(3000);
                if (!listenerStarted)
                {
                    ErrorMessage = "リスナーが正常に起動していません。ポートが使用中か権限不足の可能性があります。";
                    System.Diagnostics.Debug.WriteLine("Listener failed to start");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("OAuth callback listener verified and ready");
                
                // GitHub認証URLを生成してブラウザで開く
                StatusMessage = "GitHub認証ページを開いています...";
                var authUrl = await _authService.SignInWithGitHubAsync();
                
                if (!string.IsNullOrEmpty(authUrl))
                {
                    StatusMessage = "ブラウザでGitHub認証を完了してください...";
                    System.Diagnostics.Debug.WriteLine($"Auth URL generated: {authUrl}");
                    
                    // コールバックを待機（最大60秒）
                    cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(60));
                    
                    try
                    {
                        var code = await _callbackListener.WaitForCallbackAsync(cancellationTokenSource.Token);
                        System.Diagnostics.Debug.WriteLine($"Received auth code: {!string.IsNullOrEmpty(code)}");
                        
                        if (!string.IsNullOrEmpty(code))
                        {
                            StatusMessage = "認証コードを処理しています...";
                            
                            // 認証コードをセッションに交換
                            var session = await _authService.ExchangeCodeForSessionAsync(code);
                            
                            if (session != null)
                            {
                                StatusMessage = "ログイン成功！";
                                System.Diagnostics.Debug.WriteLine("Login successful!");
                                await Task.Delay(500); // 成功メッセージを表示
                                OpenMainWindow();
                            }
                            else
                            {
                                ErrorMessage = "セッションの取得に失敗しました。";
                                System.Diagnostics.Debug.WriteLine("Failed to get session");
                            }
                        }
                        else
                        {
                            ErrorMessage = "認証がキャンセルされました。";
                            System.Diagnostics.Debug.WriteLine("Authentication cancelled - no code received");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        ErrorMessage = "認証がタイムアウトしました。もう一度お試しください。";
                        System.Diagnostics.Debug.WriteLine("Authentication timeout");
                    }
                    catch (Exception innerEx)
                    {
                        ErrorMessage = $"コールバック処理エラー: {innerEx.Message}";
                        System.Diagnostics.Debug.WriteLine($"Callback error: {innerEx}");
                    }
                }
                else
                {
                    ErrorMessage = "GitHub認証URLの生成に失敗しました。Supabaseの設定を確認してください。";
                    System.Diagnostics.Debug.WriteLine("Failed to generate auth URL");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"エラー: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"SignIn error: {ex}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
                
                // リスナーを確実に停止
                _callbackListener.StopListener();
            }
        }

        private void OpenMainWindow()
        {
            lock (_mainWindowLock)
            {
                if (_mainWindowOpened)
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow already opened, skipping...");
                    return;
                }
                _mainWindowOpened = true;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // 既存のMainViewがないか確認
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.MainView)
                    {
                        System.Diagnostics.Debug.WriteLine("MainView already exists, activating it...");
                        window.Activate();
                        CloseLoginWindow();
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine("Creating new MainView...");
                var mainWindow = new Views.MainView();
                mainWindow.Show();
                
                CloseLoginWindow();
            });
        }

        private void CloseLoginWindow()
        {
            // ログインウィンドウを閉じる
            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.LoginView)
                {
                    window.Close();
                    break;
                }
            }
        }
        
        [RelayCommand]
        private async Task SignInWithEmailAsync(System.Windows.Controls.PasswordBox passwordBox)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "メールアドレスを入力してください。";
                return;
            }

            if (passwordBox == null || string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                ErrorMessage = "パスワードを入力してください。";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = "サインインしています...";

            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting email sign in for: {Email}");
                
                var success = await _authService.SignInWithEmailAsync(Email, passwordBox.Password);
                
                if (success)
                {
                    StatusMessage = "ログイン成功！";
                    System.Diagnostics.Debug.WriteLine("Email sign in successful");
                    await Task.Delay(500);
                    OpenMainWindow();
                }
                else
                {
                    ErrorMessage = "メールアドレスまたはパスワードが間違っています。";
                    System.Diagnostics.Debug.WriteLine("Email sign in failed - invalid credentials");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"サインインエラー: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Email sign in error: {ex}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        
        [RelayCommand]
        private async Task SignUpWithEmailAsync(System.Windows.Controls.PasswordBox passwordBox)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "メールアドレスを入力してください。";
                return;
            }

            if (passwordBox == null || string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                ErrorMessage = "パスワードを入力してください。";
                return;
            }

            // パスワードの最小長チェック
            if (passwordBox.Password.Length < 6)
            {
                ErrorMessage = "パスワードは6文字以上で入力してください。";
                return;
            }

            // TODO: 確認用パスワードの検証は後で実装
            // 現在はパスワードの基本検証のみ実装

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = "新規登録しています...";

            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting email sign up for: {Email}");
                
                var success = await _authService.SignUpWithEmailAsync(Email, passwordBox.Password);
                
                if (success)
                {
                    StatusMessage = "新規登録成功！";
                    System.Diagnostics.Debug.WriteLine("Email sign up successful");
                    await Task.Delay(500);
                    OpenMainWindow();
                }
                else
                {
                    ErrorMessage = "新規登録に失敗しました。入力内容を確認してください。";
                    System.Diagnostics.Debug.WriteLine("Email sign up failed - unknown reason");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"新規登録エラー: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Email sign up error: {ex}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
    }
}