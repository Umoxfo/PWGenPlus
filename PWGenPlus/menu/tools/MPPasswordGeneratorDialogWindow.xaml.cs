using System.Windows;
using PWGenPlus.Dialog;

namespace PWGenPlus.menu.tools
{
    /// <summary>
    /// MPPasswordGeneratorDialogWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MPPasswordGeneratorDialogWindow : Window
    {
        public MPPasswordGeneratorDialogWindow()
        {
            InitializeComponent();
        }

        private void EnterPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordEnterDialog passwordEnterDialog = new PasswordEnterDialog
            {
                Title = "Master Password."
            };

        }//EnterPasswordButton_Click

        //Clear the key cache, the parameter, and the lastly generated password
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {

        }//ClearButton_Click


    }
}
