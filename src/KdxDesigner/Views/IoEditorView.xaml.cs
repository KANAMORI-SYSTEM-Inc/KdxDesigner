using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;

using System.Windows;

namespace KdxDesigner.Views
{
    public partial class IoEditorView : Window
    {
        public IoEditorView(IAccessRepository repository, MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new IoEditorViewModel(repository, mainViewModel);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}