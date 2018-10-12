using System.Windows;

namespace PWGenPlus.menu.file
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class ProfileEditorDialog : Window
    {
        public ProfileEditorDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
