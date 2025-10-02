using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace KdxDesigner.Services.Authentication
{
    public interface IOAuthCallbackListener
    {
        Task<string?> ListenForCallbackAsync(int port = 3000, CancellationToken cancellationToken = default);
        Task<bool> StartListenerAsync(int port = 3000);
        Task<string?> WaitForCallbackAsync(CancellationToken cancellationToken = default);
        void StopListener();
    }

    public class OAuthCallbackListener : IOAuthCallbackListener
    {
        private HttpListener? _listener;
        private int _currentPort;

        public async Task<string?> ListenForCallbackAsync(int port = 3000, CancellationToken cancellationToken = default)
        {
            var url = $"http://localhost:{port}/";
            
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            
            // +を使用して全てのホスト名でリッスンする（管理者権限が必要）
            // または、localhostと127.0.0.1の両方を追加
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            
            try
            {
                try
                {
                    _listener.Start();
                    System.Diagnostics.Debug.WriteLine($"OAuth callback listener started successfully on {url}");
                    
                    // リスナーが正常に起動したことを確認するため、少し待機
                    await Task.Delay(100, cancellationToken);
                    
                    // リスナーが本当に動作しているかテスト
                    if (!_listener.IsListening)
                    {
                        throw new InvalidOperationException("HTTPリスナーが起動していません。");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Listener is confirmed to be listening on port {port}");
                }
                catch (HttpListenerException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start HTTP listener: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Error code: {ex.ErrorCode}");
                    
                    // ポートが使用中の場合
                    if (ex.ErrorCode == 48 || ex.ErrorCode == 183) // ERROR_ACCESS_DENIED or ERROR_ALREADY_EXISTS
                    {
                        throw new InvalidOperationException($"ポート {port} は既に使用されています。別のアプリケーションがポートを使用していないか確認してください。", ex);
                    }
                    // 権限不足の場合
                    else if (ex.ErrorCode == 5) // ERROR_ACCESS_DENIED
                    {
                        throw new InvalidOperationException($"ポート {port} でのリッスンに必要な権限がありません。管理者権限で実行するか、別のポートを使用してください。", ex);
                    }
                    
                    throw;
                }
                
                System.Diagnostics.Debug.WriteLine($"OAuth callback listener ready to receive requests on {url}");
                
                // 非同期でリクエストを待機
                var contextTask = _listener.GetContextAsync();
                
                // キャンセレーショントークンと組み合わせて待機
                using (cancellationToken.Register(() => _listener.Stop()))
                {
                    var context = await contextTask;
                    
                    // URLからクエリパラメータを取得
                    var query = context.Request.Url?.Query;
                    string? code = null;
                    
                    if (!string.IsNullOrEmpty(query))
                    {
                        var queryParams = HttpUtility.ParseQueryString(query);
                        code = queryParams["code"];
                        
                        // エラーがある場合はそれも確認
                        var error = queryParams["error"];
                        var errorCode = queryParams["error_code"];
                        var errorDescription = queryParams["error_description"];
                        
                        if (!string.IsNullOrEmpty(error))
                        {
                            System.Diagnostics.Debug.WriteLine($"OAuth error: {error}");
                            System.Diagnostics.Debug.WriteLine($"Error code: {errorCode}");
                            System.Diagnostics.Debug.WriteLine($"Error description: {errorDescription}");
                            
                            // エラーページを表示
                            await SendResponseAsync(context.Response, CreateErrorHtml(error, errorDescription, errorCode));
                            return null;
                        }
                    }
                    
                    // 成功レスポンスを送信
                    await SendResponseAsync(context.Response, CreateSuccessHtml());
                    
                    return code;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OAuth callback listener error: {ex.Message}");
                throw;
            }
            finally
            {
                _listener?.Stop();
                _listener?.Close();
            }
        }
        
        private async Task SendResponseAsync(HttpListenerResponse response, string html)
        {
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html; charset=UTF-8";
            
            using (var output = response.OutputStream)
            {
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        
        private string CreateSuccessHtml()
        {
            return @"
<!DOCTYPE html>
<html lang='ja'>
<head>
    <meta charset='UTF-8'>
    <title>認証成功 - KDX Designer</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            margin: 0;
            padding: 0;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
        }
        .container {
            background: white;
            border-radius: 10px;
            padding: 40px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 400px;
        }
        h1 {
            color: #4CAF50;
            margin-bottom: 20px;
        }
        p {
            color: #666;
            line-height: 1.6;
        }
        .checkmark {
            width: 80px;
            height: 80px;
            border-radius: 50%;
            display: block;
            stroke-width: 2;
            stroke: #4CAF50;
            stroke-miterlimit: 10;
            margin: 0 auto 20px;
            box-shadow: inset 0px 0px 0px #4CAF50;
            animation: fill .4s ease-in-out .4s forwards, scale .3s ease-in-out .9s both;
        }
        .checkmark__circle {
            stroke-dasharray: 166;
            stroke-dashoffset: 166;
            stroke-width: 2;
            stroke-miterlimit: 10;
            stroke: #4CAF50;
            fill: none;
            animation: stroke 0.6s cubic-bezier(0.65, 0, 0.45, 1) forwards;
        }
        .checkmark__check {
            transform-origin: 50% 50%;
            stroke-dasharray: 48;
            stroke-dashoffset: 48;
            animation: stroke 0.3s cubic-bezier(0.65, 0, 0.45, 1) 0.8s forwards;
        }
        @keyframes stroke {
            100% {
                stroke-dashoffset: 0;
            }
        }
        @keyframes scale {
            0%, 100% {
                transform: none;
            }
            50% {
                transform: scale3d(1.1, 1.1, 1);
            }
        }
        @keyframes fill {
            100% {
                box-shadow: inset 0px 0px 0px 30px #4CAF50;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
        <svg class='checkmark' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 52 52'>
            <circle class='checkmark__circle' cx='26' cy='26' r='25' fill='none'/>
            <path class='checkmark__check' fill='none' d='M14.1 27.2l7.1 7.2 16.7-16.8'/>
        </svg>
        <h1>認証成功！</h1>
        <p>GitHubアカウントでの認証が完了しました。</p>
        <p>このウィンドウは自動的に閉じられます。<br>KDX Designerアプリケーションに戻ってください。</p>
    </div>
    <script>
        setTimeout(function() {
            window.close();
        }, 3000);
    </script>
</body>
</html>";
        }
        
        private string CreateErrorHtml(string? error, string? errorDescription, string? errorCode = null)
        {
            var troubleshootingTips = GetTroubleshootingTips(error, errorCode);
            return $@"
<!DOCTYPE html>
<html lang='ja'>
<head>
    <meta charset='UTF-8'>
    <title>認証エラー - KDX Designer</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #f5576c 0%, #f093fb 100%);
            margin: 0;
            padding: 0;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
        }}
        .container {{
            background: white;
            border-radius: 10px;
            padding: 40px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 400px;
        }}
        h1 {{
            color: #f44336;
            margin-bottom: 20px;
        }}
        p {{
            color: #666;
            line-height: 1.6;
        }}
        .error-details {{
            background: #fff3e0;
            border-left: 4px solid #ff9800;
            padding: 10px;
            margin: 20px 0;
            text-align: left;
            border-radius: 4px;
        }}
        .error-icon {{
            width: 80px;
            height: 80px;
            margin: 0 auto 20px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <svg class='error-icon' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='#f44336' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
            <circle cx='12' cy='12' r='10'></circle>
            <line x1='15' y1='9' x2='9' y2='15'></line>
            <line x1='9' y1='9' x2='15' y2='15'></line>
        </svg>
        <h1>認証エラー</h1>
        <p>GitHubアカウントでの認証中にエラーが発生しました。</p>
        <div class='error-details'>
            <strong>エラー:</strong> {error ?? "不明なエラー"}<br>
            {(string.IsNullOrEmpty(errorCode) ? "" : $"<strong>エラーコード:</strong> {errorCode}<br>")}
            <strong>詳細:</strong> {errorDescription ?? "詳細情報はありません"}
        </div>
        {troubleshootingTips}
        <p>このウィンドウを閉じて、もう一度お試しください。</p>
    </div>
</body>
</html>";
        }
        
        private string GetTroubleshootingTips(string? error, string? errorCode)
        {
            if (error == "server_error" && errorCode == "unexpected_failure")
            {
                return @"
                <div class='error-details' style='background: #e3f2fd; border-left-color: #2196f3; margin-top: 20px;'>
                    <strong>解決方法:</strong><br>
                    1. <strong>GitHubのメール公開設定を確認</strong>: GitHub → Settings → Emails → 'Keep my email addresses private' のチェックを外す<br>
                    2. <strong>Supabase Client Secret再生成</strong>: GitHub OAuth App → Generate new client secret → Supabaseに貼り付け<br>
                    3. <strong>GitHub OAuth App設定確認</strong>: Callback URL = https://eebsrvkpcjsqmuvfqtve.supabase.co/auth/v1/callback<br>
                    4. <strong>一時的解決策</strong>: アプリの「開発モードでサインイン」ボタンを使用<br>
                    <br>
                    <strong style='color: #f44336;'>最も可能性の高い原因: GitHubアカウントのメールアドレスが非公開設定になっています</strong>
                </div>";
            }
            
            return "";
        }
        
        public async Task<bool> StartListenerAsync(int port = 3000)
        {
            try
            {
                _currentPort = port;
                var url = $"http://localhost:{port}/";
                
                _listener = new HttpListener();
                _listener.Prefixes.Add(url);
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                
                _listener.Start();
                System.Diagnostics.Debug.WriteLine($"OAuth callback listener started successfully on {url}");
                
                // リスナーが正常に起動したことを確認するため、少し待機
                await Task.Delay(200);
                
                // リスナーが本当に動作しているかテスト
                if (!_listener.IsListening)
                {
                    System.Diagnostics.Debug.WriteLine("HTTPリスナーが起動していません。");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"Listener is confirmed to be listening on port {port}");
                return true;
            }
            catch (HttpListenerException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start HTTP listener: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error code: {ex.ErrorCode}");
                
                // ポートが使用中の場合
                if (ex.ErrorCode == 48 || ex.ErrorCode == 183) // ERROR_ACCESS_DENIED or ERROR_ALREADY_EXISTS
                {
                    System.Diagnostics.Debug.WriteLine($"ポート {port} は既に使用されています。");
                }
                // 権限不足の場合
                else if (ex.ErrorCode == 5) // ERROR_ACCESS_DENIED
                {
                    System.Diagnostics.Debug.WriteLine($"ポート {port} でのリッスンに必要な権限がありません。");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error starting listener: {ex.Message}");
                return false;
            }
        }
        
        public async Task<string?> WaitForCallbackAsync(CancellationToken cancellationToken = default)
        {
            if (_listener == null || !_listener.IsListening)
            {
                System.Diagnostics.Debug.WriteLine("Listener is not started");
                return null;
            }
            
            try
            {
                System.Diagnostics.Debug.WriteLine("Waiting for OAuth callback...");
                
                // 非同期でリクエストを待機
                var contextTask = _listener.GetContextAsync();
                
                // キャンセレーショントークンと組み合わせて待機
                using (cancellationToken.Register(() => _listener?.Stop()))
                {
                    var context = await contextTask;
                    
                    // URLからクエリパラメータを取得
                    var query = context.Request.Url?.Query;
                    string? code = null;
                    
                    // 詳細な診断情報をログに出力
                    System.Diagnostics.Debug.WriteLine("=== OAuth Callback受信 ===");
                    System.Diagnostics.Debug.WriteLine($"Request URL: {context.Request.Url}");
                    System.Diagnostics.Debug.WriteLine($"Query: {query}");
                    System.Diagnostics.Debug.WriteLine($"User Agent: {context.Request.UserAgent}");
                    System.Diagnostics.Debug.WriteLine($"Referer: {context.Request.UrlReferrer}");
                    
                    if (!string.IsNullOrEmpty(query))
                    {
                        var queryParams = HttpUtility.ParseQueryString(query);
                        code = queryParams["code"];
                        
                        // エラーがある場合はそれも確認
                        var error = queryParams["error"];
                        var errorCode = queryParams["error_code"];
                        var errorDescription = queryParams["error_description"];
                        
                        System.Diagnostics.Debug.WriteLine($"Auth Code: {!string.IsNullOrEmpty(code)}");
                        System.Diagnostics.Debug.WriteLine($"Error: {error}");
                        System.Diagnostics.Debug.WriteLine($"Error Code: {errorCode}");
                        System.Diagnostics.Debug.WriteLine($"Error Description: {errorDescription}");
                        System.Diagnostics.Debug.WriteLine("========================");
                        
                        if (!string.IsNullOrEmpty(error))
                        {
                            // エラーページを表示
                            await SendResponseAsync(context.Response, CreateErrorHtml(error, errorDescription, errorCode));
                            return null;
                        }
                    }
                    
                    // 成功レスポンスを送信
                    await SendResponseAsync(context.Response, CreateSuccessHtml());
                    
                    return code;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OAuth callback listener error: {ex.Message}");
                return null;
            }
        }
        
        public void StopListener()
        {
            try
            {
                _listener?.Stop();
                _listener?.Close();
                _listener = null;
                System.Diagnostics.Debug.WriteLine("OAuth callback listener stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping listener: {ex.Message}");
            }
        }
    }
}