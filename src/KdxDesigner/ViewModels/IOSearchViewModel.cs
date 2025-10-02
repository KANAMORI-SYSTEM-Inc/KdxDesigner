using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Kdx.Contracts.DTOs;
using Kdx.Contracts.Interfaces;
using KdxDesigner.Utils;

namespace KdxDesigner.ViewModels
{
    public class IOSearchViewModel : INotifyPropertyChanged
    {
        private readonly IAccessRepository _repository;
        private readonly int _plcId;
        private string? _searchText;
        private IO? _selectedIO;
        private readonly ObservableCollection<IO> _allIOs;
        private readonly ICollectionView _filteredIOs;

        public IOSearchViewModel(IAccessRepository repository, int plcId, string? initialSearchText = null)
        {
            _repository = repository;
            _plcId = plcId;
            _allIOs = new ObservableCollection<IO>();
            _filteredIOs = CollectionViewSource.GetDefaultView(_allIOs);
            _filteredIOs.Filter = FilterIO;

            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);

            LoadIOs();

            // Set initial search text if provided
            if (!string.IsNullOrEmpty(initialSearchText))
            {
                SearchText = initialSearchText;
            }
        }

        public string? SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _filteredIOs.Refresh();
            }
        }

        public IO? SelectedIO
        {
            get => _selectedIO;
            set
            {
                _selectedIO = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView FilteredIOs => _filteredIOs;

        public ICommand ClearSearchCommand { get; }

        private void LoadIOs()
        {
            // Load all IOs from all PLCs to allow cross-PLC linking
            var ios = _repository.GetIoList();
            _allIOs.Clear();
            foreach (var io in ios)
            {
                _allIOs.Add(io);
            }
        }

        private bool FilterIO(object obj)
        {
            if (obj is not IO io) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var searchLower = SearchText.ToLower();
            return (io.Address?.ToLower().Contains(searchLower) ?? false) ||
                   (io.IOName?.ToLower().Contains(searchLower) ?? false) ||
                   (io.IOExplanation?.ToLower().Contains(searchLower) ?? false) ||
                   (io.IOText?.ToLower().Contains(searchLower) ?? false) ||
                   (io.LinkDevice?.ToLower().Contains(searchLower) ?? false) ||
                   (io.IOSpot?.ToLower().Contains(searchLower) ?? false) ||
                   io.PlcId.ToString().Contains(searchLower);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}