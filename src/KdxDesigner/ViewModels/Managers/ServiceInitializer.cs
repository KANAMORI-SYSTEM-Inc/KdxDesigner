using Kdx.Contracts.Interfaces;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Services.IOSelector;
using KdxDesigner.Services.MemonicTimerDevice;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.Services.MnemonicSpeedDevice;
using Microsoft.Extensions.DependencyInjection;
using ErrorService = KdxDesigner.Services.ErrorService.ErrorService;

namespace KdxDesigner.ViewModels.Managers
{
    /// <summary>
    /// サービス初期化を管理するクラス
    /// MainViewModelで使用する各種サービスの初期化を担当
    /// </summary>
    public class ServiceInitializer
    {
        private readonly ISupabaseRepository _repository;
        private readonly MainViewModel _mainViewModel;

        public IMnemonicDeviceService? MnemonicService { get; private set; }
        public IMnemonicTimerDeviceService? TimerService { get; private set; }
        public IProsTimeDeviceService? ProsTimeService { get; private set; }
        public IMnemonicSpeedDeviceService? SpeedService { get; private set; }
        public IMemoryService? MemoryService { get; private set; }
        public IMnemonicDeviceMemoryStore? MemoryStore { get; private set; }
        internal ErrorService? ErrorService { get; private set; }
        public WpfIOSelectorService? IOSelectorService { get; private set; }

        public ServiceInitializer(ISupabaseRepository repository, MainViewModel mainViewModel)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        /// <summary>
        /// 全サービスを初期化
        /// </summary>
        public void InitializeAll()
        {
            InitializeBasicServices();
            InitializeMemoryStore();
            InitializeMemoryOnlyServices();
        }

        /// <summary>
        /// 基本サービスの初期化
        /// </summary>
        private void InitializeBasicServices()
        {
            ProsTimeService = App.Services?.GetService<IProsTimeDeviceService>()
                ?? new Kdx.Infrastructure.Services.ProsTimeDeviceService(_repository);

            MemoryService = App.Services?.GetService<IMemoryService>()
                ?? new Kdx.Infrastructure.Services.MemoryService(_repository);

            IOSelectorService = new WpfIOSelectorService();
            ErrorService = new ErrorService(_repository);
        }

        /// <summary>
        /// メモリストアの初期化
        /// </summary>
        private void InitializeMemoryStore()
        {
            MemoryStore = App.Services?.GetService<IMnemonicDeviceMemoryStore>()
                ?? new MnemonicDeviceMemoryStore();
        }

        /// <summary>
        /// メモリオンリーモードのサービスを初期化
        /// </summary>
        private void InitializeMemoryOnlyServices()
        {
            if (MemoryStore == null || MemoryService == null)
                throw new InvalidOperationException("MemoryStore and MemoryService must be initialized first");

            // MnemonicDeviceハイブリッドサービス
            var hybridService = new MnemonicDeviceHybridService(_repository, MemoryService, MemoryStore);
            hybridService.SetMemoryOnlyMode(true);
            MnemonicService = hybridService;

            // TimerDeviceアダプター
            var timerAdapter = new MnemonicTimerDeviceMemoryAdapter(_repository, _mainViewModel, MemoryStore, MemoryService);
            timerAdapter.SetMemoryOnlyMode(true);
            TimerService = timerAdapter;

            // SpeedDeviceアダプター
            var speedAdapter = new MnemonicSpeedDeviceMemoryAdapter(_repository, MemoryStore);
            speedAdapter.SetMemoryOnlyMode(true);
            SpeedService = speedAdapter;
        }

        /// <summary>
        /// サービスがすべて初期化されているか確認
        /// </summary>
        public bool AreServicesInitialized()
        {
            return MnemonicService != null
                && TimerService != null
                && ProsTimeService != null
                && SpeedService != null
                && MemoryService != null
                && MemoryStore != null
                && ErrorService != null
                && IOSelectorService != null;
        }
    }
}
