using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace PWGenPlus.menu.tools
{
    /// <summary>
    /// CreateTrigramFileDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class CreateTrigramFileDialog : Window
    {
        private readonly OpenFileDialog openFileDialog;
        private readonly SaveFileDialog saveFileDialog;
        private static string saveFileName;
        private static int fileSizeByByte = 0;

        public CreateTrigramFileDialog()
        {
            InitializeComponent();

            openFileDialog = new OpenFileDialog
            {
                FilterIndex = 0,
                Filter = "All files (*.*)|*.*"
            };

            saveFileDialog = new SaveFileDialog
            {
                FilterIndex = 0,
                Filter = "All files (*.*)|*.*"
            };
        }

        // Save dialog for the random data file
        void OpenCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            if ((openFileDialog.ShowDialog() ?? false))
            {
                sourceFileTextBox.Text = saveFileDialog.FileName;
            }//if
        }//SaveCmdExecuted

        void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        // Save dialog for the random data file
        void SaveCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            if ((saveFileDialog.ShowDialog() ?? false))
            {
                destFileTextBox.Text = saveFileDialog.FileName;
                saveFileName = saveFileDialog.SafeFileName;
            }//if
        }//SaveCmdExecuted

        void SaveCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void CreateFileButton_Click(object sender, RoutedEventArgs e)
        {
            //fileSizeByByte = fileSizeNumericBox.Value * (int)sizeUnitComboBox.SelectedValue;

            //// Create the file.
            //using (FileStream fs = File.Create(fileNameTextBox.Text, fileSizeByByte))
            //{
            //    Byte[] info = new UTF8Encoding(true).GetBytes("This is some text in the file.");
            //    // Add some information to the file.
            //    fs.Write(info, 0, info.Length);
            //}

            string infoMessage = $"File \"{saveFileName}\" successfully created.\n\n{fileSizeByByte} bytes written.";
            MessageBox.Show(infoMessage, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }//CreateFileButton_Click

        // From Close button
        void CloseCmdExecuted(object target, ExecutedRoutedEventArgs e) => this.Close();

        void CloseCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
    }
}
