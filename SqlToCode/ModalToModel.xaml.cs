using System.Windows;
using System.Windows.Input;

namespace SqlToCode
{
    /// <summary>
    /// ModalToModel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModalToModel : Window
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
                TextBox_ModalToModel.Text = value;
            }
        }

        #endregion

        public ModalToModel()
        {
            InitializeComponent();
        }

        #region Event Handler

        private void OnLoadModalToModel(object sender, RoutedEventArgs e)
        {
            TextBox_ModalToModel.Focus();
            TextBox_ModalToModel.SelectAll();
        }

        private void OnKeyDownModalToModel(object sender, KeyEventArgs e)
        {
            // ESC 키 입력 또는 Ctrl + Q 입력
            if (e.Key == Key.Escape ||
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Q)
            {
                this.Close();

                // 이벤트 처리를 중단하고 더 이상의 이벤트 전파를 막음
                e.Handled = true;
            }
        }

        #endregion
    }
}
