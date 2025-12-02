using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows;
using Kdx.Contracts.DTOs;

namespace KdxDesigner.Models
{
    public enum ConnectionType
    {
        Normal,
        Start,
        Finish,
        ProcessToDetail  // Process-ProcessDetail間の接続
    }

    public partial class ProcessFlowConnection : ObservableObject
    {
        [ObservableProperty] private ProcessFlowNode _fromNode;
        [ObservableProperty] private ProcessFlowNode _toNode;
        [ObservableProperty] private bool _isHighlighted;
        [ObservableProperty] private bool _isModified;
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isFinishConnection;
        [ObservableProperty] private ConnectionType _connectionType = ConnectionType.Normal;
        [ObservableProperty] private string? _finishSensor;
        [ObservableProperty] private string? _startSensorValue;
        
        public ProcessFlowConnection(ProcessFlowNode from, ProcessFlowNode to, bool isModified = false)
        {
            _fromNode = from;
            _toNode = to;
            _isModified = isModified;
            
            // ノードの位置が変更されたときに通知を受け取る
            from.PropertyChanged += OnNodePropertyChanged;
            to.PropertyChanged += OnNodePropertyChanged;
        }
        
        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProcessFlowNode.Position) || 
                e.PropertyName == nameof(ProcessFlowNode.NodeHeight))
            {
                OnPropertyChanged(nameof(StartPoint));
                OnPropertyChanged(nameof(EndPoint));
                OnPropertyChanged(nameof(ArrowAngle));
                OnPropertyChanged(nameof(ArrowX));
                OnPropertyChanged(nameof(ArrowY));
                OnPropertyChanged(nameof(MidPoint));
            }
        }
        
        public Point StartPoint
        {
            get
            {
                // 終了条件も通常接続も同じ向き：FromNodeから出発
                var nodeWidth = FromNode.NodeWidth;
                var nodeHeight = FromNode.NodeHeight;
                var fromCenter = new Point(FromNode.Position.X + nodeWidth / 2, FromNode.Position.Y + nodeHeight / 2);
                var toCenter = new Point(ToNode.Position.X + ToNode.NodeWidth / 2, ToNode.Position.Y + ToNode.NodeHeight / 2);

                // 接続線の角度を計算
                var angle = Math.Atan2(toCenter.Y - fromCenter.Y, toCenter.X - fromCenter.X);

                // ノードの端の点を計算
                var edgeX = FromNode.Position.X + nodeWidth; // ノードの右端
                var edgeY = FromNode.Position.Y + nodeHeight / 2;  // ノードの中央Y

                // 角度に基づいて適切な端点を選択
                if (Math.Abs(angle) <= Math.PI / 4) // 右方向
                {
                    return new Point(edgeX, edgeY);
                }
                else if (angle > Math.PI / 4 && angle <= 3 * Math.PI / 4) // 下方向
                {
                    return new Point(fromCenter.X, FromNode.Position.Y + nodeHeight);
                }
                else if (angle < -Math.PI / 4 && angle >= -3 * Math.PI / 4) // 上方向
                {
                    return new Point(fromCenter.X, FromNode.Position.Y);
                }
                else // 左方向
                {
                    return new Point(FromNode.Position.X, edgeY);
                }
            }
        }
        
        public Point EndPoint
        {
            get
            {
                // 終了条件も通常接続も同じ向き：ToNodeに到着
                var nodeWidth = ToNode.NodeWidth;
                var nodeHeight = ToNode.NodeHeight;
                var fromCenter = new Point(FromNode.Position.X + FromNode.NodeWidth / 2, FromNode.Position.Y + FromNode.NodeHeight / 2);
                var toCenter = new Point(ToNode.Position.X + nodeWidth / 2, ToNode.Position.Y + nodeHeight / 2);

                // 接続線の角度を計算（逆方向）
                var angle = Math.Atan2(fromCenter.Y - toCenter.Y, fromCenter.X - toCenter.X);

                // ノードの端の点を計算
                if (Math.Abs(angle) <= Math.PI / 4) // 右方向から来る
                {
                    return new Point(ToNode.Position.X + nodeWidth, toCenter.Y);
                }
                else if (angle > Math.PI / 4 && angle <= 3 * Math.PI / 4) // 下方向から来る
                {
                    return new Point(toCenter.X, ToNode.Position.Y + nodeHeight);
                }
                else if (angle < -Math.PI / 4 && angle >= -3 * Math.PI / 4) // 上方向から来る
                {
                    return new Point(toCenter.X, ToNode.Position.Y);
                }
                else // 左方向から来る
                {
                    return new Point(ToNode.Position.X, toCenter.Y);
                }
            }
        }
        
        // 矢印の角度を計算
        public double ArrowAngle
        {
            get
            {
                var dx = EndPoint.X - StartPoint.X;
                var dy = EndPoint.Y - StartPoint.Y;
                return Math.Atan2(dy, dx) * 180 / Math.PI;
            }
        }
        
        // 矢印の位置（終点から少し手前）
        public double ArrowX => EndPoint.X - 8 * Math.Cos(ArrowAngle * Math.PI / 180) - 4;
        public double ArrowY => EndPoint.Y - 8 * Math.Sin(ArrowAngle * Math.PI / 180) - 4;
        
        // 線の中点
        public Point MidPoint => new Point(
            (StartPoint.X + EndPoint.X) / 2,
            (StartPoint.Y + EndPoint.Y) / 2
        );
        
        // ToNodeのStartSensorを表示
        public string StartSensor => ToNode?.ProcessDetail?.StartSensor ?? "";
        
        // データベース側のStartSensor値を保持
        public string? DbStartSensor { get; set; }
        
        // 位置を更新するメソッド（自動的に呼ばれる）
        public void UpdatePosition()
        {
            OnPropertyChanged(nameof(StartPoint));
            OnPropertyChanged(nameof(EndPoint));
            OnPropertyChanged(nameof(ArrowAngle));
            OnPropertyChanged(nameof(ArrowX));
            OnPropertyChanged(nameof(ArrowY));
            OnPropertyChanged(nameof(MidPoint));
        }
        
        // 他サイクルへの接続かどうかを示すプロパティ
        [ObservableProperty] private bool _isOtherCycleConnection = false;
    }
}