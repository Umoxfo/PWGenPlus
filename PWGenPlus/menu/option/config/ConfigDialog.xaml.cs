using System.Windows;

namespace PWGenPlus.menu.option.config
{
    /// <summary>
    /// Configlation.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigurationDialog : Window
    {
        public ConfigurationDialog()
        {
            InitializeComponent();
        }

        private void SelectFontButton_Click(object sender, RoutedEventArgs e)
        {
            FontDialog fontDialog = new FontDialog();
            fontDialog.Show();
        }//SelectFontButton_Click
    }
}
