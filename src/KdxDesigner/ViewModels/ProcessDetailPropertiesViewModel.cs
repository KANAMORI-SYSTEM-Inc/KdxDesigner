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

        public bool DialogResult { get; private set; }

        public ProcessDetailPropertiesViewModel(ISupabaseRepository repository, ProcessDetail processDetail, int? cycleId)
        {
            _repository = repository;
            _processDetail = processDetail;

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
                // Operationリストを読み込み
                if (cycleId.HasValue)
                {
                    var operations = await _repository.GetOperationsByCycleIdAsync(cycleId.Value);
                    Operations = new ObservableCollection<Operation>(operations);
                }

                // ProcessDetailCategoryリストを読み込み
                var categories = await _repository.GetProcessDetailCategoriesAsync();
                ProcessDetailCategories = new ObservableCollection<ProcessDetailCategory>(categories);
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

                DialogResult = true;
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
        }
    }
}
