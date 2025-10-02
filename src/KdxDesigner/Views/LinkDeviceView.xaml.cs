using Kdx.Contracts.Interfaces;
using KdxDesigner.ViewModels;

using System.Windows;

namespace KdxDesigner.Views
{
    public partial class LinkDeviceView : Window
    {
        public LinkDeviceView(IAccessRepository repository) // メインウィンドウからリポジトリを受け取る
        {
            InitializeComponent();
            DataContext = new LinkDeviceViewModel(repository);
        }
    }
}