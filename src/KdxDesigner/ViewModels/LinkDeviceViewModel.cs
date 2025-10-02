using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Kdx.Contracts.DTOs;
using KdxDesigner.Models;
using Kdx.Contracts.Interfaces;
using KdxDesigner.Services.LinkDevice;

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows; // MessageBoxのため

namespace KdxDesigner.ViewModels
{
    public partial class LinkDeviceViewModel : ObservableObject
    {
        private readonly IAccessRepository _repository;
        private readonly LinkDeviceService _linkDeviceService;

        /// <summary>
        /// ComboBoxに表示するための、利用可能な全てのPLCのリスト。
        /// </summary>
        public ObservableCollection<PLC> AvailablePlcs { get; }

        /// <summary>
        /// ユーザーが選択したメインPLC。
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PlcLinkSettings))]
        private PLC? _selectedMainPlc;

        /// <summary>
        /// 従属PLCとして選択可能なPLCのリスト。メインPLC自身は除外される。
        /// </summary>
        public ObservableCollection<PlcLinkSettingViewModel> PlcLinkSettings { get; } = new();

        public LinkDeviceViewModel(IAccessRepository repository)
        {
            _repository = repository;
            _linkDeviceService = new LinkDeviceService(_repository);

            // データベースから利用可能なPLCをすべて読み込む
            AvailablePlcs = new ObservableCollection<PLC>(_repository.GetPLCs());
        }

        /// <summary>
        /// メインPLCが変更されたときに自動的に呼び出されるメソッド。
        /// </summary>
        partial void OnSelectedMainPlcChanged(PLC? value)
        {
            // 従属PLCのリストをクリアし、再構築する
            PlcLinkSettings.Clear();

            if (value != null)
            {
                // メインPLCとして選択されたものを除く、すべてのPLCを従属PLCの候補としてリストに追加
                var subordinateCandidates = AvailablePlcs.Where(p => p.Id != value.Id);
                foreach (var plc in subordinateCandidates)
                {
                    PlcLinkSettings.Add(new PlcLinkSettingViewModel(plc));
                }
            }
        }

        [RelayCommand]
        private void ExecuteLink()
        {
            // 1. 入力検証
            if (SelectedMainPlc == null)
            {
                MessageBox.Show("メインPLCが選択されていません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedSettings = PlcLinkSettings.Where(s => s.IsSelected).ToList();
            if (!selectedSettings.Any())
            {
                MessageBox.Show("リンク対象の従属PLCが1つも選択されていません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (selectedSettings.Any(s => string.IsNullOrWhiteSpace(s.XDeviceStart) || string.IsNullOrWhiteSpace(s.YDeviceStart)))
            {
                MessageBox.Show("リンク対象に設定したPLCの先頭デバイスを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 2. LinkDeviceServiceに処理を委譲
            try
            {
                bool success = _linkDeviceService.CreateLinkDeviceRecords(SelectedMainPlc, selectedSettings);
                if (success)
                {
                    MessageBox.Show("リンクデバイスの登録が完了しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("リンクデバイスの登録に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}", "実行時エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}