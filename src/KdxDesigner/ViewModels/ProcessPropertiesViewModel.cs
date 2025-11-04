using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;
using Process = Kdx.Contracts.DTOs.Process;

namespace KdxDesigner.ViewModels
{
    public partial class ProcessPropertiesViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private Process _process;

        [ObservableProperty] private int _id;
        [ObservableProperty] private string _processName = "";
        [ObservableProperty] private int? _processCategoryId;
        [ObservableProperty] private int? _cycleId;
        [ObservableProperty] private string? _testStart;
        [ObservableProperty] private string? _testCondition;
        [ObservableProperty] private string? _testMode;
        [ObservableProperty] private string? _autoMode;
        [ObservableProperty] private string? _autoStart;
        [ObservableProperty] private string? _iLStart;
        [ObservableProperty] private string? _comment1;
        [ObservableProperty] private string? _comment2;
        [ObservableProperty] private int? _sortNumber;
        [ObservableProperty] private ObservableCollection<ProcessCategory> _processCategories = new();

        public bool DialogResult { get; private set; }

        public ProcessPropertiesViewModel(ISupabaseRepository repository, Process process)
        {
            _repository = repository;
            _process = process;

            // プロセスのプロパティを読み込み
            Id = process.Id;
            ProcessName = process.ProcessName ?? "";
            ProcessCategoryId = process.ProcessCategoryId;
            CycleId = process.CycleId;
            TestStart = process.TestStart;
            TestCondition = process.TestCondition;
            TestMode = process.TestMode;
            AutoMode = process.AutoMode;
            AutoStart = process.AutoStart;
            ILStart = process.ILStart;
            Comment1 = process.Comment1;
            Comment2 = process.Comment2;
            SortNumber = process.SortNumber;

            // カテゴリのリストを読み込み
            LoadCategories();
        }

        private async void LoadCategories()
        {
            var categories = await _repository.GetProcessCategoriesAsync();
            ProcessCategories = new ObservableCollection<ProcessCategory>(categories);
        }

        [RelayCommand]
        private void Save()
        {
            // プロセスのプロパティを更新
            _process.ProcessName = ProcessName;
            _process.ProcessCategoryId = ProcessCategoryId;
            _process.CycleId = CycleId;
            _process.TestStart = TestStart;
            _process.TestCondition = TestCondition;
            _process.TestMode = TestMode;
            _process.AutoMode = AutoMode;
            _process.AutoStart = AutoStart;
            _process.ILStart = ILStart;
            _process.Comment1 = Comment1;
            _process.Comment2 = Comment2;
            _process.SortNumber = SortNumber;

            // データベースに保存
            _ = _repository.UpdateProcessAsync(_process);

            DialogResult = true;
            RequestClose?.Invoke();
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke();
        }

        public event Action? RequestClose;

        public void UpdateProcess(Process process)
        {
            _process = process;

            // プロセスのプロパティを読み込み
            Id = process.Id;
            ProcessName = process.ProcessName ?? "";
            ProcessCategoryId = process.ProcessCategoryId;
            CycleId = process.CycleId;
            TestStart = process.TestStart;
            TestCondition = process.TestCondition;
            TestMode = process.TestMode;
            AutoMode = process.AutoMode;
            AutoStart = process.AutoStart;
            ILStart = process.ILStart;
            Comment1 = process.Comment1;
            Comment2 = process.Comment2;
            SortNumber = process.SortNumber;
        }

        public void ClearEventHandlers()
        {
            // イベントハンドラをクリア
            RequestClose = null;
        }
    }
}