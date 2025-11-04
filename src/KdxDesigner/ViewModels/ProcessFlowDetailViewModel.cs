using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using KdxDesigner.Models;
using KdxDesigner.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Process = Kdx.Contracts.DTOs.Process;
using Timer = Kdx.Contracts.DTOs.Timer;

namespace KdxDesigner.ViewModels
{
    public partial class ProcessFlowDetailViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private readonly int _cycleId;
        private readonly int? _plcId;

        // MainViewModelから受け取った既存データのキャッシュ
        private List<Process>? _cachedAllProcesses;
        private List<ProcessDetail>? _cachedAllProcessDetails;
        private List<ProcessDetailCategory>? _cachedCategories;

        private ProcessFlowNode? _draggedNode;
        private Point _dragOffset;
        private ProcessFlowNode? _connectionStartNode;
        private bool _isCreatingFinishConnection;
        private bool _isSelecting;
        private Point _selectionStartPoint;
        private bool _hasMouseMoved;
        private List<ProcessFlowNode> _selectedNodes = new();
        private Dictionary<ProcessFlowNode, Point> _dragOffsets = new();
        private ProcessPropertiesWindow? _openProcessWindow;

        [ObservableProperty] private string _windowTitle;
        [ObservableProperty] private string _cycleName;
        [ObservableProperty] private ObservableCollection<ProcessFlowNode> _nodes = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _connections = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowNode> _allNodes = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _allConnections = new();
        [ObservableProperty] private ProcessFlowNode? _selectedNode;
        [ObservableProperty] private ProcessFlowConnection? _selectedConnection;

        // 選択されたノードのプロパティ
        [ObservableProperty] private string _selectedNodeDetailName = "";
        [ObservableProperty] private int? _selectedNodeOperationId;
        [ObservableProperty] private string _selectedNodeStartSensor = "";
        [ObservableProperty] private string _selectedNodeFinishSensor = "";
        [ObservableProperty] private int? _selectedNodeCategoryId;
        [ObservableProperty] private int? _selectedNodeBlockNumber;
        [ObservableProperty] private string _selectedNodeSkipMode = "";
        [ObservableProperty] private int? _selectedNodeSortNumber;
        [ObservableProperty] private string _selectedNodeComment = "";
        [ObservableProperty] private string _selectedNodeILStart = "";
        [ObservableProperty] private int? _selectedNodeId;
        [ObservableProperty] private int? _selectedNodeStartTimerId;
        [ObservableProperty] private bool? _selectedNodeIsResetAfter;
        [ObservableProperty] private string _selectedNodeDisplayName = "";
        [ObservableProperty] private bool _isNodeSelected = false;
        [ObservableProperty] private int? _selectedNodeProcessId;
        [ObservableProperty] private ObservableCollection<Process> _processes = new();

        [ObservableProperty] private Point _mousePosition;
        [ObservableProperty] private bool _isConnecting;
        [ObservableProperty] private Point _connectionStartPoint;
        [ObservableProperty] private bool _isFiltered = false;
        [ObservableProperty] private ProcessFlowNode? _filterNode;
        [ObservableProperty] private double _canvasWidth = 3000;
        [ObservableProperty] private double _canvasHeight = 3000;
        [ObservableProperty] private bool _isRectangleSelecting = false;
        [ObservableProperty] private Rect _selectionRectangle = new(-1, -1, 0, 0);
        [ObservableProperty] private ObservableCollection<ProcessDetailCategory> _categories = new();
        [ObservableProperty] private ObservableCollection<CompositeProcessGroup> _compositeGroups = new();
        [ObservableProperty] private ObservableCollection<Operation> _operations = new();
        [ObservableProperty] private ObservableCollection<Operation> _filteredOperations = new();
        [ObservableProperty] private ObservableCollection<Timer> _availableTimers = new();
        [ObservableProperty] private ObservableCollection<Timer> _filteredTimers = new();
        [ObservableProperty] private string _timerFilterText = "";
        [ObservableProperty] private bool _showOnlyOperationTimers = false;
        [ObservableProperty] private bool _highlightStartSensor = false;
        [ObservableProperty] private bool _highlightStartSensorWithoutTimer = false;
        [ObservableProperty] private bool _canChangeConnectionType = false;
        [ObservableProperty] private bool _showAllConnections = false;
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _incomingConnections = new();
        [ObservableProperty] private ObservableCollection<ProcessFlowConnection> _outgoingConnections = new();
        [ObservableProperty] private bool _hasOtherCycleConnections = false;
        [ObservableProperty] private bool _isLoading = false;
        private bool _showNodeId = false;
        private bool _showBlockNumber = false;

        // ズーム機能用のプロパティ
        [ObservableProperty] private double _zoomScale = 1.0;
        private const double _minZoomScale = 0.1;
        private const double _maxZoomScale = 5.0;
        private const double _zoomStep = 0.1;

        public bool ShowNodeId
        {
            get => _showNodeId;
            set
            {
                if (SetProperty(ref _showNodeId, value))
                {
                    UpdateNodeHeights();
                }
            }
        }

        public bool ShowBlockNumber
        {
            get => _showBlockNumber;
            set
            {
                if (SetProperty(ref _showBlockNumber, value))
                {
                    UpdateNodeHeights();
                }
            }
        }

        private void UpdateNodeHeights()
        {
            double height = 45;
            if (ShowNodeId && ShowBlockNumber)
                height = 60;
            else if (ShowNodeId || ShowBlockNumber)
                height = 55;

            foreach (var node in AllNodes)
            {
                node.NodeHeight = height;
            }

            // 接続線の位置を更新
            foreach (var connection in AllConnections)
            {
                connection.UpdatePosition();
            }
        }

        private List<ProcessDetailConnection> _dbConnections = new();
        private List<ProcessDetailFinish> _dbFinishes = new();
        private List<ProcessStartCondition> _processStartConditions = new();
        private List<ProcessFinishCondition> _processFinishConditions = new();
        private Dictionary<int, Point> _originalPositions = new();

        private void CreateProcessConnections(
            Dictionary<int, ProcessFlowNode> processNodeDict,
            Dictionary<int, ProcessFlowNode> detailNodeDict,
            List<ProcessStartCondition> startConditions,
            List<ProcessFinishCondition> finishConditions)
        {
            System.Diagnostics.Debug.WriteLine($"========== CreateProcessConnections START ==========");
            System.Diagnostics.Debug.WriteLine($"Process nodes count: {processNodeDict.Count}");
            foreach (var kvp in processNodeDict)
            {
                System.Diagnostics.Debug.WriteLine($"  Process node ID={kvp.Key}, Name={kvp.Value.Process?.ProcessName}");
            }

            System.Diagnostics.Debug.WriteLine($"Detail nodes count: {detailNodeDict.Count}");
            foreach (var kvp in detailNodeDict.Take(5))  // 最初の5個だけ表示
            {
                System.Diagnostics.Debug.WriteLine($"  Detail node ID={kvp.Key}, Name={kvp.Value.ProcessDetail?.DetailName}");
            }

            System.Diagnostics.Debug.WriteLine($"Start conditions count: {startConditions.Count}");
            System.Diagnostics.Debug.WriteLine($"Finish conditions count: {finishConditions.Count}");

            // Process -> ProcessDetail (開始条件)
            int startConnectionsCreated = 0;
            foreach (var startCondition in startConditions)
            {
                System.Diagnostics.Debug.WriteLine($"Processing start condition: ProcessId={startCondition.ProcessId}, StartProcessDetailId={startCondition.StartProcessDetailId}, Sensor={startCondition.StartSensor}");

                if (processNodeDict.ContainsKey(startCondition.ProcessId) &&
                    detailNodeDict.ContainsKey(startCondition.StartProcessDetailId))
                {
                    var fromNode = processNodeDict[startCondition.ProcessId];
                    var toNode = detailNodeDict[startCondition.StartProcessDetailId];

                    var connection = new ProcessFlowConnection(fromNode, toNode)
                    {
                        ConnectionType = ConnectionType.ProcessToDetail,
                        IsFinishConnection = false,  // 開始条件接続
                        DbStartSensor = startCondition.StartSensor
                    };

                    AllConnections.Add(connection);
                    Connections.Add(connection);
                    startConnectionsCreated++;
                    System.Diagnostics.Debug.WriteLine($"✓ Created START connection: Process {fromNode.Process?.ProcessName} (ID={startCondition.ProcessId}) -> ProcessDetail {toNode.ProcessDetail?.DetailName} (ID={startCondition.StartProcessDetailId})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Skipped start condition: Process node exists={processNodeDict.ContainsKey(startCondition.ProcessId)}, Detail node exists={detailNodeDict.ContainsKey(startCondition.StartProcessDetailId)}");
                }
            }

            // ProcessDetail -> Process (終了条件)
            int finishConnectionsCreated = 0;
            foreach (var finishCondition in finishConditions)
            {
                System.Diagnostics.Debug.WriteLine($"Processing finish condition: ProcessId={finishCondition.ProcessId}, FinishProcessDetailId={finishCondition.FinishProcessDetailId}, Sensor={finishCondition.FinishSensor}");

                if (detailNodeDict.ContainsKey(finishCondition.FinishProcessDetailId) &&
                    processNodeDict.ContainsKey(finishCondition.ProcessId))
                {
                    var fromNode = detailNodeDict[finishCondition.FinishProcessDetailId];
                    var toNode = processNodeDict[finishCondition.ProcessId];

                    var connection = new ProcessFlowConnection(fromNode, toNode)
                    {
                        ConnectionType = ConnectionType.ProcessToDetail,
                        IsFinishConnection = true,  // 終了条件接続
                        DbStartSensor = finishCondition.FinishSensor
                    };

                    AllConnections.Add(connection);
                    Connections.Add(connection);
                    finishConnectionsCreated++;
                    System.Diagnostics.Debug.WriteLine($"✓ Created FINISH connection: ProcessDetail {fromNode.ProcessDetail?.DetailName} (ID={finishCondition.FinishProcessDetailId}) -> Process {toNode.Process?.ProcessName} (ID={finishCondition.ProcessId})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Skipped finish condition: Detail node exists={detailNodeDict.ContainsKey(finishCondition.FinishProcessDetailId)}, Process node exists={processNodeDict.ContainsKey(finishCondition.ProcessId)}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"========== CreateProcessConnections SUMMARY ==========");
            System.Diagnostics.Debug.WriteLine($"Start connections created: {startConnectionsCreated}/{startConditions.Count}");
            System.Diagnostics.Debug.WriteLine($"Finish connections created: {finishConnectionsCreated}/{finishConditions.Count}");
            System.Diagnostics.Debug.WriteLine($"Total connections in AllConnections: {AllConnections.Count}");
            System.Diagnostics.Debug.WriteLine($"Total connections in Connections: {Connections.Count}");
            System.Diagnostics.Debug.WriteLine($"========== CreateProcessConnections END ==========");
        }

        // 既存のコンストラクタ（後方互換性のため）
        public ProcessFlowDetailViewModel(ISupabaseRepository repository, int cycleId, string cycleName, int? plcId = null)
            : this(repository, cycleId, cycleName, null, null, null, plcId)
        {
        }

        // MainViewModelから既存データを受け取る最適化されたコンストラクタ
        public ProcessFlowDetailViewModel(
            ISupabaseRepository repository,
            int cycleId,
            string cycleName,
            List<Process>? allProcesses,
            List<ProcessDetail>? allProcessDetails,
            List<ProcessDetailCategory>? categories,
            int? plcId = null)
        {
            _repository = repository;
            _cycleId = cycleId;
            _plcId = plcId;
            CycleName = $"{cycleId} - {cycleName}";
            WindowTitle = $"工程フロー詳細 - {CycleName}";

            // キャッシュに保存
            _cachedAllProcesses = allProcesses;
            _cachedAllProcessDetails = allProcessDetails;
            _cachedCategories = categories;

            // 初期値を設定
            IsNodeSelected = false;
            SelectedNodeDisplayName = "";

            // 既存データがある場合はそれを使用、ない場合は読み込み
            if (allProcesses != null)
            {
                var cycleProcesses = allProcesses.Where(p => p.CycleId == _cycleId).OrderBy(p => p.SortNumber);
                Processes.Clear();
                foreach (var process in cycleProcesses)
                {
                    Processes.Add(process);
                }
            }
            else
            {
                LoadProcesses();
            }

            LoadOperations();
        }

        public async void LoadNodesAsync()
        {
            // ローディング開始
            IsLoading = true;

            try
            {
                // データ取得を非同期で行い、UI更新はUIスレッドで実行
                await LoadProcessDetailsInternal();
            }
            finally
            {
                // ローディング終了
                IsLoading = false;
            }
        }

        private async void LoadProcesses()
        {
            var processes = (await _repository.GetProcessesAsync())
                .Where(p => p.CycleId == _cycleId)
                .OrderBy(p => p.SortNumber)
                .ToList();

            Processes.Clear();
            foreach (var process in processes)
            {
                Processes.Add(process);
            }
        }

        private async void LoadOperations()
        {
            var operations = (await _repository.GetOperationsAsync())
                .OrderBy(o => o.Id)
                .ToList();

            Operations.Clear();
            foreach (var operation in operations)
            {
                Operations.Add(operation);
            }

            // フィルタされたOperationsを初期化
            FilteredOperations.Clear();
            foreach (var operation in operations)
            {
                FilteredOperations.Add(operation);
            }
        }

        private ProcessFlowNode CreateNode(ProcessDetail detail, Point position, List<ProcessDetailCategory> categoriesList, Dictionary<int, string> processMap)
        {
            var node = new ProcessFlowNode(detail, position);

            // カテゴリ名を設定
            if (detail.CategoryId.HasValue)
            {
                var category = categoriesList.FirstOrDefault(c => c.Id == detail.CategoryId.Value);
                node.CategoryName = category?.CategoryName;
            }

            // BlockNumberが設定されている場合、対応する工程名を設定
            if (detail.BlockNumber.HasValue && processMap.ContainsKey(detail.BlockNumber.Value))
            {
                node.CompositeProcessName = processMap[detail.BlockNumber.Value];
            }

            System.Diagnostics.Debug.WriteLine($"Added node: {node.DisplayName} at position ({position.X}, {position.Y})");

            return node;
        }

        // 接続選択時のイベント
        public event EventHandler? ConnectionSelected;

        // 接続削除完了時のイベント
        public event EventHandler? ConnectionDeleted;

        // プロパティウィンドウ表示要求イベント（ダブルクリック時）
        public event EventHandler? RequestShowPropertiesWindow;

        public async void LoadProcessDetails()
        {
            await LoadProcessDetailsInternal();
        }

        private async Task LoadProcessDetailsInternal()
        {
            // キャッシュされたデータを使用するか、データベースから取得するかを決定
            Task<List<ProcessDetail>> detailsTask;
            Task<List<ProcessDetailCategory>> categoriesTask;
            Task<List<Process>> processesTask;

            if (_cachedAllProcessDetails != null)
            {
                detailsTask = Task.FromResult(_cachedAllProcessDetails);
            }
            else
            {
                detailsTask = _repository.GetProcessDetailsAsync();
            }

            if (_cachedCategories != null)
            {
                categoriesTask = Task.FromResult(_cachedCategories);
            }
            else
            {
                categoriesTask = _repository.GetProcessDetailCategoriesAsync();
            }

            if (_cachedAllProcesses != null)
            {
                processesTask = Task.FromResult(_cachedAllProcesses);
            }
            else
            {
                processesTask = _repository.GetProcessesAsync();
            }

            // 条件と接続情報を並列で取得
            var startConditionsTask = _repository.GetProcessStartConditionsAsync(_cycleId);
            var finishConditionsTask = _repository.GetProcessFinishConditionsAsync(_cycleId);

            Task<List<ProcessDetailConnection>> connectionsTask;
            Task<List<ProcessDetailFinish>> finishesTask;
            if (ShowAllConnections)
            {
                connectionsTask = _repository.GetAllProcessDetailConnectionsAsync();
                finishesTask = _repository.GetAllProcessDetailFinishesAsync();
            }
            else
            {
                connectionsTask = _repository.GetProcessDetailConnectionsAsync(_cycleId);
                finishesTask = _repository.GetProcessDetailFinishesAsync(_cycleId);
            }

            // すべてのタスクを並列実行
            await Task.WhenAll(detailsTask, categoriesTask, processesTask, startConditionsTask, finishConditionsTask, connectionsTask, finishesTask);

            // 結果を取得してフィルタリング
            var details = detailsTask.Result
                .Where(d => d.CycleId == _cycleId)
                .OrderBy(d => d.SortNumber)
                .ToList();

            var categoriesList = categoriesTask.Result;
            Categories.Clear();
            foreach (var category in categoriesList)
            {
                Categories.Add(category);
            }

            var processes = processesTask.Result
                .Where(p => p.CycleId == _cycleId)
                .ToList();

            _processStartConditions = startConditionsTask.Result;
            _processFinishConditions = finishConditionsTask.Result;
            _dbConnections = connectionsTask.Result;
            _dbFinishes = finishesTask.Result;

            // すべての工程のIDとProcessNameのマッピングを作成
            var processMap = processes
                .ToDictionary(p => p.Id, p => p.ProcessName ?? $"工程{p.Id}");

            AllNodes.Clear();
            AllConnections.Clear();
            Nodes.Clear();
            Connections.Clear();
            CompositeGroups.Clear();

            // ノードを作成し、レイアウトを計算
            var nodeDict = new Dictionary<int, ProcessFlowNode>();
            var processNodeDict = new Dictionary<int, ProcessFlowNode>();

            // ProcessごとにProcessDetailをグループ化
            var detailsByProcess = details.GroupBy(d => d.ProcessId)
                .ToDictionary(g => g.Key, g => g.OrderBy(d => d.SortNumber).ToList());

            double currentY = 50;
            double maxX = 0;

            // Processごとに階層的に配置
            foreach (var process in processes.OrderBy(p => p.SortNumber))
            {
                // Processノードを作成（左側）
                var processPosition = new Point(50, currentY);
                var processNode = new ProcessFlowNode(process, processPosition);
                processNodeDict[process.Id] = processNode;
                AllNodes.Add(processNode);
                Nodes.Add(processNode);

                double processDetailStartY = currentY;
                double processDetailMaxY = currentY;

                // このProcessに属するProcessDetailを取得
                if (detailsByProcess.TryGetValue(process.Id, out var processDetails))
                {
                    // ProcessDetailをProcessの右側に配置
                    var sortedDetails = processDetails.ToList();
                    var levels = CalculateNodeLevels(sortedDetails);

                    // レベルごとのノード数をカウント
                    var levelCounts = new Dictionary<int, int>();
                    foreach (var detail in sortedDetails)
                    {
                        var level = levels.ContainsKey(detail.Id) ? levels[detail.Id] : 0;
                        if (!levelCounts.ContainsKey(level))
                            levelCounts[level] = 0;
                        levelCounts[level]++;
                    }

                    // 各レベルの現在のノード数を追跡
                    var currentLevelCounts = new Dictionary<int, int>();

                    foreach (var detail in sortedDetails)
                    {
                        var level = levels.ContainsKey(detail.Id) ? levels[detail.Id] : 0;
                        if (!currentLevelCounts.ContainsKey(level))
                            currentLevelCounts[level] = 0;

                        var index = currentLevelCounts[level];

                        // ProcessDetailの位置を計算（Processノードの右側に階層的に配置）
                        double x = level * 250 + 350; // 水平間隔を250に、左余白を350に
                        double y = processDetailStartY + (index * 100); // 垂直間隔を100に

                        var position = new Point(x, y);
                        var node = CreateNode(detail, position, categoriesList, processMap);

                        nodeDict[detail.Id] = node;
                        AllNodes.Add(node);
                        Nodes.Add(node);

                        currentLevelCounts[level]++;

                        // このProcessのProcessDetailの最大Y座標を更新
                        processDetailMaxY = Math.Max(processDetailMaxY, y + 80);

                        // キャンバスの最大X座標を更新
                        maxX = Math.Max(maxX, x + 150);
                    }
                }

                // 次のProcessのY座標を計算（現在のProcessとそのProcessDetailの高さを考慮）
                currentY = Math.Max(currentY + 150, processDetailMaxY + 50);
            }

            // キャンバスサイズを設定（余白を追加）
            CanvasWidth = Math.Max(3000, maxX + 400);
            CanvasHeight = Math.Max(3000, currentY + 200);

            // 他サイクルのノードも追加（ShowAllConnectionsがtrueの場合）
            if (ShowAllConnections)
            {
                await AddOtherCycleNodes(nodeDict, categoriesList, processMap);
            }

            // ProcessDetailノード間の接続を作成
            CreateConnections(nodeDict);

            // ProcessとProcessDetailの接続を作成
            CreateProcessConnections(processNodeDict, nodeDict, _processStartConditions, _processFinishConditions);

            // 複合工程のグループを作成
            CreateCompositeGroups(nodeDict, processes);

            // 元の位置を記録
            _originalPositions.Clear();
            foreach (var node in AllNodes)
            {
                if (node.NodeType == ProcessFlowNodeType.ProcessDetail && node.ProcessDetail != null)
                {
                    _originalPositions[node.ProcessDetail.Id] = node.Position;
                }
                else if (node.NodeType == ProcessFlowNodeType.Process && node.Process != null)
                {
                    _originalPositions[node.Process.Id] = node.Position;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Created {AllNodes.Count} nodes and {AllConnections.Count} connections");
        }

        private async Task AddOtherCycleNodes(Dictionary<int, ProcessFlowNode> nodeDict, List<ProcessDetailCategory> categoriesList, Dictionary<int, string> processMap)
        {
            // 他サイクルへの接続を見つける
            var otherCycleDetailIds = new HashSet<int>();

            // 接続元と接続先から他サイクルのIDを収集
            foreach (var conn in _dbConnections)
            {
                if (!nodeDict.ContainsKey(conn.FromProcessDetailId))
                    otherCycleDetailIds.Add(conn.FromProcessDetailId);
                if (conn.ToProcessDetailId.HasValue && !nodeDict.ContainsKey(conn.ToProcessDetailId.Value))
                    otherCycleDetailIds.Add(conn.ToProcessDetailId.Value);
            }

            foreach (var finish in _dbFinishes)
            {
                if (!nodeDict.ContainsKey(finish.ProcessDetailId))
                    otherCycleDetailIds.Add(finish.ProcessDetailId);
                if (!nodeDict.ContainsKey(finish.FinishProcessDetailId))
                    otherCycleDetailIds.Add(finish.FinishProcessDetailId);
            }

            // 他サイクルのProcessDetailを取得
            if (otherCycleDetailIds.Count > 0)
            {
                var otherCycleDetails = (await _repository.GetProcessDetailsAsync())
                    .Where(d => otherCycleDetailIds.Contains(d.Id))
                    .ToList();

                // 他サイクルのProcess情報も取得
                var otherCycleCycleIds = otherCycleDetails.Select(d => d.CycleId).Distinct().ToList();
                var otherCycleProcesses = (await _repository.GetProcessesAsync())
                    .Where(p => otherCycleCycleIds.Contains(p.CycleId))
                    .ToList();

                // 他サイクルの工程マップを更新
                foreach (var process in otherCycleProcesses)
                {
                    if (!processMap.ContainsKey(process.Id))
                    {
                        processMap[process.Id] = process.ProcessName ?? $"工程{process.Id}";
                    }
                }

                // 他サイクルのノードを作成
                double otherCycleY = 50;
                foreach (var detail in otherCycleDetails.OrderBy(d => d.CycleId).ThenBy(d => d.SortNumber))
                {
                    var position = new Point(1400, otherCycleY); // 右側に配置
                    var node = CreateNode(detail, position, categoriesList, processMap);
                    node.IsOtherCycleNode = true;

                    nodeDict[detail.Id] = node;
                    AllNodes.Add(node);
                    Nodes.Add(node);

                    otherCycleY += 80;
                }
            }
        }

        private Dictionary<int, int> CalculateNodeLevels(List<ProcessDetail> details)
        {
            var levels = new Dictionary<int, int>();
            var connections = _dbConnections.Where(c =>
                c.ToProcessDetailId.HasValue &&
                details.Any(d => d.Id == c.FromProcessDetailId) &&
                details.Any(d => d.Id == c.ToProcessDetailId.Value)).ToList();

            // 初期レベルを設定
            foreach (var detail in details)
            {
                levels[detail.Id] = 0;
            }

            // 接続に基づいてレベルを計算
            // 最大反復回数を設定（DAGの場合、最大でノード数-1回の反復で完了する）
            int maxIterations = details.Count;
            int iteration = 0;
            bool changed = true;

            while (changed && iteration < maxIterations)
            {
                changed = false;
                iteration++;

                foreach (var conn in connections)
                {
                    if (conn.ToProcessDetailId.HasValue &&
                        levels.ContainsKey(conn.FromProcessDetailId) &&
                        levels.ContainsKey(conn.ToProcessDetailId.Value))
                    {
                        int newLevel = levels[conn.FromProcessDetailId] + 1;
                        if (levels[conn.ToProcessDetailId.Value] < newLevel)
                        {
                            levels[conn.ToProcessDetailId.Value] = newLevel;
                            changed = true;
                        }
                    }
                }
            }

            // サイクル検出：最大反復回数に達した場合は警告を出力
            if (iteration >= maxIterations && changed)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ 警告: ProcessDetail接続に循環参照（サイクル）が検出されました。レベル計算を中断しました。");
                System.Diagnostics.Debug.WriteLine($"  反復回数: {iteration}, ノード数: {details.Count}");
                MessageBox.Show(
                    "工程詳細の接続に循環参照が検出されました。\nレイアウト計算が不完全な可能性があります。\n\n接続を確認してください。",
                    "循環参照の警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return levels;
        }

        private void CreateConnections(Dictionary<int, ProcessFlowNode> nodeDict)
        {
            // 通常の接続を作成
            foreach (var conn in _dbConnections)
            {
                // ProcessDetail -> ProcessDetail の接続
                if (conn.ToProcessDetailId.HasValue &&
                    nodeDict.ContainsKey(conn.FromProcessDetailId) &&
                    nodeDict.ContainsKey(conn.ToProcessDetailId.Value))
                {
                    var fromNode = nodeDict[conn.FromProcessDetailId];
                    var toNode = nodeDict[conn.ToProcessDetailId.Value];

                    var connection = new ProcessFlowConnection(fromNode, toNode)
                    {
                        IsFinishConnection = false,
                        IsOtherCycleConnection = fromNode.ProcessDetail?.CycleId != _cycleId || toNode.ProcessDetail?.CycleId != _cycleId
                    };
                    // StartSensor is stored in ProcessDetail, not in ProcessDetailConnection

                    AllConnections.Add(connection);
                    Connections.Add(connection);
                }
                // ProcessDetail -> Process の接続
                // Note: ToProcessId プロパティは削除されたため、この機能は現在サポートされていません
                /*
                else if (conn.ToProcessId.HasValue &&
                    nodeDict.ContainsKey(conn.FromProcessDetailId))
                {
                    var fromNode = nodeDict[conn.FromProcessDetailId];
                    var toProcessNode = AllNodes.FirstOrDefault(n => n.NodeType == ProcessFlowNodeType.Process && n.Process?.Id == conn.ToProcessId.Value);

                    if (toProcessNode != null)
                    {
                        var connection = new ProcessFlowConnection(fromNode, toProcessNode)
                        {
                            ConnectionType = ConnectionType.ProcessToDetail,
                            IsFinishConnection = false,
                            IsOtherCycleConnection = fromNode.ProcessDetail?.CycleId != _cycleId
                        };
                        // StartSensor is stored in ProcessDetail, not in ProcessDetailConnection

                        AllConnections.Add(connection);
                        Connections.Add(connection);
                    }
                }
                */
            }

            // 終了条件接続を作成
            foreach (var finish in _dbFinishes)
            {
                // ProcessDetail -> ProcessDetail の終了条件
                if (nodeDict.ContainsKey(finish.ProcessDetailId) &&
                    nodeDict.ContainsKey(finish.FinishProcessDetailId))
                {
                    var fromNode = nodeDict[finish.ProcessDetailId];
                    var toNode = nodeDict[finish.FinishProcessDetailId];

                    var connection = new ProcessFlowConnection(fromNode, toNode)
                    {
                        IsFinishConnection = true,
                        IsOtherCycleConnection = fromNode.ProcessDetail?.CycleId != _cycleId || toNode.ProcessDetail?.CycleId != _cycleId
                    };
                    // FinishSensorプロパティはProcessDetailFinishテーブルに存在しないためコメントアウト
                    // connection.DbStartSensor = finish.FinishSensor ?? "";

                    AllConnections.Add(connection);
                    Connections.Add(connection);
                }
                // ProcessDetail -> Process の終了条件
                // FinishProcessIdプロパティがProcessDetailFinishテーブルに存在しないため、
                // ProcessDetail -> Process の終了条件接続は現在のスキーマではサポートされていません
                /*
                else if (finish.FinishProcessId.HasValue &&
                    nodeDict.ContainsKey(finish.ProcessDetailId))
                {
                    var fromNode = nodeDict[finish.ProcessDetailId];
                    var toProcessNode = AllNodes.FirstOrDefault(n => n.NodeType == ProcessFlowNodeType.Process && n.Process?.Id == finish.FinishProcessId.Value);

                    if (toProcessNode != null)
                    {
                        var connection = new ProcessFlowConnection(fromNode, toProcessNode)
                        {
                            ConnectionType = ConnectionType.ProcessToDetail,
                            IsFinishConnection = true,
                            IsOtherCycleConnection = fromNode.ProcessDetail?.CycleId != _cycleId
                        };
                        connection.DbStartSensor = finish.FinishSensor ?? "";

                        AllConnections.Add(connection);
                        Connections.Add(connection);
                    }
                }
                */
            }
        }

        private void CreateCompositeGroups(Dictionary<int, ProcessFlowNode> nodeDict, List<Process> processes)
        {
            // 複合工程のグループ化は現在使用していないためコメントアウト
            // 必要に応じて後で実装
        }

        partial void OnSelectedNodeChanged(ProcessFlowNode? value)
        {
            if (value != null)
            {
                // ノードタイプに応じて処理を分岐
                if (value.NodeType == ProcessFlowNodeType.Process)
                {
                    // Processノードの場合
                    if (value.Process != null)
                    {
                        // Process専用のプロパティを設定
                        SelectedNodeDisplayName = value.DisplayName;
                        IsNodeSelected = true;

                        // ProcessDetailのプロパティをクリア
                        SelectedNodeDetailName = "";
                        SelectedNodeOperationId = null;
                        SelectedNodeStartSensor = "";
                        SelectedNodeFinishSensor = "";
                        SelectedNodeCategoryId = null;
                        SelectedNodeBlockNumber = null;
                        SelectedNodeSkipMode = "";
                        SelectedNodeSortNumber = null;
                        SelectedNodeComment = "";
                        SelectedNodeILStart = "";
                        SelectedNodeId = value.Process.Id;
                        SelectedNodeProcessId = value.Process.Id;
                        SelectedNodeStartTimerId = null;
                        SelectedNodeIsResetAfter = null;

                        // TODO: Process用のプロパティウィンドウを開く
                        ShowProcessPropertiesWindow(value.Process);
                    }
                }
                else if (value.NodeType == ProcessFlowNodeType.ProcessDetail)
                {
                    // ProcessDetailノードの場合
                    if (value.ProcessDetail != null)
                    {
                        SelectedNodeDetailName = value.ProcessDetail.DetailName ?? "";
                        SelectedNodeOperationId = value.ProcessDetail.OperationId;
                        SelectedNodeStartSensor = value.ProcessDetail.StartSensor ?? "";
                        SelectedNodeFinishSensor = value.ProcessDetail.FinishSensor ?? "";
                        SelectedNodeCategoryId = value.ProcessDetail.CategoryId;
                        SelectedNodeBlockNumber = value.ProcessDetail.BlockNumber;
                        SelectedNodeSkipMode = value.ProcessDetail.SkipMode ?? "";
                        SelectedNodeSortNumber = value.ProcessDetail.SortNumber;
                        SelectedNodeComment = value.ProcessDetail.Comment ?? "";
                        SelectedNodeILStart = value.ProcessDetail.ILStart ?? "";
                        SelectedNodeId = value.ProcessDetail.Id;
                        SelectedNodeProcessId = value.ProcessDetail.ProcessId;
                        SelectedNodeStartTimerId = value.ProcessDetail.StartTimerId;
                        SelectedNodeIsResetAfter = value.ProcessDetail.IsResetAfter;
                        SelectedNodeDisplayName = value.DisplayName;
                        IsNodeSelected = true;

                        // 接続情報を更新
                        UpdateNodeConnections(value);
                    }
                }
            }
            else
            {
                SelectedNodeDetailName = "";
                SelectedNodeOperationId = null;
                SelectedNodeStartSensor = "";
                SelectedNodeFinishSensor = "";
                SelectedNodeCategoryId = null;
                SelectedNodeBlockNumber = null;
                SelectedNodeSkipMode = "";
                SelectedNodeSortNumber = null;
                SelectedNodeComment = "";
                SelectedNodeILStart = "";
                SelectedNodeId = null;
                SelectedNodeProcessId = null;
                SelectedNodeStartTimerId = null;
                SelectedNodeIsResetAfter = null;
                SelectedNodeDisplayName = "";
                IsNodeSelected = false;

                IncomingConnections.Clear();
                OutgoingConnections.Clear();
                HasOtherCycleConnections = false;
            }

            // 全ノードの選択状態を更新
            foreach (var node in Nodes)
            {
                node.IsSelected = node == value;
            }
        }

        private void UpdateNodeConnections(ProcessFlowNode node)
        {
            IncomingConnections.Clear();
            OutgoingConnections.Clear();
            HasOtherCycleConnections = false;

            // 接続元（このノードへの接続）
            var incoming = AllConnections.Where(c => c.ToNode == node).ToList();
            foreach (var conn in incoming)
            {
                IncomingConnections.Add(conn);
                if (conn.IsOtherCycleConnection)
                    HasOtherCycleConnections = true;
            }

            // 接続先（このノードからの接続）
            var outgoing = AllConnections.Where(c => c.FromNode == node).ToList();
            foreach (var conn in outgoing)
            {
                OutgoingConnections.Add(conn);
                if (conn.IsOtherCycleConnection)
                    HasOtherCycleConnections = true;
            }
        }

        [RelayCommand]
        private void CanvasMouseDown(MouseButtonEventArgs e)
        {
            if (e.Source is Canvas canvas)
            {
                var position = e.GetPosition(canvas);
                MousePosition = position;

                // Ctrlキーが押されている場合は接続モード
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    return;
                }

                // 左クリックで選択開始
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _isSelecting = true;
                    _selectionStartPoint = position;
                    _hasMouseMoved = false;
                    IsRectangleSelecting = false;
                    SelectionRectangle = new Rect(-1, -1, 0, 0);

                    // すべてのノードの選択を解除
                    SelectedNode = null;
                    foreach (var node in Nodes)
                    {
                        node.IsSelected = false;
                    }
                    _selectedNodes.Clear();

                    // 接続の選択も解除
                    SelectedConnection = null;
                    foreach (var conn in Connections)
                    {
                        conn.IsSelected = false;
                    }
                }
            }
        }

        [RelayCommand]
        private void CanvasMouseMove(MouseEventArgs e)
        {
            if (e.Source is Canvas canvas)
            {
                var position = e.GetPosition(canvas);
                MousePosition = position;

                if (IsConnecting && _connectionStartNode != null)
                {
                    // 接続線の更新はバインディングで自動的に行われる
                }
                else if (_draggedNode != null)
                {
                    // ドラッグ中の処理
                    if (_selectedNodes.Count > 1 && _selectedNodes.Contains(_draggedNode))
                    {
                        // 複数選択されている場合、すべて移動
                        foreach (var node in _selectedNodes)
                        {
                            if (_dragOffsets.ContainsKey(node))
                            {
                                var offset = _dragOffsets[node];
                                node.Position = new Point(position.X - offset.X, position.Y - offset.Y);
                            }
                        }
                    }
                    else
                    {
                        // 単一ノードの移動
                        _draggedNode.Position = new Point(position.X - _dragOffset.X, position.Y - _dragOffset.Y);
                    }

                    // 接続線の更新は自動的に処理される
                }
                else if (_isSelecting && e.LeftButton == MouseButtonState.Pressed)
                {
                    // 矩形選択中
                    if (!_hasMouseMoved)
                    {
                        var distance = Math.Sqrt(Math.Pow(position.X - _selectionStartPoint.X, 2) +
                                                Math.Pow(position.Y - _selectionStartPoint.Y, 2));
                        if (distance > 5) // 5ピクセル以上動いたら選択開始
                        {
                            _hasMouseMoved = true;
                            IsRectangleSelecting = true;
                        }
                    }

                    if (_hasMouseMoved)
                    {
                        var x = Math.Min(_selectionStartPoint.X, position.X);
                        var y = Math.Min(_selectionStartPoint.Y, position.Y);
                        var width = Math.Abs(position.X - _selectionStartPoint.X);
                        var height = Math.Abs(position.Y - _selectionStartPoint.Y);

                        SelectionRectangle = new Rect(x, y, width, height);

                        // 矩形内のノードを選択
                        _selectedNodes.Clear();
                        foreach (var node in Nodes)
                        {
                            var nodeRect = new Rect(node.Position.X, node.Position.Y, 140, 40);
                            node.IsSelected = SelectionRectangle.IntersectsWith(nodeRect);
                            if (node.IsSelected)
                            {
                                _selectedNodes.Add(node);
                            }
                        }
                    }
                }
            }
        }

        [RelayCommand]
        private void CanvasMouseUp()
        {
            _draggedNode = null;
            _dragOffsets.Clear();
            IsConnecting = false;
            _connectionStartNode = null;
            _isSelecting = false;
            _hasMouseMoved = false;
            IsRectangleSelecting = false;
            SelectionRectangle = new Rect(-1, -1, 0, 0);

            // ドラッグ状態をリセット
            foreach (var node in Nodes)
            {
                node.IsDragging = false;
            }
        }

        [RelayCommand]
        private void NodeMouseDown(ProcessFlowNode node)
        {
            if (node == null) return;

            var position = MousePosition;

            // Ctrlキーが押されている場合は接続を作成
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (_connectionStartNode == null)
                {
                    _connectionStartNode = node;
                    IsConnecting = true;
                    _isCreatingFinishConnection = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    ConnectionStartPoint = new Point(node.Position.X + 70, node.Position.Y + 20);
                    IsConnecting = true;
                }
                return;
            }

            // Shiftキーが押されている場合は複数選択
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                node.IsSelected = !node.IsSelected;
                if (node.IsSelected)
                {
                    if (!_selectedNodes.Contains(node))
                        _selectedNodes.Add(node);
                }
                else
                {
                    _selectedNodes.Remove(node);
                }
                return;
            }

            // 通常のクリックの場合
            if (!node.IsSelected)
            {
                // 他の選択を解除
                foreach (var n in Nodes)
                {
                    n.IsSelected = false;
                }
                _selectedNodes.Clear();

                // このノードを選択
                node.IsSelected = true;
                SelectedNode = node;
                _selectedNodes.Add(node);
            }

            // ドラッグ開始
            _draggedNode = node;
            _dragOffset = new Point(position.X - node.Position.X, position.Y - node.Position.Y);
            node.IsDragging = true;

            // 複数選択されている場合、各ノードのオフセットを記録
            if (_selectedNodes.Count > 1)
            {
                _dragOffsets.Clear();
                foreach (var n in _selectedNodes)
                {
                    _dragOffsets[n] = new Point(position.X - n.Position.X, position.Y - n.Position.Y);
                    n.IsDragging = true;
                }
            }
        }

        [RelayCommand]
        private async Task NodeMouseUp(ProcessFlowNode node)
        {
            if (node == null) return;

            // 接続を完成させる
            if (IsConnecting && _connectionStartNode != null && _connectionStartNode != node)
            {
                // ProcessからProcessDetailへの接続は許可しない（自動生成のため）
                if (_connectionStartNode.NodeType == ProcessFlowNodeType.Process &&
                    node.NodeType == ProcessFlowNodeType.ProcessDetail)
                {
                    // Process->ProcessDetail接続は自動的に作成されるため、手動での接続は不要
                    IsConnecting = false;
                    _connectionStartNode = null;
                    return;
                }

                // ProcessDetailからProcessへの接続を処理
                if (_connectionStartNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                    node.NodeType == ProcessFlowNodeType.Process)
                {
                    // 同じ接続が既に存在するかチェック
                    var existingConnection = Connections.FirstOrDefault(c =>
                        c.FromNode == _connectionStartNode && c.ToNode == node &&
                        c.IsFinishConnection == _isCreatingFinishConnection);

                    if (existingConnection == null && _connectionStartNode.ProcessDetail != null && node.Process != null)
                    {
                        var newConnection = new ProcessFlowConnection(_connectionStartNode, node)
                        {
                            ConnectionType = ConnectionType.ProcessToDetail,  // 緑色で表示
                            IsFinishConnection = _isCreatingFinishConnection,
                            IsOtherCycleConnection = _connectionStartNode.ProcessDetail.CycleId != _cycleId
                        };

                        AllConnections.Add(newConnection);
                        Connections.Add(newConnection);

                        // データベースに保存（正しいテーブルを使用）
                        if (_isCreatingFinishConnection)
                        {
                            // ProcessDetailからProcessへの終了条件接続 → ProcessFinishConditionテーブル
                            var dbFinish = new ProcessFinishCondition
                            {
                                ProcessId = node.Process.Id,  // ProcessのID
                                FinishProcessDetailId = _connectionStartNode.ProcessDetail.Id,  // ProcessDetailのID
                                FinishSensor = ""
                            };
                            await _repository.AddProcessFinishConditionAsync(dbFinish);
                            // リストにも追加（ローカル管理用）
                            _processFinishConditions.Add(dbFinish);
                        }
                        else
                        {
                            // ProcessDetailからProcessへの開始条件接続 → ProcessStartConditionテーブル
                            var dbStart = new ProcessStartCondition
                            {
                                ProcessId = node.Process.Id,  // ProcessのID
                                StartProcessDetailId = _connectionStartNode.ProcessDetail.Id,  // ProcessDetailのID
                                StartSensor = ""
                            };
                            await _repository.AddProcessStartConditionAsync(dbStart);
                            // リストにも追加（ローカル管理用）
                            _processStartConditions.Add(dbStart);
                        }
                    }

                    IsConnecting = false;
                    _connectionStartNode = null;
                    return;
                }

                // ProcessDetail同士の接続処理
                if (_connectionStartNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                    node.NodeType == ProcessFlowNodeType.ProcessDetail)
                {
                    // 同じ接続が既に存在するかチェック
                    var existingConnection = Connections.FirstOrDefault(c =>
                        c.FromNode == _connectionStartNode && c.ToNode == node &&
                        c.IsFinishConnection == _isCreatingFinishConnection);

                    if (existingConnection == null && _connectionStartNode.ProcessDetail != null && node.ProcessDetail != null)
                    {
                        var newConnection = new ProcessFlowConnection(_connectionStartNode, node)
                        {
                            IsFinishConnection = _isCreatingFinishConnection,
                            IsOtherCycleConnection = _connectionStartNode.ProcessDetail.CycleId != _cycleId ||
                                                    node.ProcessDetail.CycleId != _cycleId
                        };

                        AllConnections.Add(newConnection);
                        Connections.Add(newConnection);

                        // データベースに保存
                        if (_isCreatingFinishConnection)
                        {
                            var dbFinish = new ProcessDetailFinish
                            {
                                ProcessDetailId = _connectionStartNode.ProcessDetail.Id,
                                FinishProcessDetailId = node.ProcessDetail.Id,
                                CycleId = _cycleId
                                // FinishSensorプロパティはProcessDetailFinishテーブルに存在しません
                            };
                            await _repository.AddProcessDetailFinishAsync(dbFinish);
                            _dbFinishes.Add(dbFinish);
                        }
                        else
                        {
                            var dbConnection = new ProcessDetailConnection
                            {
                                FromProcessDetailId = _connectionStartNode.ProcessDetail.Id,
                                ToProcessDetailId = node.ProcessDetail.Id,
                                CycleId = _cycleId  // 現在のサイクルIDを設定
                            };

                            // デバッグ出力：保存しようとしているデータを表示
                            System.Diagnostics.Debug.WriteLine("=== ProcessDetailConnection 保存前 ===");
                            System.Diagnostics.Debug.WriteLine($"  FromProcessDetailId: {dbConnection.FromProcessDetailId}");
                            System.Diagnostics.Debug.WriteLine($"  ToProcessDetailId: {dbConnection.ToProcessDetailId}");
                            System.Diagnostics.Debug.WriteLine($"  CycleId: {dbConnection.CycleId}");
                            System.Diagnostics.Debug.WriteLine($"  From: {_connectionStartNode.ProcessDetail.DetailName} (ID: {_connectionStartNode.ProcessDetail.Id})");
                            System.Diagnostics.Debug.WriteLine($"  To: {node.ProcessDetail.DetailName} (ID: {node.ProcessDetail.Id})");

                            // 既存の接続を確認
                            var existingDbConnection = _dbConnections.FirstOrDefault(c =>
                                c.FromProcessDetailId == dbConnection.FromProcessDetailId &&
                                c.ToProcessDetailId == dbConnection.ToProcessDetailId);
                            if (existingDbConnection != null)
                            {
                                System.Diagnostics.Debug.WriteLine("  ⚠️ 警告: 既に同じ接続が存在します！");
                                System.Diagnostics.Debug.WriteLine($"    既存接続: From={existingDbConnection.FromProcessDetailId}, To={existingDbConnection.ToProcessDetailId}");
                            }
                            System.Diagnostics.Debug.WriteLine("=====================================");

                            await _repository.AddProcessDetailConnectionAsync(dbConnection);
                            _dbConnections.Add(dbConnection);
                        }
                    }
                }
            }

            IsConnecting = false;
            _connectionStartNode = null;
        }

        [RelayCommand]
        private void EditSelectedNode()
        {
            // ダブルクリックでプロパティウィンドウを開く/フォーカスする
            // (ProcessFlowDetailWindow.xaml.cs側で処理される)
            if (SelectedNode != null)
            {
                // プロパティウィンドウ表示要求イベントを発火
                RequestShowPropertiesWindow?.Invoke(this, EventArgs.Empty);
            }
        }

        [RelayCommand]
        private async void SaveChanges()
        {
            try
            {
                // ProcessDetailの位置とプロパティを保存
                foreach (var node in AllNodes.Where(n => n.NodeType == ProcessFlowNodeType.ProcessDetail &&
                                                         n.ProcessDetail != null &&
                                                         n.ProcessDetail.CycleId == _cycleId))
                {
                    // ProcessDetailの位置情報を更新
                    await _repository.UpdateProcessDetailAsync(node.ProcessDetail);
                    node.IsModified = false;
                }

                // 接続の変更を保存
                foreach (var connection in AllConnections.Where(c => c.IsModified))
                {
                    if (connection.IsFinishConnection)
                    {
                        // ProcessDetail -> ProcessDetail の終了条件
                        if (connection.FromNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            connection.ToNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            connection.FromNode.ProcessDetail != null &&
                            connection.ToNode.ProcessDetail != null)
                        {
                            var finish = _dbFinishes.FirstOrDefault(f =>
                                f.ProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                                f.FinishProcessDetailId == connection.ToNode.ProcessDetail.Id);

                            // FinishSensorプロパティがProcessDetailFinishテーブルに存在しないため、
                            // センサー情報の保存はサポートされていません
                            // 終了条件の接続関係のみが管理されます
                            /*
                            if (finish != null)
                            {
                                finish.FinishSensor = connection.StartSensor;
                                // UpdateProcessDetailFinishメソッドが存在しない場合は削除と追加で対応
                                await _repository.DeleteProcessDetailFinishAsync(finish.Id);
                                await _repository.AddProcessDetailFinishAsync(finish);
                            }
                            */
                        }
                        // ProcessDetail -> Process の終了条件
                        // FinishProcessIdプロパティがProcessDetailFinishテーブルに存在しないため、
                        // ProcessDetail -> Process の終了条件接続は現在のスキーマではサポートされていません
                        /*
                        else if (connection.FromNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            connection.ToNode.NodeType == ProcessFlowNodeType.Process &&
                            connection.FromNode.ProcessDetail != null &&
                            connection.ToNode.Process != null)
                        {
                            var finish = _dbFinishes.FirstOrDefault(f =>
                                f.ProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                                f.FinishProcessId == connection.ToNode.Process.Id);

                            if (finish != null)
                            {
                                finish.FinishSensor = connection.StartSensor;
                                // UpdateProcessDetailFinishメソッドが存在しない場合は削除と追加で対応
                                await _repository.DeleteProcessDetailFinishAsync(finish.Id);
                                await _repository.AddProcessDetailFinishAsync(finish);
                            }
                        }
                        */
                    }
                    else
                    {
                        // ProcessDetail -> ProcessDetail の通常接続
                        if (connection.FromNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            connection.ToNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            connection.FromNode.ProcessDetail != null &&
                            connection.ToNode.ProcessDetail != null)
                        {
                            var conn = _dbConnections.FirstOrDefault(c =>
                                c.FromProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                                c.ToProcessDetailId == connection.ToNode.ProcessDetail.Id);

                        }
                        // ProcessDetail -> Process の通常接続
                        // Note: ToProcessId プロパティは削除されたため、この機能は現在サポートされていません
                        /*
                        else if (connection.FromNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            connection.ToNode.NodeType == ProcessFlowNodeType.Process &&
                            connection.FromNode.ProcessDetail != null &&
                            connection.ToNode.Process != null)
                        {
                            var dbConnection = _dbConnections.FirstOrDefault(c =>
                                c.FromProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                                c.ToProcessId == connection.ToNode.Process.Id);
                        }
                        */
                    }

                    connection.IsModified = false;
                }

                MessageBox.Show("変更を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async void AddNewNode()
        {
            // 新しいノードを追加するロジック
            var newDetail = new ProcessDetail
            {
                CycleId = _cycleId,
                DetailName = "新規工程",
                SortNumber = AllNodes.Count(n => n.NodeType == ProcessFlowNodeType.ProcessDetail &&
                                                 n.ProcessDetail != null &&
                                                 n.ProcessDetail.CycleId == _cycleId) + 1
            };

            // データベースに追加
            await _repository.AddProcessDetailAsync(newDetail);

            // UIに追加
            var position = new Point(100, 100);
            var node = new ProcessFlowNode(newDetail, position);
            AllNodes.Add(node);
            Nodes.Add(node);

            SelectedNode = node;
        }

        [RelayCommand]
        private async void DeleteSelectedNode()
        {
            if (SelectedNode == null) return;

            // ProcessDetailノードのみ削除可能
            if (SelectedNode.NodeType != ProcessFlowNodeType.ProcessDetail ||
                SelectedNode.ProcessDetail == null ||
                SelectedNode.ProcessDetail.CycleId != _cycleId) return;

            var result = MessageBox.Show($"選択したノード「{SelectedNode.DisplayName}」を削除しますか？",
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 関連する接続を削除
                var connectionsToRemove = AllConnections
                    .Where(c => c.FromNode == SelectedNode || c.ToNode == SelectedNode)
                    .ToList();

                foreach (var conn in connectionsToRemove)
                {
                    AllConnections.Remove(conn);
                    Connections.Remove(conn);

                    // データベースから削除
                    if (conn.IsFinishConnection)
                    {
                        if (conn.FromNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            conn.ToNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            conn.FromNode.ProcessDetail != null &&
                            conn.ToNode.ProcessDetail != null)
                        {
                            var finish = _dbFinishes.FirstOrDefault(f =>
                                f.ProcessDetailId == conn.FromNode.ProcessDetail.Id &&
                                f.FinishProcessDetailId == conn.ToNode.ProcessDetail.Id);
                            if (finish != null)
                                await _repository.DeleteProcessDetailFinishAsync(finish.ProcessDetailId, finish.FinishProcessDetailId);
                        }
                    }
                    else
                    {
                        if (conn.FromNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            conn.ToNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                            conn.FromNode.ProcessDetail != null &&
                            conn.ToNode.ProcessDetail != null)
                        {
                            var connection = _dbConnections.FirstOrDefault(c =>
                                c.FromProcessDetailId == conn.FromNode.ProcessDetail.Id &&
                                c.ToProcessDetailId == conn.ToNode.ProcessDetail.Id);
                            if (connection != null && connection.ToProcessDetailId.HasValue)
                                await _repository.DeleteProcessDetailConnectionAsync(connection.FromProcessDetailId, connection.ToProcessDetailId.Value);
                        }
                    }
                }

                // ノードを削除
                AllNodes.Remove(SelectedNode);
                Nodes.Remove(SelectedNode);

                // データベースから削除
                await _repository.DeleteProcessDetailAsync(SelectedNode.ProcessDetail.Id);

                SelectedNode = null;
            }
        }

        [RelayCommand]
        private void SelectConnection(ProcessFlowConnection connection)
        {
            if (connection == null) return;

            // 他の接続の選択を解除
            foreach (var conn in Connections)
            {
                conn.IsSelected = false;
            }

            // この接続を選択
            connection.IsSelected = true;
            SelectedConnection = connection;

            // 接続情報ウィンドウを表示するイベントを発生させる
            ConnectionSelected?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async void DeleteConnection(ProcessFlowConnection connection)
        {
            if (connection == null) return;

            var result = MessageBox.Show("選択した接続を削除しますか？",
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // データベースから削除
                // ProcessDetail -> ProcessDetail の接続
                if (connection.FromNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                    connection.ToNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                    connection.FromNode.ProcessDetail != null &&
                    connection.ToNode.ProcessDetail != null)
                {
                    if (connection.IsFinishConnection)
                    {
                        var finish = _dbFinishes.FirstOrDefault(f =>
                            f.ProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                            f.FinishProcessDetailId == connection.ToNode.ProcessDetail.Id);
                        if (finish != null)
                        {
                            await _repository.DeleteProcessDetailFinishAsync(finish.ProcessDetailId, finish.FinishProcessDetailId);
                            _dbFinishes.Remove(finish);
                        }
                    }
                    else
                    {
                        var conn = _dbConnections.FirstOrDefault(c =>
                            c.FromProcessDetailId == connection.FromNode.ProcessDetail.Id &&
                            c.ToProcessDetailId == connection.ToNode.ProcessDetail.Id);
                        if (conn != null && conn.ToProcessDetailId.HasValue)
                        {
                            await _repository.DeleteProcessDetailConnectionAsync(conn.FromProcessDetailId, conn.ToProcessDetailId.Value);
                            _dbConnections.Remove(conn);
                        }
                    }
                }
                // ProcessDetail -> Process の接続
                else if (connection.FromNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                    connection.ToNode.NodeType == ProcessFlowNodeType.Process &&
                    connection.FromNode.ProcessDetail != null &&
                    connection.ToNode.Process != null)
                {
                    if (connection.IsFinishConnection)
                    {
                        // ProcessFinishConditionから削除
                        var finish = _processFinishConditions.FirstOrDefault(f =>
                            f.ProcessId == connection.ToNode.Process.Id &&
                            f.FinishProcessDetailId == connection.FromNode.ProcessDetail.Id);
                        if (finish != null)
                        {
                            await _repository.DeleteProcessFinishConditionAsync(finish.Id);
                            _processFinishConditions.Remove(finish);
                        }
                    }
                    else
                    {
                        // ProcessStartConditionから削除
                        var start = _processStartConditions.FirstOrDefault(s =>
                            s.ProcessId == connection.ToNode.Process.Id &&
                            s.StartProcessDetailId == connection.FromNode.ProcessDetail.Id);
                        if (start != null)
                        {
                            await _repository.DeleteProcessStartConditionAsync(start.Id);
                            _processStartConditions.Remove(start);
                        }
                    }
                }
                // Process -> ProcessDetail の接続
                else if (connection.FromNode.NodeType == ProcessFlowNodeType.Process &&
                    connection.ToNode.NodeType == ProcessFlowNodeType.ProcessDetail &&
                    connection.FromNode.Process != null &&
                    connection.ToNode.ProcessDetail != null)
                {
                    // ProcessStartConditionから削除（Process->ProcessDetailは開始条件）
                    var start = _processStartConditions.FirstOrDefault(s =>
                        s.ProcessId == connection.FromNode.Process.Id &&
                        s.StartProcessDetailId == connection.ToNode.ProcessDetail.Id);
                    if (start != null)
                    {
                        await _repository.DeleteProcessStartConditionAsync(start.Id);
                        _processStartConditions.Remove(start);
                    }
                }

                // UIから接続を削除（データベース削除後に行う）
                AllConnections.Remove(connection);
                Connections.Remove(connection);

                // 明示的にUIの更新を通知
                OnPropertyChanged(nameof(Connections));
                OnPropertyChanged(nameof(AllConnections));

                SelectedConnection = null;

                // 接続削除完了イベントを発火（ConnectionInfoWindowを閉じるため）
                ConnectionDeleted?.Invoke(this, EventArgs.Empty);
            }
        }

        [RelayCommand]
        private void DeleteSelectedConnection()
        {
            if (SelectedConnection != null)
            {
                DeleteConnection(SelectedConnection);
            }
        }

        [RelayCommand]
        private void ChangeConnectionType()
        {
            if (SelectedConnection == null) return;

            // 期間工程の場合のみ接続タイプを変更可能
            if (!CanChangeConnectionType) return;

            // 接続タイプを切り替え
            SelectedConnection.IsFinishConnection = !SelectedConnection.IsFinishConnection;
            SelectedConnection.IsModified = true;

            MessageBox.Show(
                SelectedConnection.IsFinishConnection ? "終了条件接続に変更しました。" : "開始条件接続に変更しました。",
                "接続タイプ変更",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowProcessPropertiesWindow(Process process)
        {
            // 既存のウィンドウがあるかチェック
            if (_openProcessWindow != null && _openProcessWindow.IsLoaded)
            {
                // 既存のウィンドウを再利用して、新しいProcessを設定
                if (_openProcessWindow.DataContext is ProcessPropertiesViewModel existingVm)
                {
                    // 既存のイベントハンドラをクリア
                    existingVm.ClearEventHandlers();

                    // 新しいProcessを設定
                    existingVm.UpdateProcess(process);

                    // 保存が完了したときの処理を再設定
                    Action? saveHandler = null;
                    saveHandler = () =>
                    {
                        // 変更が保存された場合、ノードの表示を更新
                        if (existingVm.DialogResult == true)
                        {
                            var node = Nodes.FirstOrDefault(n => n.NodeType == ProcessFlowNodeType.Process && n.Process?.Id == process.Id);
                            if (node != null)
                            {
                                node.UpdateDisplayName();
                            }
                        }
                    };

                    existingVm.RequestClose += saveHandler;
                }

                // ウィンドウが最小化されている場合は元に戻す
                if (_openProcessWindow.WindowState == WindowState.Minimized)
                {
                    _openProcessWindow.WindowState = WindowState.Normal;
                }

                // ウィンドウをアクティブにしてフォーカスを与える
                _openProcessWindow.Activate();
                _openProcessWindow.Focus();
                return;
            }

            // 新しいウィンドウを作成
            var window = new ProcessPropertiesWindow(_repository, process);

            // ViewModelを取得してイベントハンドラを設定
            if (window.DataContext is ProcessPropertiesViewModel vm)
            {
                // 保存が完了したときの処理
                Action? saveHandler = null;
                saveHandler = () =>
                {
                    // 変更が保存された場合、ノードの表示を更新
                    if (vm.DialogResult == true)
                    {
                        var node = Nodes.FirstOrDefault(n => n.NodeType == ProcessFlowNodeType.Process && n.Process?.Id == process.Id);
                        if (node != null)
                        {
                            node.UpdateDisplayName();
                        }
                    }
                };

                vm.RequestClose += saveHandler;
            }

            // ウィンドウが閉じられたときの処理
            window.Closed += (s, e) =>
            {
                // 参照をクリア
                _openProcessWindow = null;
            };

            // 参照を保持
            _openProcessWindow = window;

            // 非モーダルで表示
            window.Show();
        }

        [RelayCommand]
        private void Close()
        {
            // ウィンドウを閉じるためのイベントを発行
            RequestClose?.Invoke();
        }

        public event Action? RequestClose;

        [RelayCommand]
        private void FilterBySelectedNode()
        {
            if (SelectedNode == null || SelectedNode.ProcessDetail == null) return;

            FilterNode = SelectedNode;
            IsFiltered = true;

            // 関連するノードを見つける
            var relatedNodeIds = new HashSet<int> { SelectedNode.ProcessDetail.Id };

            // 接続元と接続先を再帰的に追加
            bool added = true;
            while (added)
            {
                added = false;
                var currentIds = relatedNodeIds.ToList();
                foreach (var id in currentIds)
                {

                    var node = AllNodes.FirstOrDefault(n => n.ProcessDetail.Id == id);
                    if (node != null)
                    {
                        // このノードへの接続
                        var incoming = AllConnections.Where(c => c.ToNode == node).Select(c => c.FromNode.ProcessDetail.Id);
                        // このノードからの接続
                        var outgoing = AllConnections.Where(c => c.FromNode == node).Select(c => c.ToNode.ProcessDetail.Id);

                        foreach (var relatedId in incoming.Concat(outgoing))
                        {
                            if (relatedNodeIds.Add(relatedId))
                            {
                                added = true;
                            }
                        }
                    }
                }
            }

            // フィルタリング
            Nodes.Clear();
            Connections.Clear();

            foreach (var node in AllNodes.Where(n => relatedNodeIds.Contains(n.ProcessDetail.Id)))
            {
                Nodes.Add(node);
            }

            foreach (var conn in AllConnections.Where(c =>
                relatedNodeIds.Contains(c.FromNode.ProcessDetail.Id) &&
                relatedNodeIds.Contains(c.ToNode.ProcessDetail.Id)))
            {
                Connections.Add(conn);
            }
        }

        [RelayCommand]
        private void FilterByDirectNeighbors()
        {
            if (SelectedNode == null) return;

            FilterNode = SelectedNode;
            IsFiltered = true;

            // 直接の前後のノードのみを表示
            var relatedNodeIds = new HashSet<int> { SelectedNode.ProcessDetail.Id };

            // 直接の接続元
            var incoming = AllConnections.Where(c => c.ToNode == SelectedNode).Select(c => c.FromNode.ProcessDetail.Id);
            // 直接の接続先
            var outgoing = AllConnections.Where(c => c.FromNode == SelectedNode).Select(c => c.ToNode.ProcessDetail.Id);

            foreach (var id in incoming.Concat(outgoing))
            {
                relatedNodeIds.Add(id);
            }

            // フィルタリング
            Nodes.Clear();
            Connections.Clear();

            foreach (var node in AllNodes.Where(n => relatedNodeIds.Contains(n.ProcessDetail.Id)))
            {
                Nodes.Add(node);
            }

            foreach (var conn in AllConnections.Where(c =>
                relatedNodeIds.Contains(c.FromNode.ProcessDetail.Id) &&
                relatedNodeIds.Contains(c.ToNode.ProcessDetail.Id)))
            {
                Connections.Add(conn);
            }
        }

        [RelayCommand]
        private void ResetFilter()
        {
            IsFiltered = false;
            FilterNode = null;

            // すべてのノードと接続を表示
            Nodes.Clear();
            Connections.Clear();

            foreach (var node in AllNodes)
            {
                Nodes.Add(node);
            }

            foreach (var conn in AllConnections)
            {
                Connections.Add(conn);
            }
        }

        [RelayCommand]
        private async void EditOperation()
        {
            if (SelectedNode == null || SelectedNode.ProcessDetail == null || SelectedNode.ProcessDetail.OperationId == null) return;

            try
            {
                // OperationIdからOperationを取得
                var operation = await _repository.GetOperationByIdAsync(SelectedNode.ProcessDetail.OperationId.Value);
                if (operation == null)
                {
                    MessageBox.Show($"Operation ID {SelectedNode.ProcessDetail.OperationId} が見つかりません。",
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Operation編集ダイアログを開く
                var operationViewModel = new OperationViewModel(_repository, operation, _plcId);
                var operationDialog = new Views.OperationEditorDialog
                {
                    DataContext = operationViewModel,
                    Owner = Application.Current.MainWindow
                };

                bool? dialogResult = false;
                operationViewModel.SetCloseAction(async (result) =>
                {
                    dialogResult = result;
                    if (result)
                    {
                        // 保存処理
                        var updatedOperation = operationViewModel.GetOperation();
                        await _repository.UpdateOperationAsync(updatedOperation);
                    }
                    operationDialog.DialogResult = result;
                });

                if (operationDialog.ShowDialog() == true)
                {
                    // 変更は既にSetCloseAction内で保存済み
                    var updatedOperation = operationViewModel.GetOperation();

                    // ノードの表示名を更新
                    if (!string.IsNullOrEmpty(updatedOperation.OperationName))
                    {
                        SelectedNode.ProcessDetail.DetailName = updatedOperation.OperationName;
                        SelectedNode.UpdateDisplayName();

                        // データベースのProcessDetailも更新
                        await _repository.UpdateProcessDetailAsync(SelectedNode.ProcessDetail);
                    }

                    MessageBox.Show("Operationを更新しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation編集中にエラーが発生しました:\n{ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task UpdateSelectedNodeProperties()
        {
            if (SelectedNode != null && SelectedNode.ProcessDetail != null)
            {
                try
                {
                    // OperationIdがnullまたは0の場合は、現在のProcessDetailの値を維持
                    var operationId = SelectedNodeOperationId;
                    if (operationId == null || operationId == 0)
                    {
                        // 現在のProcessDetailの値を使用
                        operationId = SelectedNode.ProcessDetail.OperationId;
                        // ViewModelのプロパティも更新
                        SelectedNodeOperationId = operationId;
                    }

                    // ProcessDetailのプロパティを更新
                    SelectedNode.ProcessDetail.DetailName = SelectedNodeDetailName;
                    SelectedNode.ProcessDetail.ProcessId = SelectedNodeProcessId ?? 0;
                    SelectedNode.ProcessDetail.OperationId = operationId;
                    SelectedNode.ProcessDetail.StartSensor = SelectedNodeStartSensor;
                    SelectedNode.ProcessDetail.FinishSensor = SelectedNodeFinishSensor;
                    SelectedNode.ProcessDetail.CategoryId = SelectedNodeCategoryId;
                    SelectedNode.ProcessDetail.BlockNumber = SelectedNodeBlockNumber;
                    SelectedNode.ProcessDetail.SkipMode = SelectedNodeSkipMode;
                    SelectedNode.ProcessDetail.SortNumber = SelectedNodeSortNumber;
                    SelectedNode.ProcessDetail.Comment = SelectedNodeComment;
                    SelectedNode.ProcessDetail.ILStart = SelectedNodeILStart;
                    SelectedNode.ProcessDetail.StartTimerId = SelectedNodeStartTimerId;
                    SelectedNode.ProcessDetail.IsResetAfter = SelectedNodeIsResetAfter;

                    // データベースに保存
                    await _repository.UpdateProcessDetailAsync(SelectedNode.ProcessDetail);

                    // カテゴリ名を更新
                    if (SelectedNode.ProcessDetail.CategoryId.HasValue)
                    {
                        var category = Categories.FirstOrDefault(c => c.Id == SelectedNode.ProcessDetail.CategoryId.Value);
                        SelectedNode.CategoryName = category?.CategoryName;
                    }
                    else
                    {
                        SelectedNode.CategoryName = null;
                    }

                    // BlockNumberが変更された場合、対応する工程名を更新
                    if (SelectedNode.ProcessDetail.BlockNumber.HasValue)
                    {
                        // Process情報を再取得してすべての工程から工程名を更新
                        var processes = (await _repository.GetProcessesAsync())
                            .Where(p => p.CycleId == _cycleId)
                            .ToList();

                        var process = processes.FirstOrDefault(p => p.Id == SelectedNode.ProcessDetail.BlockNumber.Value);
                        SelectedNode.CompositeProcessName = process?.ProcessName ?? null;
                    }
                    else
                    {
                        SelectedNode.CompositeProcessName = null;
                    }

                    // 表示名が変更された場合はUIを更新
                    OnPropertyChanged(nameof(SelectedNode));

                    // UIのプロパティを更新（データベースから読み込んだ値でリフレッシュ）
                    var allDetails = await _repository.GetProcessDetailsAsync();
                    var updatedDetail = allDetails.FirstOrDefault(d => d.Id == SelectedNode.ProcessDetail.Id);
                    if (updatedDetail != null)
                    {
                        // ProcessDetailオブジェクトの値を更新
                        SelectedNode.ProcessDetail.DetailName = updatedDetail.DetailName;
                        SelectedNode.ProcessDetail.ProcessId = updatedDetail.ProcessId;
                        SelectedNode.ProcessDetail.OperationId = updatedDetail.OperationId;
                        SelectedNode.ProcessDetail.StartSensor = updatedDetail.StartSensor;
                        SelectedNode.ProcessDetail.FinishSensor = updatedDetail.FinishSensor;
                        SelectedNode.ProcessDetail.CategoryId = updatedDetail.CategoryId;
                        SelectedNode.ProcessDetail.BlockNumber = updatedDetail.BlockNumber;
                        SelectedNode.ProcessDetail.SkipMode = updatedDetail.SkipMode;
                        SelectedNode.ProcessDetail.SortNumber = updatedDetail.SortNumber;
                        SelectedNode.ProcessDetail.Comment = updatedDetail.Comment;
                        SelectedNode.ProcessDetail.ILStart = updatedDetail.ILStart;
                        SelectedNode.ProcessDetail.StartTimerId = updatedDetail.StartTimerId;
                        SelectedNode.ProcessDetail.IsResetAfter = updatedDetail.IsResetAfter;

                        // UIプロパティもリフレッシュ
                        SelectedNodeDetailName = updatedDetail.DetailName ?? string.Empty;
                        SelectedNodeProcessId = updatedDetail.ProcessId;
                        SelectedNodeOperationId = updatedDetail.OperationId;
                        SelectedNodeStartSensor = updatedDetail.StartSensor ?? string.Empty;
                        SelectedNodeFinishSensor = updatedDetail.FinishSensor ?? string.Empty;
                        SelectedNodeCategoryId = updatedDetail.CategoryId;
                        SelectedNodeBlockNumber = updatedDetail.BlockNumber;
                        SelectedNodeSkipMode = updatedDetail.SkipMode ?? string.Empty;
                        SelectedNodeSortNumber = updatedDetail.SortNumber;
                        SelectedNodeComment = updatedDetail.Comment ?? string.Empty;
                        SelectedNodeILStart = updatedDetail.ILStart ?? string.Empty;
                        SelectedNodeStartTimerId = updatedDetail.StartTimerId;
                        SelectedNodeIsResetAfter = updatedDetail.IsResetAfter;
                    }

                    MessageBox.Show("工程詳細を更新しました。", "更新完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"更新に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        partial void OnShowAllConnectionsChanged(bool value)
        {
            // 再読み込み
            LoadProcessDetails();
        }

        // ズーム機能のメソッド
        [RelayCommand]
        private void ZoomIn()
        {
            var newScale = Math.Min(ZoomScale + _zoomStep, _maxZoomScale);
            ZoomScale = Math.Round(newScale, 2);
        }

        [RelayCommand]
        private void ZoomOut()
        {
            var newScale = Math.Max(ZoomScale - _zoomStep, _minZoomScale);
            ZoomScale = Math.Round(newScale, 2);
        }

        public void SetZoom(double scale)
        {
            ZoomScale = Math.Round(Math.Max(_minZoomScale, Math.Min(_maxZoomScale, scale)), 2);
        }

        [RelayCommand]
        private void ResetZoom()
        {
            ZoomScale = 1.0;
        }

        /// <summary>
        /// ProcessDetailを別のProcessに移動
        /// </summary>
        public async Task MoveProcessDetailToProcess(ProcessFlowNode detailNode, int newProcessId)
        {
            if (detailNode.NodeType != ProcessFlowNodeType.ProcessDetail || detailNode.ProcessDetail == null)
                return;

            var oldProcessId = detailNode.ProcessDetail.ProcessId;
            if (oldProcessId == newProcessId)
                return;

            try
            {
                // ProcessDetailのProcessIdを更新
                detailNode.ProcessDetail.ProcessId = newProcessId;
                detailNode.IsModified = true;

                // データベースを更新
                await _repository.UpdateProcessDetailAsync(detailNode.ProcessDetail);

                // レイアウトを再計算して表示を更新
                LoadProcessDetails();

                MessageBox.Show($"ProcessDetailを Process ID {newProcessId} に移動しました。",
                    "移動完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移動中にエラーが発生しました: {ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ProcessDetailのProcess選択ダイアログを表示
        /// </summary>
        [RelayCommand]
        private async void ChangeProcessDetailProcess()
        {
            if (SelectedNode == null ||
                SelectedNode.NodeType != ProcessFlowNodeType.ProcessDetail ||
                SelectedNode.ProcessDetail == null)
            {
                MessageBox.Show("ProcessDetailノードを選択してください。",
                    "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Process選択ダイアログを表示
            var dialog = new ProcessSelectionDialog(Processes.ToList(), SelectedNode.ProcessDetail.ProcessId)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.SelectedProcess != null)
            {
                await MoveProcessDetailToProcess(SelectedNode, dialog.SelectedProcess.Id);
            }
        }

        /// <summary>
        /// ProcessDetailが更新されたときにノードの表示を更新
        /// </summary>
        public void UpdateNodeFromProcessDetail(ProcessDetail updatedProcessDetail)
        {
            if (updatedProcessDetail == null) return;

            // 対応するノードを検索
            var node = AllNodes.FirstOrDefault(n =>
                n.NodeType == ProcessFlowNodeType.ProcessDetail &&
                n.ProcessDetail != null &&
                n.ProcessDetail.Id == updatedProcessDetail.Id);

            if (node != null && node.ProcessDetail != null)
            {
                // ProcessDetailオブジェクトのプロパティを更新
                node.ProcessDetail.DetailName = updatedProcessDetail.DetailName;
                node.ProcessDetail.OperationId = updatedProcessDetail.OperationId;
                node.ProcessDetail.StartSensor = updatedProcessDetail.StartSensor;
                node.ProcessDetail.CategoryId = updatedProcessDetail.CategoryId;
                node.ProcessDetail.FinishSensor = updatedProcessDetail.FinishSensor;
                node.ProcessDetail.BlockNumber = updatedProcessDetail.BlockNumber;
                node.ProcessDetail.SkipMode = updatedProcessDetail.SkipMode;
                node.ProcessDetail.CycleId = updatedProcessDetail.CycleId;
                node.ProcessDetail.SortNumber = updatedProcessDetail.SortNumber;
                node.ProcessDetail.Comment = updatedProcessDetail.Comment;
                node.ProcessDetail.ILStart = updatedProcessDetail.ILStart;
                node.ProcessDetail.StartTimerId = updatedProcessDetail.StartTimerId;
                node.ProcessDetail.IsResetAfter = updatedProcessDetail.IsResetAfter;

                // カテゴリ名を更新
                if (node.ProcessDetail.CategoryId.HasValue)
                {
                    var category = Categories.FirstOrDefault(c => c.Id == node.ProcessDetail.CategoryId.Value);
                    node.CategoryName = category?.CategoryName;
                }
                else
                {
                    node.CategoryName = null;
                }

                // 複合工程名を更新
                if (node.ProcessDetail.BlockNumber.HasValue)
                {
                    var process = Processes.FirstOrDefault(p => p.Id == node.ProcessDetail.BlockNumber.Value);
                    node.CompositeProcessName = process?.ProcessName;
                }
                else
                {
                    node.CompositeProcessName = null;
                }

                // 表示名を更新
                node.UpdateDisplayName();

                // 現在選択中のノードの場合は、プロパティも更新
                if (SelectedNode == node)
                {
                    SelectedNodeDetailName = node.ProcessDetail.DetailName ?? "";
                    SelectedNodeOperationId = node.ProcessDetail.OperationId;
                    SelectedNodeStartSensor = node.ProcessDetail.StartSensor ?? "";
                    SelectedNodeFinishSensor = node.ProcessDetail.FinishSensor ?? "";
                    SelectedNodeCategoryId = node.ProcessDetail.CategoryId;
                    SelectedNodeBlockNumber = node.ProcessDetail.BlockNumber;
                    SelectedNodeSkipMode = node.ProcessDetail.SkipMode ?? "";
                    SelectedNodeSortNumber = node.ProcessDetail.SortNumber;
                    SelectedNodeComment = node.ProcessDetail.Comment ?? "";
                    SelectedNodeILStart = node.ProcessDetail.ILStart ?? "";
                    SelectedNodeStartTimerId = node.ProcessDetail.StartTimerId;
                    SelectedNodeIsResetAfter = node.ProcessDetail.IsResetAfter;
                    SelectedNodeDisplayName = node.DisplayName;
                }

                // 接続線の開始センサー情報も更新
                var connections = AllConnections.Where(c => c.FromNode == node).ToList();
                foreach (var conn in connections)
                {
                    // ProcessDetailのStartSensorが変更された場合、接続線のセンサー表示も更新
                    conn.UpdatePosition();
                }

                System.Diagnostics.Debug.WriteLine($"Updated node display for ProcessDetail ID={updatedProcessDetail.Id}, Name={updatedProcessDetail.DetailName}");
            }
        }
    }
}
