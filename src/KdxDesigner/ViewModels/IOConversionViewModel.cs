using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Services;
using Kdx.Infrastructure.Supabase.Repositories;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// IO割付表の変換画面用ViewModel。Excelファイルからの変換とデータベース保存を管理。
    /// </summary>
    public partial class IOConversionViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repository;
        private readonly IOConversionService _conversionService;

        [ObservableProperty]
        private ObservableCollection<PLC> _plcs = new();

        [ObservableProperty]
        private PLC? _selectedPlc;

        [ObservableProperty]
        private string _sourceFilePath = string.Empty;

        [ObservableProperty]
        private string _sheetNames = string.Empty;

        [ObservableProperty]
        private bool _includeCircle;

        [ObservableProperty]
        private bool _useNewNaming;

        [ObservableProperty]
        private ObservableCollection<IO> _convertedIOs = new();

        [ObservableProperty]
        private bool _hasConvertedData;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public IOConversionViewModel(ISupabaseRepository repository)
        {
            _repository = repository;
            _conversionService = new IOConversionService();

            LoadPlcsAsync();
        }

        /// <summary>
        /// PLCリストを読み込む
        /// </summary>
        private async void LoadPlcsAsync()
        {
            try
            {
                var plcs = await _repository.GetPLCsAsync();
                Plcs.Clear();
                foreach (var plc in plcs)
                {
                    Plcs.Add(plc);
                }

                // デフォルトで最初のPLCを選択
                if (Plcs.Count > 0)
                {
                    SelectedPlc = Plcs[0];
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"PLCリスト読み込みエラー: {ex.Message}";
            }
        }

        /// <summary>
        /// Excelファイルを選択するコマンド
        /// </summary>
        [RelayCommand]
        private void SelectExcelFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                Title = "IO割付表Excelファイルを選択"
            };

            if (dialog.ShowDialog() == true)
            {
                SourceFilePath = dialog.FileName;
                StatusMessage = $"ファイル選択: {System.IO.Path.GetFileName(SourceFilePath)}";
            }
        }

        /// <summary>
        /// Excel変換を実行するコマンド
        /// </summary>
        [RelayCommand]
        private void ConvertExcel()
        {
            try
            {
                // バリデーション
                if (string.IsNullOrWhiteSpace(SourceFilePath))
                {
                    MessageBox.Show("Excelファイルを選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!System.IO.File.Exists(SourceFilePath))
                {
                    MessageBox.Show("指定されたファイルが存在しません。", "ファイルエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(SheetNames))
                {
                    MessageBox.Show("シート名を入力してください。（カンマ区切りで複数指定可能）", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedPlc == null)
                {
                    MessageBox.Show("PLCを選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = "変換中...";

                // 変換設定を構築
                var settings = new IOConversionService.ConversionSettings
                {
                    SourceFilePath = SourceFilePath,
                    SheetNames = SheetNames,
                    IncludeCircle = IncludeCircle,
                    UseNewNaming = UseNewNaming,
                    PlcId = SelectedPlc.Id
                };

                // 変換実行
                var result = _conversionService.ConvertExcelToIOList(settings);

                ConvertedIOs.Clear();
                foreach (var io in result)
                {
                    ConvertedIOs.Add(io);
                }

                HasConvertedData = ConvertedIOs.Count > 0;
                StatusMessage = $"変換完了: {ConvertedIOs.Count}件のIOデータを取得しました。";

                if (ConvertedIOs.Count == 0)
                {
                    MessageBox.Show("変換結果が0件でした。シート名やファイル形式を確認してください。",
                        "変換結果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"変換エラー: {ex.Message}";
                MessageBox.Show($"変換中にエラーが発生しました:\n{ex.Message}",
                    "変換エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// データベースに保存するコマンド
        /// </summary>
        [RelayCommand]
        private async Task SaveToDatabaseAsync()
        {
            try
            {
                if (ConvertedIOs.Count == 0)
                {
                    MessageBox.Show("保存するデータがありません。先に変換を実行してください。",
                        "保存エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"{ConvertedIOs.Count}件のIOデータをデータベースに保存しますか？\n" +
                    $"PLC ID: {SelectedPlc?.Id}",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                StatusMessage = "データベースに保存中...";

                // IOデータをリストに変換
                var ioList = ConvertedIOs.ToList();

                // データベースに保存
                // 既存のUpdateAndLogIoChangesAsyncを使用（履歴は空リスト）
                var emptyHistories = new List<IOHistory>();
                await _repository.UpdateAndLogIoChangesAsync(ioList, emptyHistories);

                StatusMessage = $"保存完了: {ioList.Count}件のIOデータを保存しました。";
                MessageBox.Show($"{ioList.Count}件のIOデータを正常に保存しました。",
                    "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存エラー: {ex.Message}";
                MessageBox.Show($"保存中にエラーが発生しました:\n{ex.Message}",
                    "保存エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 変換データをクリアするコマンド
        /// </summary>
        [RelayCommand]
        private void ClearConvertedData()
        {
            ConvertedIOs.Clear();
            HasConvertedData = false;
            StatusMessage = "変換データをクリアしました。";
        }
    }
}
