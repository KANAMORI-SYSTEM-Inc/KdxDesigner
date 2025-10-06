using CommunityToolkit.Mvvm.ComponentModel;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;
using Process = Kdx.Contracts.DTOs.Process;

namespace KdxDesigner.ViewModels.Managers
{
    /// <summary>
    /// 選択状態を管理するマネージャークラス
    /// Company, Model, PLC, Cycle, Process, ProcessDetailの選択状態を一元管理
    /// </summary>
    public partial class SelectionStateManager : ObservableObject
    {
        private readonly ISupabaseRepository _repository;

        // コレクション
        [ObservableProperty] private ObservableCollection<Company> _companies = new();
        [ObservableProperty] private ObservableCollection<Model> _models = new();
        [ObservableProperty] private ObservableCollection<PLC> _plcs = new();
        [ObservableProperty] private ObservableCollection<Cycle> _cycles = new();
        [ObservableProperty] private ObservableCollection<Process> _processes = new();
        [ObservableProperty] private ObservableCollection<ProcessDetail> _processDetails = new();
        [ObservableProperty] private ObservableCollection<Operation> _selectedOperations = new();

        // カテゴリ
        [ObservableProperty] private ObservableCollection<ProcessCategory> _processCategories = new();
        [ObservableProperty] private ObservableCollection<ProcessDetailCategory> _processDetailCategories = new();
        [ObservableProperty] private ObservableCollection<OperationCategory> _operationCategories = new();

        // 選択項目
        private Company? _selectedCompany;
        private Model? _selectedModel;
        private PLC? _selectedPlc;
        private Cycle? _selectedCycle;
        [ObservableProperty] private Process? _selectedProcess;
        [ObservableProperty] private ProcessDetail? _selectedProcessDetail;

        public Company? SelectedCompany
        {
            get => _selectedCompany;
            set
            {
                if (SetProperty(ref _selectedCompany, value) && value != null)
                {
                    _ = OnCompanySelectedAsync(value);
                }
            }
        }

        public Model? SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (SetProperty(ref _selectedModel, value) && value != null)
                {
                    _ = OnModelSelectedAsync(value);
                }
            }
        }

        public PLC? SelectedPlc
        {
            get => _selectedPlc;
            set
            {
                if (SetProperty(ref _selectedPlc, value) && value != null)
                {
                    _ = OnPlcSelectedAsync(value);
                }
            }
        }

        public Cycle? SelectedCycle
        {
            get => _selectedCycle;
            set
            {
                if (SetProperty(ref _selectedCycle, value) && value != null)
                {
                    _ = OnCycleSelectedAsync(value);
                }
            }
        }

        // キャッシュデータ
        private List<ProcessDetail> _allDetails = new();
        private List<Process> _allProcesses = new();

        public SelectionStateManager(ISupabaseRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// マスターデータを読み込む
        /// </summary>
        public async Task LoadMasterDataAsync()
        {
            Companies = new ObservableCollection<Company>(await _repository.GetCompaniesAsync());
            _allProcesses = await _repository.GetProcessesAsync();
            _allDetails = await _repository.GetProcessDetailsAsync();

            ProcessCategories = new ObservableCollection<ProcessCategory>(await _repository.GetProcessCategoriesAsync());
            ProcessDetailCategories = new ObservableCollection<ProcessDetailCategory>(await _repository.GetProcessDetailCategoriesAsync());
            OperationCategories = new ObservableCollection<OperationCategory>(await _repository.GetOperationCategoriesAsync());
        }

        /// <summary>
        /// 会社が選択されたときの処理
        /// </summary>
        public async Task OnCompanySelectedAsync(Company company)
        {
            Models = new ObservableCollection<Model>(
                (await _repository.GetModelsAsync()).Where(m => m.CompanyId == company.Id));
            SelectedModel = null;
        }

        /// <summary>
        /// モデルが選択されたときの処理
        /// </summary>
        public async Task OnModelSelectedAsync(Model model)
        {
            Plcs = new ObservableCollection<PLC>(
                (await _repository.GetPLCsAsync()).Where(p => p.ModelId == model.Id));
            SelectedPlc = null;
        }

        /// <summary>
        /// PLCが選択されたときの処理
        /// </summary>
        public async Task OnPlcSelectedAsync(PLC plc)
        {
            Cycles = new ObservableCollection<Cycle>(
                (await _repository.GetCyclesAsync()).Where(c => c.PlcId == plc.Id));
            SelectedCycle = null;
        }

        /// <summary>
        /// サイクルが選択されたときの処理
        /// </summary>
        public async Task OnCycleSelectedAsync(Cycle cycle)
        {
            Processes = new ObservableCollection<Process>(
                _allProcesses.Where(p => p.CycleId == cycle.Id).OrderBy(p => p.SortNumber));

            var operations = await _repository.GetOperationsByCycleIdAsync(cycle.Id);
            SelectedOperations = new ObservableCollection<Operation>(operations.OrderBy(o => o.SortNumber));
        }

        /// <summary>
        /// 前回の選択状態を復元
        /// </summary>
        public void RestoreSelection(int? companyId, int? modelId, int? cycleId)
        {
            if (!companyId.HasValue) return;

            var savedCompany = Companies.FirstOrDefault(c => c.Id == companyId.Value);
            if (savedCompany == null) return;

            SelectedCompany = savedCompany;

            if (modelId.HasValue)
            {
                var savedModel = Models.FirstOrDefault(m => m.Id == modelId.Value);
                if (savedModel != null)
                {
                    SelectedModel = savedModel;

                    if (cycleId.HasValue)
                    {
                        var savedCycle = Cycles.FirstOrDefault(c => c.Id == cycleId.Value);
                        if (savedCycle != null)
                        {
                            SelectedCycle = savedCycle;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 全プロセスを取得
        /// </summary>
        public List<Process> GetAllProcesses() => _allProcesses;

        /// <summary>
        /// 全詳細を取得
        /// </summary>
        public List<ProcessDetail> GetAllDetails() => _allDetails;

        /// <summary>
        /// プロセスをキャッシュに追加
        /// </summary>
        public void AddProcessToCache(Process process)
        {
            _allProcesses.Add(process);
        }

        /// <summary>
        /// プロセス詳細をキャッシュに追加
        /// </summary>
        public void AddDetailToCache(ProcessDetail detail)
        {
            _allDetails.Add(detail);
        }

        /// <summary>
        /// プロセスをキャッシュから削除
        /// </summary>
        public void RemoveProcessFromCache(Process process)
        {
            _allProcesses.Remove(process);
        }

        /// <summary>
        /// プロセス詳細をキャッシュから削除
        /// </summary>
        public void RemoveDetailFromCache(ProcessDetail detail)
        {
            _allDetails.Remove(detail);
        }
    }
}
