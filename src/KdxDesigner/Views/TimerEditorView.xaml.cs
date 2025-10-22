using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
using System.Windows;

namespace KdxDesigner.Views
{
    public partial class TimerEditorView : Window
    {
        public TimerEditorView(ISupabaseRepository repository, MainViewModel mainViewModel)
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