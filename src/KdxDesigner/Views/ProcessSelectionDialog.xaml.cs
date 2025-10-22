using Kdx.Contracts.DTOs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace KdxDesigner.Views
{
    public partial class ProcessSelectionDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<Process> _processes = new();
        private Process? _selectedProcess;

        public ObservableCollection<Process> Processes
        {
            get => _processes;
            set
            {
                _processes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Processes)));
            }
        }

        public Process? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                _selectedProcess = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedProcess)));
            }
        }

        public ProcessSelectionDialog(List<Process> processes, int currentProcessId)
        {
            InitializeComponent();
            DataContext = this;

            Processes = new ObservableCollection<Process>(processes);

            // 現在のProcessを初期選択
            SelectedProcess = Processes.FirstOrDefault(p => p.Id == currentProcessId);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProcess == null)
            {
                MessageBox.Show("Processを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
