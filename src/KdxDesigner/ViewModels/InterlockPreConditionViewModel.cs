using CommunityToolkit.Mvvm.Input;
using Kdx.Contracts.DTOs;
using Kdx.Infrastructure.Supabase.Repositories;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace KdxDesigner.ViewModels
{
    public class InterlockPreConditionViewModel : INotifyPropertyChanged
    {
        private readonly SupabaseRepository _supabaseRepository;
        private readonly Interlock _interlock;
        private readonly Window _window;

        private InterlockPrecondition1? _selectedPreCondition1;
        private InterlockPrecondition2? _selectedPreCondition2;
        private bool _isPreCondition1Selected;
        private bool _isPreCondition2Selected;

        public ObservableCollection<InterlockPrecondition1> PreCondition1List { get; set; }
        public ObservableCollection<InterlockPrecondition2> PreCondition2List { get; set; }

        public InterlockPrecondition1? SelectedPreCondition1
        {
            get => _selectedPreCondition1;
            set
            {
                _selectedPreCondition1 = value;
                OnPropertyChanged();
                // 選択されたPreCondition1がInterlockに設定されているものと一致するか確認
                if (_selectedPreCondition1 != null && _interlock.PreConditionID1 == _selectedPreCondition1.Id)
                {
                    IsPreCondition1Selected = true;
                }
            }
        }

        public InterlockPrecondition2? SelectedPreCondition2
        {
            get => _selectedPreCondition2;
            set
            {
                _selectedPreCondition2 = value;
                OnPropertyChanged();
                // 選択されたPreCondition2がInterlockに設定されているものと一致するか確認
                if (_selectedPreCondition2 != null && _interlock.PreConditionID2 == _selectedPreCondition2.Id)
                {
                    IsPreCondition2Selected = true;
                }
            }
        }

        public bool IsPreCondition1Selected
        {
            get => _isPreCondition1Selected;
            set
            {
                _isPreCondition1Selected = value;
                OnPropertyChanged();
            }
        }

        public bool IsPreCondition2Selected
        {
            get => _isPreCondition2Selected;
            set
            {
                _isPreCondition2Selected = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddPreCondition1Command { get; }
        public ICommand DeletePreCondition1Command { get; }
        public ICommand AddPreCondition2Command { get; }
        public ICommand DeletePreCondition2Command { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public InterlockPreConditionViewModel(SupabaseRepository supabaseRepository, Interlock interlock, Window window)
        {
            _supabaseRepository = supabaseRepository;
            _interlock = interlock;
            _window = window;

            PreCondition1List = new ObservableCollection<InterlockPrecondition1>();
            PreCondition2List = new ObservableCollection<InterlockPrecondition2>();

            AddPreCondition1Command = new RelayCommand(() => AddPreCondition1(null));
            DeletePreCondition1Command = new RelayCommand(() => DeletePreCondition1(null), () => CanDeletePreCondition1(null));
            AddPreCondition2Command = new RelayCommand(() => AddPreCondition2(null));
            DeletePreCondition2Command = new RelayCommand(() => DeletePreCondition2(null), () => CanDeletePreCondition2(null));
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => Cancel(null));

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // PreCondition1のリストを取得
                var preCondition1List = await _supabaseRepository.GetInterlockPrecondition1ListAsync();
                PreCondition1List.Clear();
                foreach (var item in preCondition1List)
                {
                    PreCondition1List.Add(item);
                }

                // PreCondition2のリストを取得
                var preCondition2List = await _supabaseRepository.GetInterlockPrecondition2ListAsync();
                PreCondition2List.Clear();
                foreach (var item in preCondition2List)
                {
                    PreCondition2List.Add(item);
                }

                // 現在のInterlockに設定されているPreConditionを選択
                if (_interlock.PreConditionID1.HasValue)
                {
                    SelectedPreCondition1 = PreCondition1List.FirstOrDefault(p => p.Id == _interlock.PreConditionID1.Value);
                    IsPreCondition1Selected = SelectedPreCondition1 != null;
                }

                if (_interlock.PreConditionID2.HasValue)
                {
                    SelectedPreCondition2 = PreCondition2List.FirstOrDefault(p => p.Id == _interlock.PreConditionID2.Value);
                    IsPreCondition2Selected = SelectedPreCondition2 != null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPreCondition1(object? parameter)
        {
            var newCondition = new InterlockPrecondition1
            {
                ConditionName = "新規条件",
                Discription = ""
            };
            PreCondition1List.Add(newCondition);
            SelectedPreCondition1 = newCondition;
        }

        private bool CanDeletePreCondition1(object? parameter) => SelectedPreCondition1 != null;

        private void DeletePreCondition1(object? parameter)
        {
            if (SelectedPreCondition1 == null) return;

            var result = MessageBox.Show("選択した前提条件1を削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                PreCondition1List.Remove(SelectedPreCondition1);
                SelectedPreCondition1 = null;
                IsPreCondition1Selected = false;
            }
        }

        private void AddPreCondition2(object? parameter)
        {
            var newCondition = new InterlockPrecondition2
            {
                InterlockMode = "新規モード",
                IsEnableProcess = false
            };
            PreCondition2List.Add(newCondition);
            SelectedPreCondition2 = newCondition;
        }

        private bool CanDeletePreCondition2(object? parameter) => SelectedPreCondition2 != null;

        private void DeletePreCondition2(object? parameter)
        {
            if (SelectedPreCondition2 == null) return;

            var result = MessageBox.Show("選択した前提条件2を削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                PreCondition2List.Remove(SelectedPreCondition2);
                SelectedPreCondition2 = null;
                IsPreCondition2Selected = false;
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                // PreCondition1の保存/更新
                if (PreCondition1List.Any())
                {
                    await _supabaseRepository.UpsertInterlockPrecondition1ListAsync(PreCondition1List.ToList());
                }

                // PreCondition2の保存/更新
                if (PreCondition2List.Any())
                {
                    await _supabaseRepository.UpsertInterlockPrecondition2ListAsync(PreCondition2List.ToList());
                }

                // InterlockのPreConditionIDを更新
                if (IsPreCondition1Selected && SelectedPreCondition1 != null)
                {
                    _interlock.PreConditionID1 = SelectedPreCondition1.Id;
                }
                else
                {
                    _interlock.PreConditionID1 = null;
                }

                if (IsPreCondition2Selected && SelectedPreCondition2 != null)
                {
                    _interlock.PreConditionID2 = SelectedPreCondition2.Id;
                }
                else
                {
                    _interlock.PreConditionID2 = null;
                }

                MessageBox.Show("前提条件を保存しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                _window.DialogResult = true;
                _window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object? parameter)
        {
            _window.DialogResult = false;
            _window.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}