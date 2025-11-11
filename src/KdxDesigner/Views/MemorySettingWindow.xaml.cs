using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Models;
using KdxDesigner.ViewModels;
using KdxDesigner.ViewModels.Settings;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

        /// <summary>
        /// プロファイルListBoxの選択変更イベントハンドラー
        /// 複数選択されたアイテムをViewModelに同期
        /// </summary>
        private void ProfileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MemorySettingViewModel viewModel && sender is ListBox listBox)
            {
                // 選択されたアイテムをViewModelのコレクションに同期
                viewModel.SelectedCycleProfiles.Clear();
                foreach (var item in listBox.SelectedItems.OfType<CycleMemoryProfile>())
                {
                    viewModel.SelectedCycleProfiles.Add(item);
                }
            }
        }
    }
}
