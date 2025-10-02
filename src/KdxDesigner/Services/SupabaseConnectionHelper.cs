using System.Diagnostics;
using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;

namespace KdxDesigner.Services
{
    public class SupabaseConnectionHelper
    {
        private readonly Supabase.Client _client;
        private readonly ISupabaseRepository _repository;
        private bool _isInitialized = false;
        private readonly object _lockObject = new();

        public SupabaseConnectionHelper(Supabase.Client client, ISupabaseRepository repository)
        {
            _client = client;
            _repository = repository;
        }

        public async Task<bool> InitializeAsync()
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    Debug.WriteLine("Supabase client already initialized");
                    return true;
                }
            }

            try
            {
                Debug.WriteLine("Initializing Supabase client...");
                await _client.InitializeAsync();

                lock (_lockObject)
                {
                    _isInitialized = true;
                }

                Debug.WriteLine("Supabase client initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize Supabase client: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public bool IsInitialized
        {
            get
            {
                lock (_lockObject)
                {
                    return _isInitialized;
                }
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (!IsInitialized)
                {
                    var result = await InitializeAsync();
                    if (!result)
                    {
                        return false;
                    }
                }

                // 簡単なクエリでテスト（CompanyテーブルからLIMIT 1）
                var companies = await _repository.GetCompaniesAsync();

                Debug.WriteLine("Connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }
    }
}
