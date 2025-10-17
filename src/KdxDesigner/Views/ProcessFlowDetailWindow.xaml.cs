using Kdx.Infrastructure.Supabase.Repositories;
using Kdx.Contracts.DTOs;
using KdxDesigner.ViewModels;
using KdxDesigner.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.ComponentModel;

namespace KdxDesigner.Views
{
    public partial class ProcessFlowDetailWindow : Window
    {
        private ProcessFlowDetailViewModel _viewModel;
        private ScrollViewer? _scrollViewer;
        private bool _isPanning = false;
        private Point _lastPanPoint;
        private Point _panStartScrollOffset;
        private ProcessDetailPropertiesWindow? _propertiesWindow;
        private ProcessDetailPropertiesViewModel? _propertiesViewModel;
        private ConnectionInfoWindow? _connectionInfoWindow;

        // 既存のコンストラクタ（後方互換性のため）
        public ProcessFlowDetailWindow(ISupabaseRepository repository, int cycleId, string cycleName, int? plcId = null)
            : this(repository, cycleId, cycleName, null, null, null, plcId)
        {
        }

        // MainViewModelから既存データを受け取る最適化されたコンストラクタ
        public ProcessFlowDetailWindow(
            ISupabaseRepository repository,
            int cycleId,
            string cycleName,
            List<Process>? allProcesses,
            List<ProcessDetail>? allProcessDetails,
            List<ProcessDetailCategory>? categories,
            int? plcId = null)
        {
            InitializeComponent();
            _viewModel = new ProcessFlowDetailViewModel(repository, cycleId, cycleName, allProcesses, allProcessDetails, categories, plcId);
            DataContext = _viewModel;
            _viewModel.LoadNodesAsync();

            // RequestCloseイベントをサブスクライブ
            _viewModel.RequestClose += () => Close();

            // Loadedイベントでコントロールを取得
            Loaded += OnLoaded;
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // ScrollViewerを取得
            _scrollViewer = FindName("ProcessFlowScrollViewer") as ScrollViewer;
            if (_scrollViewer != null)
            {
                _scrollViewer.PreviewMouseDown += OnScrollViewerMouseDown;
                _scrollViewer.PreviewMouseMove += OnScrollViewerMouseMove;
                _scrollViewer.PreviewMouseUp += OnScrollViewerMouseUp;
                _scrollViewer.PreviewMouseWheel += OnScrollViewerMouseWheel;
            }

            // ViewModelのSelectedNodeプロパティの変更を監視
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // 接続選択イベントを監視
            _viewModel.ConnectionSelected += OnConnectionSelected;

            // 接続削除イベントを監視
            _viewModel.ConnectionDeleted += OnConnectionDeleted;

            // プロパティウィンドウ表示要求イベントを監視（ダブルクリック時）
            _viewModel.RequestShowPropertiesWindow += OnRequestShowPropertiesWindow;
        }
        
        private void OnScrollViewerMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed && _scrollViewer != null)
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(_scrollViewer);
                _panStartScrollOffset = new Point(_scrollViewer.HorizontalOffset, _scrollViewer.VerticalOffset);
                _scrollViewer.Cursor = Cursors.ScrollAll;
                _scrollViewer.CaptureMouse();
                e.Handled = true;
            }
        }
        
        private void OnScrollViewerMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && e.MiddleButton == MouseButtonState.Pressed && _scrollViewer != null)
            {
                var currentPoint = e.GetPosition(_scrollViewer);
                var deltaX = currentPoint.X - _lastPanPoint.X;
                var deltaY = currentPoint.Y - _lastPanPoint.Y;
                
                _scrollViewer.ScrollToHorizontalOffset(_panStartScrollOffset.X - deltaX);
                _scrollViewer.ScrollToVerticalOffset(_panStartScrollOffset.Y - deltaY);
                
                e.Handled = true;
            }
        }
        
        private void OnScrollViewerMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released && _isPanning && _scrollViewer != null)
            {
                _isPanning = false;
                _scrollViewer.Cursor = Cursors.Arrow;
                _scrollViewer.ReleaseMouseCapture();
                e.Handled = true;
            }
        }
        
        private void OnScrollViewerMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrlキーが押されているかチェック
            if (Keyboard.Modifiers == ModifierKeys.Control && _scrollViewer != null)
            {
                // マウスの位置を取得（ズームの中心点として使用）
                var mousePosition = e.GetPosition(_scrollViewer);
                
                // 現在のスクロール位置を記録
                var scrollOffsetX = _scrollViewer.HorizontalOffset;
                var scrollOffsetY = _scrollViewer.VerticalOffset;
                
                // ズーム前のマウス位置（コンテンツ座標系）
                var contentX = scrollOffsetX + mousePosition.X;
                var contentY = scrollOffsetY + mousePosition.Y;
                
                // ズーム処理（生成されたコマンドを使用）
                if (e.Delta > 0)
                {
                    _viewModel.ZoomInCommand.Execute(null);
                }
                else if (e.Delta < 0)
                {
                    _viewModel.ZoomOutCommand.Execute(null);
                }
                
                // ズーム後、マウス位置を中心にスクロール位置を調整
                // これは次のフレームで実行する必要がある（レイアウトが更新された後）
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
                {
                    if (_scrollViewer != null)
                    {
                        // ズーム後のコンテンツ座標
                        var newContentX = contentX * _viewModel.ZoomScale;
                        var newContentY = contentY * _viewModel.ZoomScale;
                        
                        // マウス位置を維持するようにスクロール位置を調整
                        _scrollViewer.ScrollToHorizontalOffset(newContentX - mousePosition.X);
                        _scrollViewer.ScrollToVerticalOffset(newContentY - mousePosition.Y);
                    }
                }));
                
                e.Handled = true;
            }
        }
        
        private void ShowPropertiesWindow()
        {
            // SelectedNodeが null または ProcessDetailノード以外の場合は何もしない
            if (_viewModel.SelectedNode == null ||
                _viewModel.SelectedNode.NodeType != Models.ProcessFlowNodeType.ProcessDetail ||
                _viewModel.SelectedNode.ProcessDetail == null)
            {
                return;
            }

            if (_propertiesWindow == null || !_propertiesWindow.IsLoaded)
            {
                // ProcessDetailPropertiesViewModelを作成
                var repository = _viewModel.GetType().GetField("_repository",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(_viewModel) as ISupabaseRepository;

                if (repository == null) return;

                _propertiesViewModel = new ProcessDetailPropertiesViewModel(
                    repository,
                    _viewModel.SelectedNode.ProcessDetail,
                    _viewModel.SelectedNode.ProcessDetail.CycleId);

                // ProcessDetail保存イベントを購読
                _propertiesViewModel.ProcessDetailSaved += OnProcessDetailSaved;

                _propertiesWindow = new ProcessDetailPropertiesWindow
                {
                    DataContext = _propertiesViewModel,
                    Owner = this
                };

                // ウィンドウの位置を設定（メインウィンドウの右側）
                // 画面の境界をチェックして、画面外に出ないようにする
                var screenWidth = SystemParameters.WorkArea.Width;
                var screenHeight = SystemParameters.WorkArea.Height;

                var proposedLeft = this.Left + this.Width + 10;
                var proposedTop = this.Top;

                // 画面右端を超える場合は、メインウィンドウの左側に表示
                if (proposedLeft + _propertiesWindow.Width > screenWidth)
                {
                    proposedLeft = Math.Max(0, this.Left - _propertiesWindow.Width - 10);
                }

                // 画面下端を超える場合は調整
                if (proposedTop + _propertiesWindow.Height > screenHeight)
                {
                    proposedTop = Math.Max(0, screenHeight - _propertiesWindow.Height);
                }

                _propertiesWindow.Left = proposedLeft;
                _propertiesWindow.Top = proposedTop;

                // ウィンドウが閉じられたときの処理
                _propertiesWindow.Closed += (s, e) =>
                {
                    _propertiesWindow = null;
                    _propertiesViewModel = null;
                };

                _propertiesWindow.Show();

                // ウィンドウを前面に表示
                _propertiesWindow.Activate();
            }
        }
        
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // ノードが選択されたときの処理
            if (e.PropertyName == nameof(ProcessFlowDetailViewModel.SelectedNode))
            {
                // 既にプロパティウィンドウが開いている場合のみ、内容を更新
                if (_propertiesWindow != null && _propertiesWindow.IsLoaded &&
                    _viewModel.SelectedNode != null &&
                    _viewModel.SelectedNode.NodeType == Models.ProcessFlowNodeType.ProcessDetail &&
                    _viewModel.SelectedNode.ProcessDetail != null)
                {
                    // ウィンドウが既に開いている場合は、ProcessDetailを更新
                    if (_propertiesViewModel != null)
                    {
                        _propertiesViewModel.UpdateProcessDetail(_viewModel.SelectedNode.ProcessDetail);
                    }

                    // ウィンドウが最小化されている場合は元に戻す
                    if (_propertiesWindow.WindowState == WindowState.Minimized)
                    {
                        _propertiesWindow.WindowState = WindowState.Normal;
                    }
                    _propertiesWindow.Show();
                    _propertiesWindow.Activate();
                    _propertiesWindow.Focus();
                }
            }
        }
        
        private void OnConnectionSelected(object? sender, EventArgs e)
        {
            if (_viewModel.SelectedConnection != null)
            {
                // 既存の接続情報ウィンドウがあれば閉じる
                _connectionInfoWindow?.Close();

                // 新しい接続情報ウィンドウを作成して表示
                _connectionInfoWindow = new ConnectionInfoWindow
                {
                    DataContext = _viewModel,
                    Owner = this
                };

                _connectionInfoWindow.Closed += (s, args) => _connectionInfoWindow = null;
                _connectionInfoWindow.ShowDialog();
            }
        }

        private void OnConnectionDeleted(object? sender, EventArgs e)
        {
            // 接続が削除されたら、接続情報ウィンドウを閉じる
            _connectionInfoWindow?.Close();
        }

        private void OnRequestShowPropertiesWindow(object? sender, EventArgs e)
        {
            // ダブルクリックでプロパティウィンドウを表示
            ShowPropertiesWindow();
        }

        private void OnProcessDetailSaved(object? sender, ProcessDetail updatedProcessDetail)
        {
            // ProcessDetailが保存されたら、ViewModelのノード表示を更新
            // Dispatcherで遅延実行することで、編集中のトランザクションとの競合を避ける
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                _viewModel.UpdateNodeFromProcessDetail(updatedProcessDetail);
            }));
        }

        private void Node_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ProcessFlowNode node)
            {
                // ダブルクリック（ClickCount == 2）の場合はドラッグ処理をスキップ
                if (e.ClickCount == 2)
                {
                    return;
                }

                // 通常のクリックの場合はViewModelのコマンドを実行
                _viewModel.NodeMouseDownCommand.Execute(node);
            }
        }

        private void Node_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ProcessFlowNode node)
            {
                _viewModel.NodeMouseUpCommand.Execute(node);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // プロパティウィンドウも閉じる
            _propertiesWindow?.Close();
            
            // 接続情報ウィンドウも閉じる
            _connectionInfoWindow?.Close();
            
            // イベントハンドラーの解除
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.ConnectionSelected -= OnConnectionSelected;
                _viewModel.ConnectionDeleted -= OnConnectionDeleted;
                _viewModel.RequestShowPropertiesWindow -= OnRequestShowPropertiesWindow;
            }
            
            if (_scrollViewer != null)
            {
                _scrollViewer.PreviewMouseDown -= OnScrollViewerMouseDown;
                _scrollViewer.PreviewMouseMove -= OnScrollViewerMouseMove;
                _scrollViewer.PreviewMouseUp -= OnScrollViewerMouseUp;
                _scrollViewer.PreviewMouseWheel -= OnScrollViewerMouseWheel;
            }
        }
    }
}