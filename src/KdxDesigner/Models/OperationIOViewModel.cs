using CommunityToolkit.Mvvm.ComponentModel;

namespace KdxDesigner.Models
{
    /// <summary>
    /// 関連付け済みIOリストに表示するためのViewModel
    /// </summary>
    public partial class OperationIOViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _operationId;

        [ObservableProperty]
        private string _iOAddress = string.Empty;

        [ObservableProperty]
        private int _plcId;

        [ObservableProperty]
        private string _iOUsage = string.Empty;

        [ObservableProperty]
        private string? _iOName;

        [ObservableProperty]
        private string? _iOExplanation;

        [ObservableProperty]
        private string? _comment;

        [ObservableProperty]
        private string? _operationName;
    }
}
