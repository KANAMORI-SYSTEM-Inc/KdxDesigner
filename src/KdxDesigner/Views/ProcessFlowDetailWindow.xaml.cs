using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.ViewModels;
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
        private ConnectionInfoWindow? _connectionInfoWindow;
        
        public ProcessFlowDetailWindow(ISupabaseRepository repository, int cycleId, string cycleName)
        {
            InitializeComponent();
            _viewModel = new ProcessFlowDetailViewModel(repository, cycleId, cycleName);
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
            
            // プロパティウィンドウを開く
            ShowPropertiesWindow();
            
            // ViewModelのSelectedNodeプロパティの変更を監視
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // 接続選択イベントを監視
            _viewModel.ConnectionSelected += OnConnectionSelected;
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
                
                // ズーム処理
                if (e.Delta > 0)
                {
                    _viewModel.ZoomIn();
                }
                else if (e.Delta < 0)
                {
                    _viewModel.ZoomOut();
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
            if (_propertiesWindow == null || !_propertiesWindow.IsLoaded)
            {
                _propertiesWindow = new ProcessDetailPropertiesWindow
                {
                    DataContext = _viewModel,
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
                _propertiesWindow.Closed += (s, e) => _propertiesWindow = null;
                
                _propertiesWindow.Show();
                
                // ウィンドウを前面に表示
                _propertiesWindow.Activate();
            }
        }
        
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // ノードが選択されたときにプロパティウィンドウを表示
            if (e.PropertyName == nameof(ProcessFlowDetailViewModel.SelectedNode))
            {
                if (_viewModel.SelectedNode != null)
                {
                    // ノードが選択された場合
                    if (_propertiesWindow == null || !_propertiesWindow.IsLoaded)
                    {
                        ShowPropertiesWindow();
                    }
                    else
                    {
                        // ウィンドウが既に開いている場合は、確実に表示してアクティブにする
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