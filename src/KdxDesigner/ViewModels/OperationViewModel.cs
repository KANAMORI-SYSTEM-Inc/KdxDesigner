using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace KdxDesigner.ViewModels
{
    public partial class OperationViewModel : ObservableObject
    {
        private readonly Operation _operation;
        private readonly ISupabaseRepository _repository;
        private Func<bool, Task>? _closeAction;

        [ObservableProperty]
        private ObservableCollection<OperationCategory> _operationCategories = new();

        [ObservableProperty]
        private ObservableCollection<Cylinder> _cylinders = new();

        private readonly int? _plcId;

        public OperationViewModel(ISupabaseRepository repository, Operation operation, int? plcId = null)
        {
            _repository = repository;
            _operation = operation ?? new Operation();
            _plcId = plcId;
            LoadOperation();
            LoadMasterData();
        }

        [ObservableProperty]
        private string _operationTitle = "Operation編集";

        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string? _operationName;

        [ObservableProperty]
        private int? _cYId;

        [ObservableProperty]
        private int? _categoryId;

        [ObservableProperty]
        private string? _goBack;

        [ObservableProperty]
        private string? _start;

        [ObservableProperty]
        private string? _finish;

        [ObservableProperty]
        private string? _valve1;

        [ObservableProperty]
        private string? _s1;

        [ObservableProperty]
        private string? _s2;

        [ObservableProperty]
        private string? _s3;

        [ObservableProperty]
        private string? _s4;

        [ObservableProperty]
        private string? _s5;

        [ObservableProperty]
        private string? _sS1;

        [ObservableProperty]
        private string? _sS2;

        [ObservableProperty]
        private string? _sS3;

        [ObservableProperty]
        private string? _sS4;

        [ObservableProperty]
        private string? _pIL;

        [ObservableProperty]
        private string? _sC;

        [ObservableProperty]
        private string? _fC;

        [ObservableProperty]
        private int? _cycleId;

        [ObservableProperty]
        private int? _sortNumber;

        [ObservableProperty]
        private string? _con;

        private async void LoadMasterData()
        {
            var categories = await _repository.GetOperationCategoriesAsync();
            OperationCategories = new ObservableCollection<OperationCategory>(categories);

            var cylinders = await _repository.GetCYsAsync();

            // PLCIDでフィルタリング
            if (_plcId.HasValue)
            {
                cylinders = cylinders.Where(c => c.PlcId == _plcId.Value).ToList();
            }

            // SortNumberでソート
            cylinders = cylinders.OrderBy(c => c.SortNumber ?? int.MaxValue).ToList();

            Cylinders = new ObservableCollection<Cylinder>(cylinders);
        }

        private void LoadOperation()
        {
            Id = _operation.Id;
            OperationName = _operation.OperationName;
            CYId = _operation.CYId;
            CategoryId = _operation.CategoryId;
            GoBack = _operation.GoBack;
            Start = _operation.Start;
            Finish = _operation.Finish;
            Valve1 = _operation.Valve1;
            S1 = _operation.S1;
            S2 = _operation.S2;
            S3 = _operation.S3;
            S4 = _operation.S4;
            S5 = _operation.S5;
            SS1 = _operation.SS1;
            SS2 = _operation.SS2;
            SS3 = _operation.SS3;
            SS4 = _operation.SS4;
            PIL = _operation.PIL;
            SC = _operation.SC;
            FC = _operation.FC;
            CycleId = _operation.CycleId;
            SortNumber = _operation.SortNumber;
            Con = _operation.Con;

            OperationTitle = Id == 0 ? "新規Operation作成" : $"Operation編集 - {OperationName ?? $"ID: {Id}"}";
        }

        public Operation GetOperation()
        {
            _operation.Id = Id;
            _operation.OperationName = OperationName;
            _operation.CYId = CYId;
            _operation.CategoryId = CategoryId;
            _operation.GoBack = GoBack;
            _operation.Start = Start;
            _operation.Finish = Finish;
            _operation.Valve1 = Valve1;
            _operation.S1 = S1;
            _operation.S2 = S2;
            _operation.S3 = S3;
            _operation.S4 = S4;
            _operation.S5 = S5;
            _operation.SS1 = SS1;
            _operation.SS2 = SS2;
            _operation.SS3 = SS3;
            _operation.SS4 = SS4;
            _operation.PIL = PIL;
            _operation.SC = SC;
            _operation.FC = FC;
            _operation.CycleId = CycleId;
            _operation.SortNumber = SortNumber;
            _operation.Con = Con;

            return _operation;
        }

        public void SetCloseAction(Func<bool, Task> closeAction)
        {
            _closeAction = closeAction;
        }

        [RelayCommand]
        private async void Save()
        {
            if (_closeAction != null)
            {
                await _closeAction(true);
            }
        }

        [RelayCommand]
        private async void Cancel()
        {
            if (_closeAction != null)
            {
                await _closeAction(false);
            }
        }
    }
}