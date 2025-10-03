using Kdx.Contracts.DTOs;
using KdxDesigner.Models;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KdxDesigner.Views
{
    public partial class RecordIdsEditorDialog : Window, INotifyPropertyChanged
    {
        public class RecordItem : INotifyPropertyChanged
        {
            private bool _isSelected;
            
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsSelected 
            { 
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
            
            public event PropertyChangedEventHandler? PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        
        private readonly TimerViewModel _timer;
        private readonly ISupabaseRepository _repository;
        private ObservableCollection<RecordItem> _availableRecords;
        private ObservableCollection<int> _selectedRecordIds;
        private string _filterText = string.Empty;
        private ICollectionView _filteredRecords;

        public RecordIdsEditorDialog(TimerViewModel timer, ISupabaseRepository repository)
            : this(timer, repository, true)
        {
        }

        // 非同期初期化用のコンストラクタ
        private RecordIdsEditorDialog(TimerViewModel timer, ISupabaseRepository repository, bool asyncInit)
        {
            InitializeComponent();
            DataContext = this;
            _timer = timer;
            _repository = repository;
            _availableRecords = new ObservableCollection<RecordItem>();
            _selectedRecordIds = new ObservableCollection<int>();

            // フィルタリング用のCollectionViewを設定
            _filteredRecords = CollectionViewSource.GetDefaultView(_availableRecords);
            _filteredRecords.Filter = FilterRecords;

            // 初期値を設定
            TimerName = _timer.TimerName ?? "";
            MnemonicTypeName = _timer.MnemonicTypeName ?? "";
            MnemonicId = _timer.MnemonicId;

            // 非同期初期化を呼び出し
            if (asyncInit)
            {
                Loaded += async (s, e) => await InitializeAsync();
            }
        }

        private async Task InitializeAsync()
        {
            // MnemonicTypeに応じて選択可能なレコードを読み込む
            await LoadAvailableRecords();

            // 既存のRecordIdsを読み込んで選択状態に設定
            var existingIds = await _repository.GetTimerRecordIdsAsync(_timer.ID);
            foreach (var record in _availableRecords)
            {
                if (existingIds.Contains(record.Id))
                {
                    record.IsSelected = true;
                    _selectedRecordIds.Add(record.Id);
                }
            }
        }

        public string TimerName { get; }
        public string MnemonicTypeName { get; }
        public int? MnemonicId { get; }

        public ObservableCollection<RecordItem> AvailableRecords
        {
            get => _availableRecords;
            set
            {
                _availableRecords = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<int> SelectedRecordIds
        {
            get => _selectedRecordIds;
            set
            {
                _selectedRecordIds = value;
                OnPropertyChanged();
            }
        }
        
        public string SelectedRecordIdsDisplay
        {
            get => string.Join(", ", _selectedRecordIds);
        }
        
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value ?? string.Empty;
                OnPropertyChanged();
                _filteredRecords?.Refresh();
                OnPropertyChanged(nameof(FilteredRecords));
            }
        }
        
        public ICollectionView FilteredRecords => _filteredRecords;
        
        private bool FilterRecords(object item)
        {
            if (item is not RecordItem record)
                return false;
                
            if (string.IsNullOrWhiteSpace(_filterText))
                return true;
                
            var searchText = _filterText.ToLower();
            return record.Name.ToLower().Contains(searchText) || 
                   record.Id.ToString().Contains(searchText);
        }

        private async Task LoadAvailableRecords()
        {
            _availableRecords.Clear();
            
            if (!MnemonicId.HasValue)
                return;
                
            switch (MnemonicId.Value)
            {
                case 1: // Process
                    var processes = await _repository.GetProcessesAsync();
                    foreach (var process in processes)
                    {
                        _availableRecords.Add(new RecordItem 
                        { 
                            Id = process.Id, 
                            Name = $"{process.ProcessName} (ID: {process.Id})",
                            IsSelected = false
                        });
                    }
                    break;
                    
                case 2: // ProcessDetail
                    var details = await _repository.GetProcessDetailsAsync();
                    foreach (var detail in details)
                    {
                        _availableRecords.Add(new RecordItem 
                        { 
                            Id = detail.Id, 
                            Name = $"{detail.DetailName} (ID: {detail.Id})",
                            IsSelected = false
                        });
                    }
                    break;
                    
                case 3: // Operation
                    var operations = await _repository.GetOperationsAsync();
                    foreach (var operation in operations)
                    {
                        _availableRecords.Add(new RecordItem 
                        { 
                            Id = operation.Id, 
                            Name = $"{operation.OperationName} (ID: {operation.Id})",
                            IsSelected = false
                        });
                    }
                    break;
                    
                case 4: // CY
                    var cylinders = await _repository.GetCYsAsync();
                    foreach (var cy in cylinders)
                    {
                        _availableRecords.Add(new RecordItem 
                        { 
                            Id = cy.Id, 
                            Name = $"CY{cy.CYNum} (ID: {cy.Id})",
                            IsSelected = false
                        });
                    }
                    break;
            }
            
            // フィルタリングを更新
            _filteredRecords?.Refresh();
            OnPropertyChanged(nameof(FilteredRecords));
        }
        
        private void RecordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is RecordItem record)
            {
                if (!_selectedRecordIds.Contains(record.Id))
                {
                    _selectedRecordIds.Add(record.Id);
                    OnPropertyChanged(nameof(SelectedRecordIdsDisplay));
                }
            }
        }
        
        private void RecordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is RecordItem record)
            {
                _selectedRecordIds.Remove(record.Id);
                OnPropertyChanged(nameof(SelectedRecordIdsDisplay));
            }
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 既存のRecordIdsをすべて削除
                await _repository.DeleteAllTimerRecordIdsAsync(_timer.ID);

                // 新しいRecordIdsを追加
                foreach (var recordId in _selectedRecordIds)
                {
                    await _repository.AddTimerRecordIdAsync(_timer.ID, recordId);
                }

                // ViewModelを更新
                _timer.RecordIds = _selectedRecordIds.ToList();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            // フィルターされたレコードのみを選択
            foreach (var item in _filteredRecords)
            {
                if (item is RecordItem record && !record.IsSelected)
                {
                    record.IsSelected = true;
                    if (!_selectedRecordIds.Contains(record.Id))
                    {
                        _selectedRecordIds.Add(record.Id);
                    }
                }
            }
            OnPropertyChanged(nameof(SelectedRecordIdsDisplay));
        }
        
        private void UnselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            // フィルターされたレコードのみを解除
            foreach (var item in _filteredRecords)
            {
                if (item is RecordItem record && record.IsSelected)
                {
                    record.IsSelected = false;
                    _selectedRecordIds.Remove(record.Id);
                }
            }
            OnPropertyChanged(nameof(SelectedRecordIdsDisplay));
        }
        
        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterText = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
