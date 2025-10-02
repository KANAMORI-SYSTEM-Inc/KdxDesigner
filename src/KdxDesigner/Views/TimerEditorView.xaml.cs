using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;
using System.Windows;

namespace KdxDesigner.Views
{
    public partial class TimerEditorView : Window
    {
        public TimerEditorView(IAccessRepository repository, MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new TimerEditorViewModel(repository, mainViewModel);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}