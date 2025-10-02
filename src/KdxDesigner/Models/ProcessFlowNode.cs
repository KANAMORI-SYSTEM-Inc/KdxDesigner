using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using Kdx.Contracts.DTOs;
using Process = Kdx.Contracts.DTOs.Process;

namespace KdxDesigner.Models
{
    public enum ProcessFlowNodeType
    {
        ProcessDetail,
        Process
    }

    public partial class ProcessFlowNode : ObservableObject
    {
        [ObservableProperty] private ProcessDetail? _processDetail;
        [ObservableProperty] private Process? _process;
        [ObservableProperty] private Point _position;
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isDragging;
        [ObservableProperty] private double _opacity = 1.0;
        [ObservableProperty] private bool _isModified = false;
        [ObservableProperty] private ProcessFlowNodeType _nodeType;
        
        // ProcessDetailノード用のコンストラクタ
        public ProcessFlowNode(ProcessDetail detail, Point position)
        {
            _processDetail = detail;
            _position = position;
            _nodeType = ProcessFlowNodeType.ProcessDetail;
        }
        
        // Processノード用のコンストラクタ
        public ProcessFlowNode(Process process, Point position)
        {
            _process = process;
            _position = position;
            _nodeType = ProcessFlowNodeType.Process;
        }
        
        public int Id => NodeType == ProcessFlowNodeType.ProcessDetail ? ProcessDetail?.Id ?? 0 : Process?.Id ?? 0;
        public string DisplayName => NodeType == ProcessFlowNodeType.ProcessDetail 
            ? (ProcessDetail?.DetailName ?? $"工程詳細 {ProcessDetail?.Id}")
            : (Process?.ProcessName ?? $"工程 {Process?.Id}");
        public string StartSensor => ProcessDetail?.StartSensor ?? "";
        public int? CategoryId => ProcessDetail?.CategoryId;
        
        // Detailプロパティへのショートカット（ViewModelとの互換性のため）
        public ProcessDetail? Detail => ProcessDetail;
        
        // カテゴリ名を表示するためのプロパティ
        [ObservableProperty] private string? _categoryName;
        
        // 複合工程名を表示するためのプロパティ（BlockNumberが複合工程IDの場合）
        [ObservableProperty] private string? _compositeProcessName;
        
        // 表示名を更新するメソッド
        public void UpdateDisplayName()
        {
            OnPropertyChanged(nameof(DisplayName));
        }
        
        // 開始センサーが設定されているかどうか
        public bool HasStartSensor => !string.IsNullOrEmpty(ProcessDetail?.StartSensor);
        
        // 開始センサーは設定されているがタイマーが未設定かどうか
        public bool HasStartSensorWithoutTimer => HasStartSensor && ProcessDetail?.StartTimerId == null;
        
        // プロパティ変更通知メソッド
        public void NotifyStartSensorPropertiesChanged()
        {
            OnPropertyChanged(nameof(HasStartSensor));
            OnPropertyChanged(nameof(HasStartSensorWithoutTimer));
        }
        
        // 他サイクルのノードかどうかを示すプロパティ
        [ObservableProperty] private bool _isOtherCycleNode = false;
        
        // ノードの現在の高さ（表示オプションに応じて変動）
        [ObservableProperty] private double _nodeHeight = 45;
        
        // ノードの幅（ノードタイプによって変更）
        public double NodeWidth => NodeType == ProcessFlowNodeType.Process ? 200 : 160;
    }
}