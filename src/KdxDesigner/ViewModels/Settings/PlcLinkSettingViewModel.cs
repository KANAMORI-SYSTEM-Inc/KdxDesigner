using CommunityToolkit.Mvvm.ComponentModel;

using Kdx.Contracts.DTOs;
using KdxDesigner.Models;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// リンクデバイス設定画面のDataGridの各行を表すViewModel。
    /// </summary>
    public partial class PlcLinkSettingViewModel : ObservableObject
    {
        // 表示するPLCの情報
        public PLC Plc { get; }

        // UIのチェックボックスとバインドするプロパティ
        [ObservableProperty]
        private bool _isSelected;

        // UIのテキストボックスとバインドするプロパティ
        [ObservableProperty]
        private string? _xDeviceStart;

        [ObservableProperty]
        private string? _yDeviceStart;

        public PlcLinkSettingViewModel(PLC plc)
        {
            Plc = plc;
        }
    }
}