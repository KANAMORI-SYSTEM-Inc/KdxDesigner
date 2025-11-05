using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels.IOEditor;
using KdxDesigner.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace KdxDesigner.ViewModels
{
    // Cylinder用のラッパークラス
    public class CylinderViewModel : INotifyPropertyChanged
    {
        private readonly Cylinder _cylinder;
        private string? _machineNameFullName;

        public CylinderViewModel(Cylinder cylinder)
        {
            _cylinder = cylinder;
        }

        // Cylinderのプロパティをプロキシ
        public int Id => _cylinder.Id;
        public int PlcId => _cylinder.PlcId;
        public string? PUCO => _cylinder.PUCO;
        public string CYNum => _cylinder.CYNum;
        public string? Go => _cylinder.Go;
        public string? Back => _cylinder.Back;
        public string? OilNum => _cylinder.OilNum;
        public int? MachineNameId => _cylinder.MachineNameId;

        // MachineNameのFullNameを保持（表示用）
        public string? MachineNameFullName
        {
            get => _machineNameFullName;
            set
            {
                _machineNameFullName = value;
                OnPropertyChanged();
            }
        }

        // 内部のCylinderオブジェクトを取得
        public Cylinder GetCylinder() => _cylinder;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ViewModel内で使用するラッパークラス
    public class InterlockViewModel : INotifyPropertyChanged
    {
        private readonly Interlock _interlock;
        private string? _conditionCylinderNum;
        private string? _cylinderNum;

        public InterlockViewModel(Interlock interlock)
        {
            _interlock = interlock;
        }

        // Interlockのプロパティをプロキシ（Idプロパティは削除、複合キーを使用）
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

        public int GoOrBack
        {
            get => _interlock.GoOrBack;
            set
            {
                _interlock.GoOrBack = value;
                OnPropertyChanged();
            }
        }

        // 表示用のCYNumプロパティ（このインターロックが属するシリンダーのCYNum）
        public string? CylinderNum
        {
            get => _cylinderNum;
            set
            {
                _cylinderNum = value;
                OnPropertyChanged();
            }
        }

        // 表示用のCYNumプロパティ（条件シリンダーのCYNum）
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

        // 外部からプロパティ変更を通知するためのpublicメソッド
        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
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

        // InterlockIOのプロパティをプロキシ（複合キー対応）
        public int InterlockId
        {
            get => _interlockIO.InterlockId;
            set
            {
                _interlockIO.InterlockId = value;
                OnPropertyChanged();
            }
        }

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

        public int InterlockSortId
        {
            get => _interlockIO.InterlockSortId;
            set
            {
                _interlockIO.InterlockSortId = value;
                OnPropertyChanged();
            }
        }

        public int ConditionNumber
        {
            get => _interlockIO.ConditionNumber;
            set
            {
                _interlockIO.ConditionNumber = value;
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
        private readonly int _cycleId;
        private CylinderViewModel? _selectedCylinder;
        private InterlockViewModel? _selectedInterlock;
        private InterlockConditionDTO? _selectedCondition;
        private InterlockIOViewModel? _selectedIO;
        private string? _cylinderSearchText;

        // Cylinder filtering
        private readonly ObservableCollection<CylinderViewModel> _allCylinders;
        private readonly ICollectionView _filteredCylinders;

        // Track deleted items for database cleanup
        private readonly List<InterlockIO> _deletedIOs = new List<InterlockIO>();
        private readonly List<InterlockConditionDTO> _deletedConditions = new List<InterlockConditionDTO>();
        private readonly List<InterlockViewModel> _deletedInterlocks = new List<InterlockViewModel>();

        // Cache all conditions and IOs for all interlocks (複合キーを使用)
        private readonly Dictionary<(int cylinderId, int sortId), List<InterlockConditionDTO>> _allConditionsByInterlockKey = new Dictionary<(int, int), List<InterlockConditionDTO>>();
        private readonly Dictionary<(int interlockId, int sortId, int conditionNumber), List<InterlockIOViewModel>> _allIOsByConditionKey = new Dictionary<(int, int, int), List<InterlockIOViewModel>>();

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

        public CylinderViewModel? SelectedCylinder
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

        public InterlockSettingsViewModel(SupabaseRepository supabaseRepository, ISupabaseRepository accessRepository, int plcId, int cycleId, Window window)
        {
            _supabaseRepository = supabaseRepository;
            _accessRepository = accessRepository;
            _plcId = plcId;
            _cycleId = cycleId;
            _window = window;

            // Initialize collections
            Interlocks = new ObservableCollection<InterlockViewModel>();
            InterlockConditions = new ObservableCollection<InterlockConditionDTO>();
            InterlockIOs = new ObservableCollection<InterlockIOViewModel>();
            ConditionTypes = new ObservableCollection<InterlockConditionType>();

            // Initialize cylinder list and filtering
            _allCylinders = new ObservableCollection<CylinderViewModel>();
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

            AddInterlockCommand = new RelayCommand(() => AddInterlock(null), () => CanAddInterlock(null));
            DeleteInterlockCommand = new RelayCommand(() => DeleteInterlock(null), () => CanDeleteInterlock(null));
            EditPreConditionsCommand = new RelayCommand(() => EditPreConditions(null), () => CanEditPreConditions(null));
            AddConditionCommand = new RelayCommand(() => AddCondition(null), () => CanAddCondition(null));
            DeleteConditionCommand = new RelayCommand(() => DeleteCondition(null), () => CanDeleteCondition(null));
            AddIOCommand = new RelayCommand(() => AddIO(null), () => CanAddIO(null));
            DeleteIOCommand = new RelayCommand(() => DeleteIO(null), () => CanDeleteIO(null));
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => Cancel(null));
            ClearCylinderSearchCommand = new RelayCommand(() => CylinderSearchText = string.Empty);

            LoadCylinders();
            _ = LoadConditionTypesAsync();
        }

        private async Task LoadCylinders()
        {
            // 選択されたサイクルに紐づくCylinderのみを取得
            var allCylinders = await _accessRepository.GetCyListAsync(_plcId);
            var cylinderCycles = await _supabaseRepository.GetCylinderCyclesByPlcIdAsync(_plcId);

            // 指定されたcycleIdに紐づくCylinderIdのセットを作成
            var cylinderIdsInCycle = cylinderCycles
                .Where(cc => cc.CycleId == _cycleId)
                .Select(cc => cc.CylinderId)
                .ToHashSet();

            // MachineNameを取得してIDでマッピング
            var machineNames = await _supabaseRepository.GetMachineNamesAsync();
            var machineNameDict = machineNames.ToDictionary(mn => mn.Id, mn => mn.FullName);

            _allCylinders.Clear();
            foreach (var cylinder in allCylinders.Where(c => cylinderIdsInCycle.Contains(c.Id)))
            {
                // CylinderViewModelを作成
                var cylinderViewModel = new CylinderViewModel(cylinder);

                // MachineNameIdからFullNameを取得してViewModelに設定
                if (cylinder.MachineNameId.HasValue && machineNameDict.TryGetValue(cylinder.MachineNameId.Value, out var fullName))
                {
                    cylinderViewModel.MachineNameFullName = fullName;
                }

                _allCylinders.Add(cylinderViewModel);
            }
        }

        private bool FilterCylinder(object obj)
        {
            if (obj is not CylinderViewModel cylinderVm) return false;
            if (string.IsNullOrWhiteSpace(CylinderSearchText)) return true;

            var searchLower = CylinderSearchText.ToLower();
            return (cylinderVm.CYNum?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinderVm.PUCO?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinderVm.Go?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinderVm.Back?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinderVm.OilNum?.ToLower().Contains(searchLower) ?? false) ||
                   (cylinderVm.MachineNameFullName?.ToLower().Contains(searchLower) ?? false) ||
                   cylinderVm.Id.ToString().Contains(searchLower);
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
                ErrorDialog.Show($"条件タイプの読み込みに失敗しました: {ex.Message}\n\nスタックトレース:\n{ex.StackTrace}", "エラー", _window);
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

                    // CylinderIdに対応するCYNumを取得（このインターロックが属するシリンダー）
                    if (_selectedCylinder != null)
                    {
                        interlockViewModel.CylinderNum = _selectedCylinder.CYNum;
                    }

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
                ErrorDialog.Show($"インターロックの読み込みに失敗しました: {ex.Message}\n\nスタックトレース:\n{ex.StackTrace}", "エラー", _window);
            }
        }

        private async Task LoadInterlockConditionsAsync()
        {
            InterlockConditions.Clear();
            InterlockIOs.Clear();

            if (SelectedInterlock == null) return;

            try
            {
                var interlockKey = (SelectedInterlock.CylinderId, SelectedInterlock.SortId);

                // キャッシュから取得するか、データベースから読み込む
                List<InterlockConditionDTO> conditions;
                if (_allConditionsByInterlockKey.TryGetValue(interlockKey, out var cachedConditions))
                {
                    conditions = cachedConditions;
                }
                else
                {
                    conditions = await _supabaseRepository.GetInterlockConditionsByInterlockIdAsync(SelectedInterlock.CylinderId);
                    _allConditionsByInterlockKey[interlockKey] = conditions;
                }

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
                ErrorDialog.Show($"インターロック条件の読み込みに失敗しました: {ex.Message}\n\nスタックトレース:\n{ex.StackTrace}", "エラー", _window);
            }
        }

        private async Task LoadInterlockIOsAsync()
        {
            InterlockIOs.Clear();

            if (SelectedCondition == null || SelectedInterlock == null) return;

            try
            {
                var conditionKey = (SelectedCondition.InterlockId, SelectedCondition.InterlockSortId, SelectedCondition.ConditionNumber);

                // キャッシュから取得するか、データベースから読み込む
                List<InterlockIOViewModel> ioViewModels;
                if (_allIOsByConditionKey.TryGetValue(conditionKey, out var cachedIOs))
                {
                    ioViewModels = cachedIOs;
                    System.Diagnostics.Debug.WriteLine($"キャッシュからIO読み込み: Key={conditionKey}, 件数={ioViewModels.Count}");
                }
                else
                {
                    var ios = await _supabaseRepository.GetInterlockIOsByInterlockIdAsync(SelectedCondition.InterlockId);
                    ioViewModels = new List<InterlockIOViewModel>();

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

                        ioViewModels.Add(ioViewModel);
                    }

                    _allIOsByConditionKey[conditionKey] = ioViewModels;
                    System.Diagnostics.Debug.WriteLine($"DBからIO読み込みしてキャッシュ: Key={conditionKey}, 件数={ioViewModels.Count}");
                }

                foreach (var ioViewModel in ioViewModels)
                {
                    InterlockIOs.Add(ioViewModel);
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show($"インターロックIOの読み込みに失敗しました: {ex.Message}\n\nスタックトレース:\n{ex.StackTrace}", "エラー", _window);
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
                // Track for deletion（複合キー対応）
                _deletedInterlocks.Add(SelectedInterlock);

                // Also mark all related conditions and IOs for deletion
                var interlockKey = (SelectedInterlock.CylinderId, SelectedInterlock.SortId);
                if (_allConditionsByInterlockKey.TryGetValue(interlockKey, out var relatedConditions))
                {
                    foreach (var condition in relatedConditions)
                    {
                        _deletedConditions.Add(condition);

                        var conditionKey = (condition.InterlockId, condition.InterlockSortId, condition.ConditionNumber);
                        if (_allIOsByConditionKey.TryGetValue(conditionKey, out var relatedIOs))
                        {
                            _deletedIOs.AddRange(relatedIOs.Select(vm => vm.GetInterlockIO()));
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
                InterlockId = SelectedInterlock.CylinderId,
                InterlockSortId = SelectedInterlock.SortId,
                ConditionNumber = InterlockConditions.Count + 1,
                ConditionTypeId = defaultTypeId,
                ConditionType = ConditionTypes.FirstOrDefault(ct => ct.Id == defaultTypeId)
            };
            InterlockConditions.Add(newCondition);

            // キャッシュにも追加
            var interlockKey = (SelectedInterlock.CylinderId, SelectedInterlock.SortId);
            if (!_allConditionsByInterlockKey.ContainsKey(interlockKey))
            {
                _allConditionsByInterlockKey[interlockKey] = new List<InterlockConditionDTO>();
            }
            _allConditionsByInterlockKey[interlockKey].Add(newCondition);

            SelectedCondition = newCondition;
        }

        private bool CanDeleteCondition(object? parameter) => SelectedCondition != null;

        private void DeleteCondition(object? parameter)
        {
            if (SelectedCondition == null) return;

            var result = MessageBox.Show("選択した条件を削除しますか？\n関連するIOも削除されます。", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Track for deletion (複合キーに対応)
                _deletedConditions.Add(SelectedCondition);

                // Also mark all related IOs for deletion
                var conditionKey = (SelectedCondition.InterlockId, SelectedCondition.InterlockSortId, SelectedCondition.ConditionNumber);
                if (_allIOsByConditionKey.TryGetValue(conditionKey, out var relatedIOs))
                {
                    _deletedIOs.AddRange(relatedIOs.Select(vm => vm.GetInterlockIO()));
                    _allIOsByConditionKey.Remove(conditionKey);
                }

                InterlockConditions.Remove(SelectedCondition);

                // キャッシュからも削除
                if (SelectedInterlock != null)
                {
                    var interlockKey = (SelectedInterlock.CylinderId, SelectedInterlock.SortId);
                    if (_allConditionsByInterlockKey.TryGetValue(interlockKey, out var conditions))
                    {
                        conditions.Remove(SelectedCondition);
                    }
                }

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
                    InterlockId = SelectedCondition.InterlockId,
                    InterlockSortId = SelectedCondition.InterlockSortId,
                    ConditionNumber = SelectedCondition.ConditionNumber,
                    PlcId = plcId,
                    IOAddress = ioAddress,
                    IsOnCondition = false
                };

                var ioViewModel = new InterlockIOViewModel(newIO, true) // 新規作成
                {
                    IOName = selectedIO.IOName  // 選択されたIOからIONameを設定
                };

                InterlockIOs.Add(ioViewModel);

                // キャッシュにも追加
                var conditionKey = (SelectedCondition.InterlockId, SelectedCondition.InterlockSortId, SelectedCondition.ConditionNumber);
                if (!_allIOsByConditionKey.ContainsKey(conditionKey))
                {
                    _allIOsByConditionKey[conditionKey] = new List<InterlockIOViewModel>();
                }
                _allIOsByConditionKey[conditionKey].Add(ioViewModel);

                SelectedIO = ioViewModel;
            }
        }

        private bool CanDeleteIO(object? parameter) => SelectedIO != null;

        private void DeleteIO(object? parameter)
        {
            if (SelectedIO == null) return;

            var result = MessageBox.Show("選択したIOを削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Track for deletion (保存時に削除)
                if (!SelectedIO.IsNew)
                {
                    _deletedIOs.Add(SelectedIO.GetInterlockIO());
                }

                // UIから削除
                InterlockIOs.Remove(SelectedIO);

                // キャッシュからも削除
                if (SelectedCondition != null)
                {
                    var conditionKey = (SelectedCondition.InterlockId, SelectedCondition.InterlockSortId, SelectedCondition.ConditionNumber);
                    if (_allIOsByConditionKey.TryGetValue(conditionKey, out var ios))
                    {
                        var removed = ios.Remove(SelectedIO);
                        System.Diagnostics.Debug.WriteLine($"キャッシュからIO削除: Key={conditionKey}, 削除成功={removed}, 残り={ios.Count}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"キャッシュにキーが見つかりません: {conditionKey}");
                    }
                }

                SelectedIO = null;
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                // First, delete all tracked items from database
                // Delete IOs first (due to foreign key constraints)
                foreach (var io in _deletedIOs)
                {
                    try
                    {
                        await _supabaseRepository.DeleteInterlockIOAsync(io);
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

                // Upsert前のIdとViewModelをマッピング
                // Interlocksを保存（複合キー対応）
                await _supabaseRepository.UpsertInterlocksAsync(interlocksToSave);

                // キャッシュから全てのInterlockConditionsを収集して保存
                var allConditionsToSave = new List<InterlockConditionDTO>();
                foreach (var kvp in _allConditionsByInterlockKey)
                {
                    allConditionsToSave.AddRange(kvp.Value);
                }

                if (allConditionsToSave.Any())
                {
                    await _supabaseRepository.UpsertInterlockConditionsAsync(allConditionsToSave);
                }

                // キャッシュから全てのInterlockIOsを収集して保存 (新規作成されたもののみ)
                foreach (var kvp in _allIOsByConditionKey)
                {
                    var iosToSave = kvp.Value.Where(io => io.IsNew).ToList();

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
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"保存に失敗しました: {ex.Message}");
                errorMessage.AppendLine();
                errorMessage.AppendLine("スタックトレース:");
                errorMessage.AppendLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    errorMessage.AppendLine();
                    errorMessage.AppendLine("内部例外:");
                    errorMessage.AppendLine(ex.InnerException.Message);
                    errorMessage.AppendLine(ex.InnerException.StackTrace);
                }

                ErrorDialog.Show(errorMessage.ToString(), "エラー", _window);
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
