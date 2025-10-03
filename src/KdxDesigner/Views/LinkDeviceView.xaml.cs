using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;

using System.Windows;

namespace KdxDesigner.Views
{
    public partial class LinkDeviceView : Window
    {
        public LinkDeviceView(ISupabaseRepository repository) // メインウィンドウからリポジトリを受け取る
        {
            InitializeComponent();
            DataContext = new LinkDeviceViewModel(repository);
        }
    }
}