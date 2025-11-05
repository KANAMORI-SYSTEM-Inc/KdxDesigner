using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Kdx.Contracts.DTOs;
using Timer = Kdx.Contracts.DTOs.Timer;
using KdxDesigner.Models;

namespace KdxDesigner.ViewModels
{
    public partial class TimerViewModel : ObservableObject
    {
        private readonly Timer _timer;
        private bool _isDirty;
        private List<int> _recordIds;

        public TimerViewModel(Timer timer)
        {
            _timer = timer;
            _isDirty = false;
            _recordIds = new List<int>();
        }

        public Timer GetModel() => _timer;

        public int ID => _timer.ID;

        [ObservableProperty]
        private int? _cycleId;
        partial void OnCycleIdChanged(int? value)
        {
            if (_timer.CycleId != value)
            {
                _timer.CycleId = value;
                IsDirty = true;
            }
        }

        [ObservableProperty]
        private int? _timerCategoryId;
        partial void OnTimerCategoryIdChanged(int? value)
        {
            if (_timer.TimerCategoryId != value)
            {
                _timer.TimerCategoryId = value;
                IsDirty = true;
            }
        }

        [ObservableProperty]
        private int? _timerNum;
        partial void OnTimerNumChanged(int? value)
        {
            if (_timer.TimerNum != value)
            {
                _timer.TimerNum = value;
                IsDirty = true;
            }
        }

        [ObservableProperty]
        private string? _timerName;
        partial void OnTimerNameChanged(string? value)
        {
            if (_timer.TimerName != value)
            {
                _timer.TimerName = value;
                IsDirty = true;
            }
        }

        [ObservableProperty]
        private int? _mnemonicId;
        partial void OnMnemonicIdChanged(int? value)
        {
            if (_timer.MnemonicId != value)
            {
                var oldValue = _timer.MnemonicId;
                _timer.MnemonicId = value;
                IsDirty = true;
                
                // MnemonicTypeが変更された場合、RecordIdsをクリアするかユーザーに確認
                if (oldValue != value && RecordIds.Any())
                {
                    OnMnemonicTypeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        
        public event EventHandler? OnMnemonicTypeChanged;

        [ObservableProperty]
        private int? _example;
        partial void OnExampleChanged(int? value)
        {
            if (_timer.Example != value)
            {
                _timer.Example = value;
                IsDirty = true;
            }
        }

        // UI表示用プロパティ
        [ObservableProperty]
        private string? _categoryName;

        [ObservableProperty]
        private string? _cycleName;

        [ObservableProperty]
        private string? _mnemonicTypeName;

        [ObservableProperty]
        private string _recordIdsDisplay = "";

        public List<int> RecordIds
        {
            get => _recordIds;
            set
            {
                // 既存の値と比較して変更があればIsDirtyをtrueに
                var newList = value ?? new List<int>();
                var hasChanged = !_recordIds.SequenceEqual(newList);
                
                _recordIds = newList;
                RecordIdsDisplay = string.Join(", ", _recordIds);
                OnPropertyChanged(nameof(RecordIds));
                
                if (hasChanged)
                {
                    IsDirty = true;
                }
            }
        }

        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

        public void LoadFromModel()
        {
            CycleId = _timer.CycleId;
            TimerCategoryId = _timer.TimerCategoryId;
            TimerNum = _timer.TimerNum;
            TimerName = _timer.TimerName;
            MnemonicId = _timer.MnemonicId;
            Example = _timer.Example;
            
            // RecordIdsは中間テーブルから読み込まれるため、ここでは初期化しない
            // RecordIdsプロパティは既に削除されている
            
            IsDirty = false;
        }

        public void ResetDirty()
        {
            IsDirty = false;
        }
    }
}