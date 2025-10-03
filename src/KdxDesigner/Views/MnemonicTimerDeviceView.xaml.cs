using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
using System.Windows;

namespace KdxDesigner.Views
{
    public partial class MnemonicTimerDeviceView : Window
    {
        public MnemonicTimerDeviceView(ISupabaseRepository repository, MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new MnemonicTimerDeviceListViewModel(repository, mainViewModel);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}