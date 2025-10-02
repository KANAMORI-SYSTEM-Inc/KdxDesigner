using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Kdx.Contracts.DTOs;
using KdxDesigner.Models;

using System.Collections.ObjectModel;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class IOSelectViewModel : ObservableObject
    {
        /// <summary>
        /// ListBoxに表示するための各項目。表示名と実際の値を保持します。
        /// </summary>
        public class AddressItem
        {
            public string Display { get; set; } = string.Empty;
            public IO OriginalIO { get; set; } = new IO();// ★元のIOオブジェクトを保持
        }

        [ObservableProperty]
        private ObservableCollection<AddressItem> _addressItems = new();

        [ObservableProperty]
        private AddressItem? _selectedItem;

        /// <summary>
        /// ユーザーが最終的に選択したIOオブジェクト。
        /// </summary>
        public IO? SelectedIO => SelectedItem?.OriginalIO;

        /// <summary>
        /// View（コードビハインド）がダイアログの結果を判断するためのプロパティ。
        /// </summary>
        [ObservableProperty]
        private bool? _dialogResult;

        /// <summary>
        /// ★【新規】UIに表示するための、検索コンテキスト情報メッセージ。
        /// </summary>
        public string ContextMessage { get; }


        /// <summary>
        /// コンストラクタでIO候補リストと、表示用のコンテキスト情報を受け取ります。
        /// </summary>
        public IOSelectViewModel(string ioText, List<IO> candidates, string recordName, int? recordId)
        {
            // 1. コンテキストメッセージを生成
            ContextMessage = $"対象:「{recordName}」(ID:{recordId}) の検索キーワード '{ioText}' で、複数のIO候補が見つかりました。\n使用するものを選択してください。";

            // 2. 表示用アイテムリストを生成
            AddressItems = new ObservableCollection<AddressItem>(
                candidates.Select(io => new AddressItem
                {
                    Display = $"{io.IOName} ({io.Address}) - {io.IOExplanation}",
                    OriginalIO = io
                })
            );
        }

        [RelayCommand]
        private void Confirm()
        {
            if (SelectedItem != null)
            {
                // ダイアログの結果をtrueに設定して、Viewに通知
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("リストから項目を選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            // ダイアログの結果をfalseに設定して、Viewに通知
            DialogResult = false;
        }
    }
}