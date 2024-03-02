using System.Windows;
using System.Windows.Input;

namespace SqlToCode
{
    /// <summary>
    /// ModalToRepository.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModalToRepository : Window
    {
        #region Model

        private string dialogData;
        public string DialogData
        {
            get
            {
                return dialogData;
            }
            set
            {
                dialogData = value;
                TextBox_ModalToRepository.Text = value;
            }
        }

        #endregion

        public ModalToRepository()
        {
            InitializeComponent();
        }

        #region Event Handler

        private void OnLoadModalToRepository(object sender, RoutedEventArgs e)
        {
            TextBox_ModalToRepository.Focus();
            TextBox_ModalToRepository.SelectAll();
        }

        private void OnKeyDownModalToRepository(object sender, KeyEventArgs e)
        {
            // ESC 키 입력 또는 Ctrl + W 입력
            if (e.Key == Key.Escape ||
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.W)
            {
                this.Close();

                // 이벤트 처리를 중단하고 더 이상의 이벤트 전파를 막음
                e.Handled = true;
            }
        }

        #endregion
    }
}
