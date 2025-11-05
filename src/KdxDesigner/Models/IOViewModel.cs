using CommunityToolkit.Mvvm.ComponentModel;

using DocumentFormat.OpenXml.Wordprocessing;

using KdxDesigner.Models;
using Kdx.Contracts.DTOs;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// IOモデルのラッパー。プロパティ変更通知を実装し、変更追跡を可能にする。
    /// </summary>
    public partial class IOViewModel : ObservableObject
    {
        private readonly IO _io;

        [ObservableProperty]
        private bool _isDirty; // 変更があったかどうかのフラグ

        // 複合キーのプロパティ
        public string Address
        {
            get => _io.Address;
            set { if (SetProperty(_io.Address, value, _io, (m, v) => m.Address = v)) IsDirty = true; }
        }
        
        public int PlcId
        {
            get => _io.PlcId;
            set { if (SetProperty(_io.PlcId, value, _io, (m, v) => m.PlcId = v)) IsDirty = true; }
        }

        public string? IOText
        {
            get => _io.IOText;
            set { if (SetProperty(_io.IOText, value, _io, (m, v) => m.IOText = v)) IsDirty = true; }
        }
        public string? XComment
        {
            get => _io.XComment;
            set { if (SetProperty(_io.XComment, value, _io, (m, v) => m.XComment = v)) IsDirty = true; }
        }
        public string? YComment
        {
            get => _io.XComment;
            set { if (SetProperty(_io.XComment, value, _io, (m, v) => m.XComment = v)) IsDirty = true; }
        }
        public string? FComment
        {
            get => _io.FComment;
            set { if (SetProperty(_io.FComment, value, _io, (m, v) => m.FComment = v)) IsDirty = true; }
        }
        public string? IOName
        {
            get => _io.IOName;
            set { if (SetProperty(_io.IOName, value, _io, (m, v) => m.IOName = v)) IsDirty = true; }
        }
        public string? IOExplanation
        {
            get => _io.IOExplanation;
            set { if (SetProperty(_io.IOExplanation, value, _io, (m, v) => m.IOExplanation = v)) IsDirty = true; }
        }
        public string? IOSpot
        {
            get => _io.IOSpot;
            set { if (SetProperty(_io.IOSpot, value, _io, (m, v) => m.IOSpot = v)) IsDirty = true; }
        }
        public string? UnitName
        {
            get => _io.UnitName;
            set { if (SetProperty(_io.UnitName, value, _io, (m, v) => m.UnitName = v)) IsDirty = true; }
        }
        public string? System
        {
            get => _io.System;
            set { if (SetProperty(_io.System, value, _io, (m, v) => m.System = v)) IsDirty = true; }
        }
        public string? StationNumber
        {
            get => _io.StationNumber;
            set { if (SetProperty(_io.StationNumber, value, _io, (m, v) => m.StationNumber = v)) IsDirty = true; }
        }
        public string? IONameNaked
        {
            get => _io.IONameNaked;
            set { if (SetProperty(_io.IONameNaked, value, _io, (m, v) => m.IONameNaked = v)) IsDirty = true; }
        }

        public string? LinkDevice
        {
            get => _io.LinkDevice;
            set { if (SetProperty(_io.LinkDevice, value, _io, (m, v) => m.LinkDevice = v)) IsDirty = true; }
        }

        public IOViewModel(IO io)
        {
            _io = io;
        }

        // 元のIOモデルオブジェクトを取得するメソッド
        public IO GetModel() => _io;
    }
}