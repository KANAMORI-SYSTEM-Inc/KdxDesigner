using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
using System.Windows;

namespace KdxDesigner.Views
{
    /// <summary>
    /// MemorySettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MemorySettingWindow : Window
    {
        /// <summary>
        /// MemorySettingWindowのコンストラクタ
        /// </summary>
        /// <param name="repository">Supabaseリポジトリ</param>
        /// <param name="mainViewModel">MainViewModel</param>
        public MemorySettingWindow(ISupabaseRepository repository, MainViewModel mainViewModel)
        {
            InitializeComponent();

            // ViewModelを設定
            DataContext = new MemorySettingViewModel(repository, mainViewModel);
        }
    }
}
