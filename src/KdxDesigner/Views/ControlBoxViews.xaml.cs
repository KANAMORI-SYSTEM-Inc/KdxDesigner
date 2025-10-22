using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
using System.Windows;

namespace KdxDesigner.Views
{
    /// <summary>
    /// ControlBoxVies.xaml の相互作用ロジック
    /// </summary>
    public partial class ControlBoxViews : Window
    {
        public ControlBoxViews(ISupabaseRepository repo, int plcId)
        {
            InitializeComponent();

            // 非同期初期化を行う
            Loaded += async (s, e) =>
            {
                DataContext = await ControlBoxViewModel.CreateAsync(repo, plcId);
            };
        }
    }
}
