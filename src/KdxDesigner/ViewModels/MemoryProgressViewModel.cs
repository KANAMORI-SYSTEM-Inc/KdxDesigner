using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    /// <summary>
    /// メモリ設定進捗ウィンドウのViewModel
    /// </summary>
    public partial class MemoryProgressViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _progressMax = 100;

        [ObservableProperty]
        private int _progressValue = 0;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _logMessages = string.Empty;

        [ObservableProperty]
        private bool _isCompleted = false;

        private readonly StringBuilder _logBuilder = new StringBuilder();

        /// <summary>
        /// ログメッセージを追加
        /// </summary>
        public void AddLog(string message)
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                _logBuilder.AppendLine($"[{timestamp}] {message}");
                LogMessages = _logBuilder.ToString();
                OnPropertyChanged(nameof(LogMessages));
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// ステータスメッセージを更新
        /// </summary>
        public void UpdateStatus(string message)
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = message;
                OnPropertyChanged(nameof(StatusMessage));
            }, System.Windows.Threading.DispatcherPriority.Background);

            // ログは別途追加
            AddLog(message);
        }

        /// <summary>
        /// プログレスバーの最大値を設定
        /// </summary>
        public void SetProgressMax(int max)
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressMax = max;
                ProgressValue = 0;
                OnPropertyChanged(nameof(ProgressMax));
                OnPropertyChanged(nameof(ProgressValue));
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// プログレスバーの値を更新
        /// </summary>
        public void IncrementProgress()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressValue++;
                OnPropertyChanged(nameof(ProgressValue));
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// 処理完了をマーク
        /// </summary>
        public void MarkCompleted()
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                IsCompleted = true;
                StatusMessage = "処理が完了しました";
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(StatusMessage));
            }, System.Windows.Threading.DispatcherPriority.Normal);

            AddLog("=== 処理完了 ===");
        }

        /// <summary>
        /// エラーをマーク
        /// </summary>
        public void MarkError(string errorMessage)
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                IsCompleted = true;
                StatusMessage = $"エラー: {errorMessage}";
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(StatusMessage));
            }, System.Windows.Threading.DispatcherPriority.Normal);

            AddLog($"!!! エラー: {errorMessage} !!!");
        }
    }
}
