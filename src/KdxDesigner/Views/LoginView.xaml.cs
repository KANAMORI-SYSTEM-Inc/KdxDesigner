using KdxDesigner.ViewModels;
using System.Windows;

namespace KdxDesigner.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            
            // ViewModelをDIコンテナから取得
            var viewModel = App.Services?.GetService(typeof(LoginViewModel)) as LoginViewModel;
            DataContext = viewModel;
        }
    }
}