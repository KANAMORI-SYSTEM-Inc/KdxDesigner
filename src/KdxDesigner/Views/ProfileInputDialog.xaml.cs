using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KdxDesigner.Views
{
    /// <summary>
    /// プロファイル入力ダイアログ
    /// プロファイル名と説明を入力するための汎用ダイアログ
    /// </summary>
    public partial class ProfileInputDialog : Window, INotifyPropertyChanged
    {
        private string _dialogTitle = "プロファイル作成";
        private string _profileName = string.Empty;
        private string _profileDescription = string.Empty;

        /// <summary>
        /// ダイアログのタイトル
        /// </summary>
        public string DialogTitle
        {
            get => _dialogTitle;
            set
            {
                _dialogTitle = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// プロファイル名
        /// </summary>
        public string ProfileName
        {
            get => _profileName;
            set
            {
                _profileName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// プロファイルの説明
        /// </summary>
        public string ProfileDescription
        {
            get => _profileDescription;
            set
            {
                _profileDescription = value;
                OnPropertyChanged();
            }
        }

        public ProfileInputDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // プロファイル名の検証
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                MessageBox.Show("プロファイル名を入力してください。", "入力エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProfileNameTextBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// キャンセルボタンクリック時の処理
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
