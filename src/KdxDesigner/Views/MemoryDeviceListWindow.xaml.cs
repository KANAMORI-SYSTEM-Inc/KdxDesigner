using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.ViewModels;
using System.Windows;

namespace KdxDesigner.Views
{
    /// <summary>
    /// MemoryDeviceListWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MemoryDeviceListWindow : Window
    {
        public MemoryDeviceListWindow(IMnemonicDeviceMemoryStore memoryStore, int? plcId = null, int? cycleId = null)
        {
            InitializeComponent();
            DataContext = new MemoryDeviceListViewModel(memoryStore, plcId, cycleId);
        }
    }
}