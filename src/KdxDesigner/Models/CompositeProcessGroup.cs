using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace KdxDesigner.Models
{
    public partial class CompositeProcessGroup : ObservableObject
    {
        [ObservableProperty] private int _blockNumber;
        [ObservableProperty] private string _processName;
        [ObservableProperty] private Rect _bounds;
        
        public CompositeProcessGroup(int blockNumber, string processName, Rect bounds)
        {
            _blockNumber = blockNumber;
            _processName = processName;
            _bounds = bounds;
        }
    }
}