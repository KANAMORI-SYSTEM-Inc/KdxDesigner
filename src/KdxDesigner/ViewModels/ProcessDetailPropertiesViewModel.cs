using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// 工程詳細プロパティウィンドウのViewModel
    /// </summary>
    public partial class ProcessDetailPropertiesViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private ProcessDetail _processDetail;
        private readonly int? _constructorCycleId;  // コンストラクタから受け取ったCycleIdを保持

        [ObservableProperty] private int _id;
        [ObservableProperty] private int _processId;
        [ObservableProperty] private int? _operationId;
        [ObservableProperty] private string? _detailName;
        [ObservableProperty] private string? _startSensor;
        [ObservableProperty] private int? _categoryId;
        [ObservableProperty] private string? _finishSensor;
        [ObservableProperty] private int? _blockNumber;
        [ObservableProperty] private string? _skipMode;
        [ObservableProperty] private int? _cycleId;
        [ObservableProperty] private int? _sortNumber;
        [ObservableProperty] private string? _comment;
        [ObservableProperty] private string? _iLStart;
        [ObservableProperty] private int? _startTimerId;

        [ObservableProperty] private ObservableCollection<Operation> _operations = new();
        [ObservableProperty] private ObservableCollection<ProcessDetailCategory> _processDetailCategories = new();
        [ObservableProperty] private ObservableCollection<ProcessDetailSelectionModel> _availableProcessDetails = new();
        [ObservableProperty] private ObservableCollection<ProcessDetailSelectionModel> _availableFinishProcessDetails = new();

        public bool DialogResult { get; private set; }

        /// <summary>
        /// ProcessDetailの選択モデル（マルチセレクト用）
        /// </summary>
        public partial class ProcessDetailSelectionModel : ObservableObject
        {
            public ProcessDetail ProcessDetail { get; set; } = new();
            [ObservableProperty] private bool _isSelected;
        }

        public ProcessDetailPropertiesViewModel(ISupabaseRepository repository, ProcessDetail processDetail, int? cycleId)
        {
            _repository = repository;
            _processDetail = processDetail;
            _constructorCycleId = cycleId;  // CycleIdを保持

            // マスターデータを読み込み
            LoadMasterData(cycleId);

            // ProcessDetailのプロパティを読み込み
            LoadProcessDetailProperties();
        }

        /// <summary>
        /// マスターデータを読み込み
        /// </summary>
        private async void LoadMasterData(int? cycleId)
        {
            try
            {
                // CycleIdを取得（ProcessDetailのCycleIdまたはProcessのCycleIdから）
                int? actualCycleId = cycleId;

                // ProcessDetailにCycleIdが設定されていない場合、ProcessからCycleIdを取得
                if (!actualCycleId.HasValue && _processDetail.ProcessId > 0)
                {
                    var processes = await _repository.GetProcessesAsync();
                    var process = processes.FirstOrDefault(p => p.Id == _processDetail.ProcessId);
                    actualCycleId = process?.CycleId;
                }

                // Operationリストを読み込み
                if (actualCycleId.HasValue)
                {
                    var operations = await _repository.GetOperationsByCycleIdAsync(actualCycleId.Value);
                    Operations = new ObservableCollection<Operation>(operations);
                }

                // ProcessDetailCategoryリストを読み込み
                var categories = await _repository.GetProcessDetailCategoriesAsync();
                ProcessDetailCategories = new ObservableCollection<ProcessDetailCategory>(categories);

                // 同一Cycle内のProcessDetailリストを読み込み（接続先候補）
                if (actualCycleId.HasValue)
                {
                    var allProcessDetails = await _repository.GetProcessDetailsAsync();
                    var processDetailsInCycle = allProcessDetails
                        .Where(pd => pd.CycleId == actualCycleId.Value && pd.Id != _processDetail.Id)
                        .OrderBy(pd => pd.SortNumber)
                        .ToList();

                    // 既存の接続を取得
                    var existingConnections = await _repository.GetConnectionsByFromIdAsync(_processDetail.Id);
                    var connectedIds = existingConnections.Select(c => c.ToProcessDetailId).ToHashSet();

                    // 選択モデルを作成
                    var selections = processDetailsInCycle.Select(pd => new ProcessDetailSelectionModel
                    {
                        ProcessDetail = pd,
                        IsSelected = connectedIds.Contains(pd.Id)
                    }).ToList();

                    AvailableProcessDetails = new ObservableCollection<ProcessDetailSelectionModel>(selections);

                    // 終了条件用のProcessDetailリストを読み込み（同じ候補を使用）
                    var existingFinishes = await _repository.GetFinishesByProcessDetailIdAsync(_processDetail.Id);
                    var finishIds = existingFinishes.Select(f => f.FinishProcessDetailId).ToHashSet();

                    var finishSelections = processDetailsInCycle.Select(pd => new ProcessDetailSelectionModel
                    {
                        ProcessDetail = pd,
                        IsSelected = finishIds.Contains(pd.Id)
                    }).ToList();

                    AvailableFinishProcessDetails = new ObservableCollection<ProcessDetailSelectionModel>(finishSelections);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"マスターデータの読み込み中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ProcessDetailのプロパティをロード
        /// </summary>
        private void LoadProcessDetailProperties()
        {
            Id = _processDetail.Id;
            ProcessId = _processDetail.ProcessId;
            OperationId = _processDetail.OperationId;
            DetailName = _processDetail.DetailName;
            StartSensor = _processDetail.StartSensor;
            CategoryId = _processDetail.CategoryId;
            FinishSensor = _processDetail.FinishSensor;
            BlockNumber = _processDetail.BlockNumber;
            SkipMode = _processDetail.SkipMode;
            CycleId = _processDetail.CycleId;
            SortNumber = _processDetail.SortNumber;
            Comment = _processDetail.Comment;
            ILStart = _processDetail.ILStart;
            StartTimerId = _processDetail.StartTimerId;
        }

        /// <summary>
        /// 保存コマンド
        /// </summary>
        [RelayCommand]
        private async Task Save()
        {
            try
            {
                // ProcessDetailのプロパティを更新
                _processDetail.OperationId = OperationId;
                _processDetail.DetailName = DetailName;
                _processDetail.StartSensor = StartSensor;
                _processDetail.CategoryId = CategoryId;
                _processDetail.FinishSensor = FinishSensor;
                _processDetail.BlockNumber = BlockNumber;
                _processDetail.SkipMode = SkipMode;
                _processDetail.CycleId = CycleId;
                _processDetail.SortNumber = SortNumber;
                _processDetail.Comment = Comment;
                _processDetail.ILStart = ILStart;
                _processDetail.StartTimerId = StartTimerId;

                // データベースに保存
                await _repository.UpdateProcessDetailAsync(_processDetail);

                // ProcessDetailConnection を保存
                // まず既存の接続を全て削除
                await _repository.DeleteConnectionsByFromIdAsync(_processDetail.Id);

                // 選択されたProcessDetailとの接続を作成
                var selectedProcessDetails = AvailableProcessDetails.Where(s => s.IsSelected).ToList();
                System.Diagnostics.Debug.WriteLine($"=== ProcessDetailConnection 保存開始 (ProcessDetailPropertiesViewModel) ===");
                System.Diagnostics.Debug.WriteLine($"  ToProcessDetail: {_processDetail.DetailName} (ID: {_processDetail.Id})");
                System.Diagnostics.Debug.WriteLine($"  保存する接続数: {selectedProcessDetails.Count}");

                foreach (var selection in selectedProcessDetails)
                {
                    var connection = new ProcessDetailConnection
                    {
                        FromProcessDetailId = selection.ProcessDetail.Id,  // 選択されたProcessDetailがFrom
                        ToProcessDetailId = _processDetail.Id,              // 編集中のProcessDetailがTo
                        CycleId = _constructorCycleId ?? _processDetail.CycleId  // コンストラクタのcycleIdを優先、なければProcessDetailのCycleId
                    };

                    System.Diagnostics.Debug.WriteLine($"  → 接続 {selectedProcessDetails.IndexOf(selection) + 1}/{selectedProcessDetails.Count}:");
                    System.Diagnostics.Debug.WriteLine($"     FromProcessDetailId: {connection.FromProcessDetailId}");
                    System.Diagnostics.Debug.WriteLine($"     ToProcessDetailId: {connection.ToProcessDetailId}");
                    System.Diagnostics.Debug.WriteLine($"     CycleId: {connection.CycleId}");
                    System.Diagnostics.Debug.WriteLine($"     From: {selection.ProcessDetail.DetailName} (ID: {selection.ProcessDetail.Id})");

                    try
                    {
                        await _repository.AddProcessDetailConnectionAsync(connection);
                        System.Diagnostics.Debug.WriteLine($"     ✓ 保存成功");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"     ✗ 保存失敗: {ex.Message}");
                        throw; // エラーを再スロー
                    }
                }
                System.Diagnostics.Debug.WriteLine("=========================================================");

                // ProcessDetailFinish を保存
                // まず既存の終了条件を全て削除
                await _repository.DeleteFinishesByProcessDetailIdAsync(_processDetail.Id);

                // 選択されたProcessDetailとの終了条件を作成
                var selectedFinishProcessDetails = AvailableFinishProcessDetails.Where(s => s.IsSelected).ToList();
                foreach (var selection in selectedFinishProcessDetails)
                {
                    var finish = new ProcessDetailFinish
                    {
                        ProcessDetailId = _processDetail.Id,
                        FinishProcessDetailId = selection.ProcessDetail.Id,
                        CycleId = _constructorCycleId ?? _processDetail.CycleId ?? 0
                    };
                    await _repository.AddProcessDetailFinishAsync(finish);
                }

                DialogResult = true;

                // 保存完了イベントを発火（更新されたProcessDetailを渡す）
                ProcessDetailSaved?.Invoke(this, _processDetail);

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// キャンセルコマンド
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke();
        }

        public event Action? RequestClose;

        /// <summary>
        /// ProcessDetailが保存された時に発火するイベント
        /// </summary>
        public event EventHandler<ProcessDetail>? ProcessDetailSaved;

        /// <summary>
        /// ProcessDetailオブジェクトを更新
        /// </summary>
        public void UpdateProcessDetail(ProcessDetail processDetail)
        {
            _processDetail = processDetail;
            LoadProcessDetailProperties();
        }

        /// <summary>
        /// イベントハンドラをクリア
        /// </summary>
        public void ClearEventHandlers()
        {
            RequestClose = null;
            ProcessDetailSaved = null;
        }
    }
}
