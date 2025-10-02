using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace KdxDesigner.Services.Authentication
{
    public interface ISessionStorageService
    {
        Task SaveSessionAsync(string accessToken, string refreshToken, long expiresIn, string? userEmail = null);
        Task<SessionData?> LoadSessionAsync();
        Task ClearSessionAsync();
    }

    public class SessionData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? UserEmail { get; set; }
    }

    public class SessionStorageService : ISessionStorageService
    {
        private readonly string _sessionFilePath;
        private readonly byte[] _entropy;

        public SessionStorageService()
        {
            // セッションファイルをユーザーのAppDataフォルダに保存
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "KdxDesigner");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            _sessionFilePath = Path.Combine(appFolder, "session.dat");
            
            // エントロピー（追加の暗号化キー）を生成
            _entropy = Encoding.UTF8.GetBytes("KdxDesigner_Session_2024");
        }

        public async Task SaveSessionAsync(string accessToken, string refreshToken, long expiresIn, string? userEmail = null)
        {
            try
            {
                var sessionData = new SessionData
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
                    UserEmail = userEmail
                };

                var json = JsonSerializer.Serialize(sessionData);
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                
                // Windows DPAPIを使用して暗号化
                var encryptedData = ProtectedData.Protect(jsonBytes, _entropy, DataProtectionScope.CurrentUser);
                
                await File.WriteAllBytesAsync(_sessionFilePath, encryptedData);
                
                System.Diagnostics.Debug.WriteLine($"Session saved successfully to {_sessionFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public async Task<SessionData?> LoadSessionAsync()
        {
            try
            {
                if (!File.Exists(_sessionFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("No saved session file found");
                    return null;
                }

                var encryptedData = await File.ReadAllBytesAsync(_sessionFilePath);
                
                // 復号化
                var decryptedData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decryptedData);
                
                var sessionData = JsonSerializer.Deserialize<SessionData>(json);
                
                // セッションの有効期限をチェック
                if (sessionData != null && sessionData.ExpiresAt > DateTime.UtcNow)
                {
                    System.Diagnostics.Debug.WriteLine($"Session loaded successfully. Expires at: {sessionData.ExpiresAt}");
                    return sessionData;
                }
                
                System.Diagnostics.Debug.WriteLine("Session has expired");
                await ClearSessionAsync();
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
                return null;
            }
        }

        public async Task ClearSessionAsync()
        {
            try
            {
                if (File.Exists(_sessionFilePath))
                {
                    await Task.Run(() => File.Delete(_sessionFilePath));
                    System.Diagnostics.Debug.WriteLine("Session cleared successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear session: {ex.Message}");
            }
        }
    }
}