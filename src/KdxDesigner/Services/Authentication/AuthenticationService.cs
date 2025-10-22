using Supabase.Gotrue;
using SupabaseClient = Supabase.Client;

namespace KdxDesigner.Services.Authentication
{
    public interface IAuthenticationService
    {
        event EventHandler<Session?> AuthStateChanged;
        Session? CurrentSession { get; }
        bool IsAuthenticated { get; }
        Task<string?> SignInWithGitHubAsync();
        Task SignOutAsync();
        Task<Session?> GetSessionAsync();
        Task<Session?> ExchangeCodeForSessionAsync(string code);
        Task<bool> SignInWithEmailAsync(string email, string password);
        Task<bool> SignUpWithEmailAsync(string email, string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly SupabaseClient _supabaseClient;
        private readonly ISessionStorageService _sessionStorage;
        private Session? _currentSession;

        public event EventHandler<Session?> AuthStateChanged = delegate { };

        public Session? CurrentSession => _currentSession;
        public bool IsAuthenticated => _currentSession != null;

        public AuthenticationService(SupabaseClient supabaseClient, ISessionStorageService sessionStorage)
        {
            _supabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
            _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));

            System.Diagnostics.Debug.WriteLine($"AuthenticationService initialized");

            // 認証状態の変更を監視
            _supabaseClient.Auth.AddStateChangedListener((sender, changed) =>
            {
                System.Diagnostics.Debug.WriteLine($"Auth state changed: {changed}");
                if (changed == Constants.AuthState.SignedIn ||
                    changed == Constants.AuthState.SignedOut ||
                    changed == Constants.AuthState.TokenRefreshed)
                {
                    _currentSession = _supabaseClient.Auth.CurrentSession;
                    AuthStateChanged?.Invoke(this, _currentSession);

                    // セッションを保存
                    if (_currentSession != null && changed == Constants.AuthState.SignedIn)
                    {
                        Task.Run(async () => await SaveSessionAsync(_currentSession));
                    }
                    else if (changed == Constants.AuthState.SignedOut)
                    {
                        Task.Run(async () => await _sessionStorage.ClearSessionAsync());
                    }
                }
            });

            // 初期セッションを取得
            _currentSession = _supabaseClient.Auth.CurrentSession;
            System.Diagnostics.Debug.WriteLine($"Initial session: {(_currentSession != null ? "Exists" : "None")}");

            // 保存されたセッションを自動的に復元
            Task.Run(async () => await TryRestoreSessionAsync());
        }

        public async Task<string?> SignInWithGitHubAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting GitHub OAuth flow...");

                // GitHub OAuthのURLを生成
                var providerAuthState = await _supabaseClient.Auth.SignIn(
                    Constants.Provider.Github,
                    new SignInOptions
                    {
                        // リダイレクトURLを設定（ローカルホスト）
                        RedirectTo = "http://localhost:3000/"
                    });

                // ProviderAuthStateからURIを取得
                var authUrl = providerAuthState?.Uri?.ToString();

                System.Diagnostics.Debug.WriteLine($"GitHub OAuth URL generated: {authUrl}");

                // ブラウザでGitHub認証ページを開く
                if (!string.IsNullOrEmpty(authUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"Opening browser with URL: {authUrl}");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = authUrl,
                        UseShellExecute = true
                    });
                    return authUrl;
                }

                System.Diagnostics.Debug.WriteLine("No OAuth URL was generated");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GitHub sign in failed: {ex}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"GitHubサインインに失敗しました: {ex.Message}", ex);
            }
        }

        public async Task<Session?> ExchangeCodeForSessionAsync(string code)
        {
            try
            {
                // 認証コードをセッションに交換
                // PKCEは使用しないのでcodeVerifierは空文字列
                var session = await _supabaseClient.Auth.ExchangeCodeForSession(code, string.Empty);
                _currentSession = session;

                // セッションを保存
                if (session != null)
                {
                    await SaveSessionAsync(session);
                }

                System.Diagnostics.Debug.WriteLine($"Session exchanged successfully");
                return session;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Code exchange failed: {ex.Message}");
                throw new InvalidOperationException($"認証コードの交換に失敗しました: {ex.Message}", ex);
            }
        }

        public async Task SignOutAsync()
        {
            try
            {
                await _supabaseClient.Auth.SignOut();
                _currentSession = null;

                // セッションをクリア
                await _sessionStorage.ClearSessionAsync();

                System.Diagnostics.Debug.WriteLine("Sign out successful");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign out failed: {ex.Message}");
                throw new InvalidOperationException($"サインアウトに失敗しました: {ex.Message}", ex);
            }
        }

        public async Task<Session?> GetSessionAsync()
        {
            try
            {
                _currentSession = await _supabaseClient.Auth.RetrieveSessionAsync();
                return _currentSession;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve session: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> TryRestoreSessionAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Attempting to restore saved session...");

                // カスタムストレージからセッションを読み込み
                var sessionData = await _sessionStorage.LoadSessionAsync();

                if (sessionData != null)
                {
                    // Supabaseにセッションを設定
                    var session = await _supabaseClient.Auth.SetSession(sessionData.AccessToken, sessionData.RefreshToken);

                    if (session != null)
                    {
                        _currentSession = session;
                        System.Diagnostics.Debug.WriteLine("Session restored successfully");
                        System.Diagnostics.Debug.WriteLine($"User email: {sessionData.UserEmail}");

                        // セッション復元を通知
                        AuthStateChanged?.Invoke(this, _currentSession);
                        return true;
                    }
                }

                System.Diagnostics.Debug.WriteLine("No saved session found");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore session: {ex.Message}");
                return false;
            }
        }

        private async Task SaveSessionAsync(Session session)
        {
            try
            {
                if (session?.AccessToken != null && session?.RefreshToken != null)
                {
                    var expiresIn = session.ExpiresIn > 0 ? session.ExpiresIn : 3600; // デフォルト1時間
                    await _sessionStorage.SaveSessionAsync(
                        session.AccessToken,
                        session.RefreshToken,
                        expiresIn,
                        session.User?.Email
                    );
                    System.Diagnostics.Debug.WriteLine("Session saved to storage");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public async Task<bool> SignInWithEmailAsync(string email, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting email sign in for: {email}");

                var session = await _supabaseClient.Auth.SignIn(email, password);

                if (session != null)
                {
                    _currentSession = session;
                    System.Diagnostics.Debug.WriteLine("Email sign in successful");

                    // セッションを保存
                    await SaveSessionAsync(session);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email sign in failed: {ex.Message}");
                throw new InvalidOperationException($"メールサインインに失敗しました: {ex.Message}", ex);
            }
        }

        public async Task<bool> SignUpWithEmailAsync(string email, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting email sign up for: {email}");

                var session = await _supabaseClient.Auth.SignUp(email, password);

                if (session != null)
                {
                    _currentSession = session;
                    System.Diagnostics.Debug.WriteLine("Email sign up successful");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine("Sign up failed - no session returned");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email sign up failed: {ex.Message}");
                throw new InvalidOperationException($"メール新規登録に失敗しました: {ex.Message}", ex);
            }
        }
    }
}