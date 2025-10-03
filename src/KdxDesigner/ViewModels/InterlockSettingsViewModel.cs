using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Utils;
using KdxDesigner.Views;

namespace KdxDesigner.ViewModels
{
    // ViewModel内で使用するラッパークラス
    public class InterlockViewModel : INotifyPropertyChanged
    {
        private readonly Interlock _interlock;
        private string? _conditionCylinderNum;

        public InterlockViewModel(Interlock interlock)
        {
            _interlock = interlock;
        }

        // Interlockのプロパティをプロキシ
        public int Id
        {
            get => _interlock.Id;
            set
            {
                _interlock.Id = value;
                OnPropertyChanged();
            }
        }

        public int PlcId
        {
            get => _interlock.PlcId;
            set
            {
                _interlock.PlcId = value;
                OnPropertyChanged();
            }
        }

        public int CylinderId
        {
            get => _interlock.CylinderId;
            set
            {
                _interlock.CylinderId = value;
                OnPropertyChanged();
            }
        }

        public int SortId
        {
            get => _interlock.SortId;
            set
            {
                _interlock.SortId = value;
                OnPropertyChanged();
            }
        }

        public int ConditionCylinderId
        {
            get => _interlock.ConditionCylinderId;
            set
            {
                _interlock.ConditionCylinderId = value;
                OnPropertyChanged();
            }
        }

        public int? PreConditionID1
        {
            get => _interlock.PreConditionID1;
            set
            {
                _interlock.PreConditionID1 = value;
                OnPropertyChanged();
            }
        }

        public int? PreConditionID2
        {
            get => _interlock.PreConditionID2;
            set
            {
                _interlock.PreConditionID2 = value;
                OnPropertyChanged();
            }
        }

        // 表示用のCYNumプロパティ
        public string? ConditionCylinderNum
        {
            get => _conditionCylinderNum;
            set
            {
                _conditionCylinderNum = value;
                OnPropertyChanged();
            }
        }

        // 内部のInterlockオブジェクトを取得
        public Interlock GetInterlock() => _interlock;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // InterlockIO用のラッパークラス
    public class InterlockIOViewModel : INotifyPropertyChanged
    {
        private readonly InterlockIO _interlockIO;
        private string? _ioName;
        private bool _isNew; // 新規作成フラグ

        public InterlockIOViewModel(InterlockIO interlockIO, bool isNew = false)
        {
            _interlockIO = interlockIO;
            _isNew = isNew;
        }

        // InterlockIOのプロパティをプロキシ
        public int PlcId
        {
            get => _interlockIO.PlcId;
            set
            {
                _interlockIO.PlcId = value;
                OnPropertyChanged();
            }
        }

        public string? IOAddress
        {
            get => _interlockIO.IOAddress;
            set
            {
                _interlockIO.IOAddress = value;
                OnPropertyChanged();
            }
        }

        public int InterlockConditionId
        {
            get => _interlockIO.InterlockConditionId;
            set
            {
                _interlockIO.InterlockConditionId = value;
                OnPropertyChanged();
            }
        }

        public bool IsOnCondition
        {
            get => _interlockIO.IsOnCondition;
            set
            {
                _interlockIO.IsOnCondition = value;
                OnPropertyChanged();
            }
        }

        public int ConditionUniqueKey
        {
            get => _interlockIO.ConditionUniqueKey;
            set
            {
                _interlockIO.ConditionUniqueKey = value;
                OnPropertyChanged();
            }
        }

        // 表示用のIONameプロパティ
        public string? IOName
        {
            get => _ioName;
            set
            {
                _ioName = value;
                OnPropertyChanged();
            }
        }

        // 新規作成フラグ
        public bool IsNew
        {
            get => _isNew;
            set
            {
                _isNew = value;
                OnPropertyChanged();
            }
        }

        // 内部のInterlockIOオブジェクトを取得
        public InterlockIO GetInterlockIO() => _interlockIO;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class InterlockSettingsViewModel : INotifyPropertyChanged
    {
        private readonly SupabaseRepository _supabaseRepository;
        private readonly ISupabaseRepository _accessRepository;
        private readonly Window _window;
        private readonly int _plcId;
        private Cylinder? _selectedCylinder;
        private InterlockViewModel? _selectedInterlock;
        private InterlockConditionDTO? _selectedCondition;
        private InterlockIOViewModel? _selectedIO;
        private string? _cylinderSearchText;

        // Cylinder filtering
        private readonly ObservableCollection<Cylinder> _allCylinders;
        private readonly ICollectionView _filteredCylinders;

        // Track deleted items for database cleanup
        private readonly List<InterlockIOViewModel> _deletedIOs = new List<InterlockIOViewModel>();
        private readonly List<InterlockConditionDTO> _deletedConditions = new List<InterlockConditionDTO>();
        private readonly List<InterlockViewModel> _deletedInterlocks = new List<InterlockViewModel>();

        public ObservableCollection<InterlockViewModel> Interlocks { get; set; }
        public ObservableCollection<InterlockConditionDTO> InterlockConditions { get; set; }
        public ObservableCollection<InterlockIOViewModel> InterlockIOs { get; set; }
        public ObservableCollection<InterlockConditionType> ConditionTypes { get; set; }

        public ICollectionView FilteredCylinders => _filteredCylinders;

        public string? CylinderSearchText
        {
            get => _cylinderSearchText;
            set
            {
                _cylinderSearchText = value;
                OnPropertyChanged();
                _filteredCylinders.Refresh();
            }
        }

        public Cylinder? SelectedCylinder
        {
            get => _selectedCylinder;
            set
            {
                _selectedCylinder = value;
                OnPropertyChanged();
                if (value != null)
                {
                    _ = LoadInterlocksAsync();
                }
            }
        }

        public InterlockViewModel? SelectedInterlock
        {
            get => _selectedInterlock;
            set
            {
                _selectedInterlock = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsInterlockSelected));
                _ = LoadInterlockConditionsAsync();
            }
        }

        public InterlockConditionDTO? SelectedCondition
        {
            get => _selectedCondition;
            set
            {
                _selectedCondition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConditionSelected));
                _ = LoadInterlockIOsAsync();
            }
        }

        public InterlockIOViewModel? SelectedIO
        {
            get => _selectedIO;
            set
            {
                _selectedIO = value;
                OnPropertyChanged();
            }
        }

        public bool IsInterlockSelected => SelectedInterlock != null;
        public bool IsConditionSelected => SelectedCondition != null;

        public ICommand AddInterlockCommand { get; }
        public ICommand DeleteInterlockCommand { get; }
        public ICommand EditPreConditionsCommand { get; }
        public ICommand AddConditionCommand { get; }
        public ICommand DeleteConditionCommand { get; }
        public ICommand AddIOCommand { get; }
        public ICommand DeleteIOCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearCylinderSearchCommand { get; }

        public InterlockSettingsViewModel(SupabaseRepository supabaseRepository, ISupabaseRepository accessRepository, int plcId, Window window)
        {
            _supabaseRepository = supabaseRepository;
            _accessRepository = accessRepository;
            _plcId = plcId;
            _window = window;

            // Initialize collections
            Interlocks = new ObservableCollection<InterlockViewModel>();
            InterlockConditions = new ObservableCollection<InterlockConditionDTO>();
            InterlockIOs = new ObservableCollection<InterlockIOViewModel>();
            ConditionTypes = new ObservableCollection<InterlockConditionType>();

            // Initialize cylinder list and filtering
            _allCylinders = new ObservableCollection<Cylinder>();
            _filteredCylinders = CollectionViewSource.GetDefaultView(_allCylinders);
            _filteredCylinders.Filter = FilterCylinder;

            // Subscribe to collection changes to sync ConditionType when ConditionTypeId changes
            InterlockConditions.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InterlockConditionDTO condition in e.NewItems)
                    {
                        condition.PropertyChanged += OnConditionPropertyChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (InterlockConditionDTO condition in e.OldItems)
                    {
                        condition.PropertyChanged -= OnConditionPropertyChanged;
                    }
                }
            };

            AddInterlockCommand = new RelayCommand(AddInterlock, CanAddInterlock);
            DeleteInterlockCommand = new RelayCommand(DeleteInterlock, CanDeleteInterlock);
            EditPreConditionsCommand = new RelayCommand(EditPreConditions, CanEditPreConditions);
            AddConditionCommand = new RelayCommand(AddCondition, CanAddCondition);
            DeleteConditionCommand = new RelayCommand(DeleteCondition, CanDeleteCondition);
            AddIOCommand = new RelayCommand(AddIO, CanAddIO);
            DeleteIOCommand = new RelayCommand(DeleteIO, CanDeleteIO);
            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(Cancel);
            ClearCylinderSearchCommand = new RelayCommand(_ => CylinderSearchText = string.Empty);

            LoadCylinders();
            _ = LoadConditionTypesAsync();
        }

        private async Task LoadCylinders()
        {
            var cylinders = await _accessRepository.GetCyListAsync(_plcId);
            _allCylinders.Clear();
            foreach (var cylinder in cylinders)
            {
                _allCylinders.Add(cylinder);
            }
        }

        private bool FilterCylinder(object obj)
        {
            if (obj is not Cylinder cylinder) return false;
            if (string.IsNullOrWhiteSpace(CylinderSearchText)) return true;

            var searchLower = CylinderSearchText.ToLower();
            return (cylinder.CYNum?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinder.PUCO?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinder.Go?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinder.Back?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinder.OilNum?.ToLower().Contains(searchLower) ?? false) ||
                   cylinder.Id.ToString().Contains(searchLower);
        }

        private void OnConditionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InterlockConditionDTO.ConditionTypeId) && sender is InterlockConditionDTO condition)
            {
                // Update the ConditionType navigation property when ConditionTypeId changes
                condition.ConditionType = ConditionTypes.FirstOrDefault(ct => ct.Id == condition.ConditionTypeId);
            }
        }

        private async Task LoadConditionTypesAsync()
        {
            try
            {
                var types = await _supabaseRepository.GetInterlockConditionTypesAsync();
                ConditionTypes.Clear();
                foreach (var type in types)
                {
                    ConditionTypes.Add(type);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"条件タイプの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadInterlocksAsync()
        {
            if (_selectedCylinder == null)
            {
                Interlocks.Clear();
                return;
            }

            try
            {
                var interlocks = await _supabaseRepository.GetInterlocksByCylindrIdAsync(_selectedCylinder.Id);
                Interlocks.Clear();
                foreach (var interlock in interlocks)
                {
                    var interlockViewModel = new InterlockViewModel(interlock);

                    // ConditionCylinderIdに対応するCYNumを取得
                    var conditionCylinder = _allCylinders.FirstOrDefault(c => c.Id == interlock.ConditionCylinderId);
                    if (conditionCylinder != null)
                    {
                        interlockViewModel.ConditionCylinderNum = conditionCylinder.CYNum;
                    }

                    Interlocks.Add(interlockViewModel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"インターロックの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadInterlockConditionsAsync()
        {
            InterlockConditions.Clear();
            InterlockIOs.Clear();

            if (SelectedInterlock == null) return;

            try
            {
                var conditions = await _supabaseRepository.GetInterlockConditionsByInterlockIdAsync(SelectedInterlock.Id);

                // Populate the ConditionType navigation property for each condition
                foreach (var condition in conditions)
                {
                    var conditionType = ConditionTypes.FirstOrDefault(ct => ct.Id == condition.ConditionTypeId);
                    condition.ConditionType = conditionType;
                    // Subscribe to property changes for existing conditions
                    condition.PropertyChanged += OnConditionPropertyChanged;
                    InterlockConditions.Add(condition);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"インターロック条件の読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadInterlockIOsAsync()
        {
            InterlockIOs.Clear();

            if (SelectedCondition == null) return;

            try
            {
                var ios = await _supabaseRepository.GetInterlockIOsByInterlockIdAsync(SelectedCondition.Id);
                foreach (var io in ios)
                {
                    var ioViewModel = new InterlockIOViewModel(io, false); // 既存データ

                    // PlcIdとIOAddressに対応するIONameを取得
                    if (!string.IsNullOrEmpty(io.IOAddress))
                    {
                        var allIOs = await _accessRepository.GetIoListAsync();
                        var ioData = allIOs.FirstOrDefault(i => i.Address == io.IOAddress && i.PlcId == io.PlcId);
                        if (ioData != null)
                        {
                            ioViewModel.IOName = ioData.IOName;
                        }
                    }

                    InterlockIOs.Add(ioViewModel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"インターロックIOの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanAddInterlock(object? parameter) => SelectedCylinder != null;

        private void AddInterlock(object? parameter)
        {
            if (_selectedCylinder == null) return;

            var newInterlock = new Interlock
            {
                CylinderId = _selectedCylinder.Id,
                PlcId = _selectedCylinder.PlcId,
                SortId = Interlocks.Count + 1,
                ConditionCylinderId = _selectedCylinder.Id
            };

            var interlockViewModel = new InterlockViewModel(newInterlock)
            {
                ConditionCylinderNum = _selectedCylinder.CYNum
            };

            Interlocks.Add(interlockViewModel);
            SelectedInterlock = interlockViewModel;
        }

        private bool CanDeleteInterlock(object? parameter) => SelectedInterlock != null;

        private void DeleteInterlock(object? parameter)
        {
            if (SelectedInterlock == null) return;

            var result = MessageBox.Show("選択したインターロックを削除しますか？\n関連する条件とIOも削除されます。", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Track for deletion if it exists in database (has Id > 0)
                if (SelectedInterlock.Id > 0)
                {
                    _deletedInterlocks.Add(SelectedInterlock);

                    // Also mark all related conditions and IOs for deletion
                    var relatedConditions = InterlockConditions.Where(c => c.InterlockId == SelectedInterlock.Id).ToList();
                    foreach (var condition in relatedConditions)
                    {
                        if (condition.Id > 0)
                        {
                            _deletedConditions.Add(condition);

                            var relatedIOs = InterlockIOs.Where(io => io.InterlockConditionId == condition.Id).ToList();
                            _deletedIOs.AddRange(relatedIOs);
                        }
                    }
                }

                Interlocks.Remove(SelectedInterlock);
                SelectedInterlock = null;
            }
        }

        private bool CanAddCondition(object? parameter) => SelectedInterlock != null;

        private void AddCondition(object? parameter)
        {
            if (SelectedInterlock == null) return;

            // Set default ConditionTypeId and populate ConditionType
            var defaultTypeId = ConditionTypes.FirstOrDefault()?.Id ?? 1;
            var newCondition = new InterlockConditionDTO
            {
                InterlockId = SelectedInterlock.Id,
                ConditionNumber = InterlockConditions.Count + 1,
                ConditionTypeId = defaultTypeId,
                ConditionType = ConditionTypes.FirstOrDefault(ct => ct.Id == defaultTypeId)
            };
            InterlockConditions.Add(newCondition);
            SelectedCondition = newCondition;
        }

        private bool CanDeleteCondition(object? parameter) => SelectedCondition != null;

        private void DeleteCondition(object? parameter)
        {
            if (SelectedCondition == null) return;

            var result = MessageBox.Show("選択した条件を削除しますか？\n関連するIOも削除されます。", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Track for deletion if it exists in database
                if (SelectedCondition.Id > 0)
                {
                    _deletedConditions.Add(SelectedCondition);

                    // Also mark all related IOs for deletion
                    var relatedIOs = InterlockIOs.Where(io => io.InterlockConditionId == SelectedCondition.Id).ToList();
                    _deletedIOs.AddRange(relatedIOs);
                }

                InterlockConditions.Remove(SelectedCondition);
                SelectedCondition = null;
            }
        }

        private bool CanAddIO(object? parameter) => SelectedCondition != null;

        private void AddIO(object? parameter)
        {
            if (SelectedCondition == null || _selectedCylinder == null) return;

            // Open IO search window with cylinder CYNum as initial search
            var ioSearchWindow = new IOSearchWindow();
            var ioSearchViewModel = new IOSearchViewModel(_accessRepository, _selectedCylinder.PlcId, _selectedCylinder.CYNum);
            ioSearchWindow.DataContext = ioSearchViewModel;
            ioSearchWindow.Owner = _window;

            if (ioSearchWindow.ShowDialog() == true && ioSearchViewModel.SelectedIO != null)
            {
                var selectedIO = ioSearchViewModel.SelectedIO;

                // Determine the IOAddress based on PlcId
                string ioAddress;
                int plcId;

                if (selectedIO.PlcId == _selectedCylinder.PlcId)
                {
                    // Same PLC - use the direct address
                    ioAddress = selectedIO.Address;
                    plcId = selectedIO.PlcId;
                }
                else
                {
                    // Different PLC - use LinkDevice if available
                    ioAddress = !string.IsNullOrEmpty(selectedIO.LinkDevice)
                        ? selectedIO.LinkDevice
                        : selectedIO.Address;
                    plcId = selectedIO.PlcId;
                }

                var newIO = new InterlockIO
                {
                    InterlockConditionId = SelectedCondition.Id,
                    PlcId = plcId,
                    IOAddress = ioAddress,
                    IsOnCondition = false,
                    ConditionUniqueKey = InterlockIOs.Count + 1
                };

                var ioViewModel = new InterlockIOViewModel(newIO, true) // 新規作成
                {
                    IOName = selectedIO.IOName  // 選択されたIOからIONameを設定
                };

                InterlockIOs.Add(ioViewModel);
                SelectedIO = ioViewModel;
            }
        }

        private bool CanDeleteIO(object? parameter) => SelectedIO != null;

        private async void DeleteIO(object? parameter)
        {
            if (SelectedIO == null) return;

            var result = MessageBox.Show("選択したIOを削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // If IO exists in database, delete it immediately
                    if (!string.IsNullOrEmpty(SelectedIO.IOAddress))
                    {
                        await _supabaseRepository.DeleteInterlockIOAsync(SelectedIO.GetInterlockIO());
                        MessageBox.Show("IOを削除しました。", "削除完了", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    InterlockIOs.Remove(SelectedIO);
                    SelectedIO = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"IOの削除に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                // First, delete all tracked items from database
                // Delete IOs first (due to foreign key constraints)
                foreach (var ioVm in _deletedIOs)
                {
                    try
                    {
                        await _supabaseRepository.DeleteInterlockIOAsync(ioVm.GetInterlockIO());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete InterlockIO: {ex.Message}");
                    }
                }

                // Delete InterlockConditions
                foreach (var condition in _deletedConditions)
                {
                    try
                    {
                        await _supabaseRepository.DeleteInterlockConditionAsync(condition);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete InterlockCondition: {ex.Message}");
                    }
                }

                // Delete Interlocks
                foreach (var interlockVm in _deletedInterlocks)
                {
                    try
                    {
                        await _supabaseRepository.DeleteInterlockAsync(interlockVm.GetInterlock());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete Interlock: {ex.Message}");
                    }
                }

                // Save Interlocks
                var interlocksToSave = Interlocks.Select(vm => vm.GetInterlock()).ToList();
                await _supabaseRepository.UpsertInterlocksAsync(interlocksToSave);

                // Save InterlockConditions
                var conditionsToSave = new List<InterlockConditionDTO>();
                foreach (var interlockVm in Interlocks)
                {
                    var conditions = InterlockConditions.Where(c => c.InterlockId == interlockVm.Id).ToList();
                    conditionsToSave.AddRange(conditions);
                }
                if (conditionsToSave.Any())
                {
                    await _supabaseRepository.UpsertInterlockConditionsAsync(conditionsToSave);
                }

                // Save InterlockIOs (新規作成されたもののみ)
                foreach (var condition in conditionsToSave)
                {
                    var iosToSave = InterlockIOs.Where(io => io.InterlockConditionId == condition.Id && io.IsNew).ToList();

                    foreach (var ioVm in iosToSave)
                    {
                        if (!string.IsNullOrEmpty(ioVm.IOAddress)) // Only save if IOAddress is set
                        {
                            try
                            {
                                await _supabaseRepository.AddInterlockIOAssociationAsync(ioVm.GetInterlockIO());
                                ioVm.IsNew = false; // 保存成功後は既存データとして扱う
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to save InterlockIO: {ex.Message}");
                            }
                        }
                    }
                }

                MessageBox.Show("保存しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanEditPreConditions(object? parameter) => SelectedInterlock != null;

        private void EditPreConditions(object? parameter)
        {
            if (SelectedInterlock == null) return;

            var preConditionWindow = new InterlockPreConditionWindow();
            var preConditionViewModel = new InterlockPreConditionViewModel(
                _supabaseRepository,
                SelectedInterlock.GetInterlock(),
                preConditionWindow);
            preConditionWindow.DataContext = preConditionViewModel;
            preConditionWindow.Owner = _window;

            if (preConditionWindow.ShowDialog() == true)
            {
                // PreConditionIDが更新された場合、ViewModelも更新
                OnPropertyChanged(nameof(SelectedInterlock));
            }
        }

        private void Cancel(object? parameter)
        {
            _window.DialogResult = false;
            _window.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
