using CommunityToolkit.Mvvm.ComponentModel;
using KdxDesigner.Models;
using Kdx.Contracts.DTOs;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// MnemonicTimerDeviceの表示用ViewModel
    /// </summary>
    public partial class MnemonicTimerDeviceViewModel : ObservableObject
    {
        private readonly MnemonicTimerDevice _model;
        private bool _isDirty;

        public MnemonicTimerDeviceViewModel(MnemonicTimerDevice model)
        {
            _model = model;
        }

        public int MnemonicId => _model.MnemonicId;
        public int RecordId => _model.RecordId;
        public int TimerId => _model.TimerId;
        public int? TimerCategoryId => _model.TimerCategoryId;
        public int PlcId => _model.PlcId;
        public int? CycleId => _model.CycleId;

        [ObservableProperty]
        private string? _mnemonicName;

        [ObservableProperty]
        private string? _recordName;

        [ObservableProperty]
        private string? _timerName;

        [ObservableProperty]
        private string? _categoryName;

        public string ProcessTimerDevice
        {
            get => _model.TimerDeviceT;
            set
            {
                if (_model.TimerDeviceT != value)
                {
                    _model.TimerDeviceT = value;
                    OnPropertyChanged();
                    IsDirty = true;
                }
            }
        }

        public string TimerDevice
        {
            get => _model.TimerDeviceZR;
            set
            {
                if (_model.TimerDeviceZR != value)
                {
                    _model.TimerDeviceZR = value;
                    OnPropertyChanged();
                    IsDirty = true;
                }
            }
        }

        public string? Comment1
        {
            get => _model.Comment1;
            set
            {
                if (_model.Comment1 != value)
                {
                    _model.Comment1 = value;
                    OnPropertyChanged();
                    IsDirty = true;
                }
            }
        }

        public string? Comment2
        {
            get => _model.Comment2;
            set
            {
                if (_model.Comment2 != value)
                {
                    _model.Comment2 = value;
                    OnPropertyChanged();
                    IsDirty = true;
                }
            }
        }

        public string? Comment3
        {
            get => _model.Comment3;
            set
            {
                if (_model.Comment3 != value)
                {
                    _model.Comment3 = value;
                    OnPropertyChanged();
                    IsDirty = true;
                }
            }
        }

        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

        public MnemonicTimerDevice GetModel() => _model;
    }
}