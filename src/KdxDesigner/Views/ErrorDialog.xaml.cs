using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KdxDesigner.Views
{
    public partial class ErrorDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _dialogTitle = "エラー";
        private string _message = string.Empty;

        public new string Title
        {
            get => _dialogTitle;
            set
            {
                _dialogTitle = value;
                base.Title = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public ErrorDialog(string message, string title = "エラー")
        {
            InitializeComponent();
            DataContext = this;
            Title = title;
            Message = message;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(Message);
                MessageBox.Show("クリップボードにコピーしました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// エラーダイアログを表示するヘルパーメソッド
        /// </summary>
        public static void Show(string message, string title = "エラー", Window? owner = null)
        {
            var dialog = new ErrorDialog(message, title);
            if (owner != null)
            {
                dialog.Owner = owner;
            }
            dialog.ShowDialog();
        }
    }
}
