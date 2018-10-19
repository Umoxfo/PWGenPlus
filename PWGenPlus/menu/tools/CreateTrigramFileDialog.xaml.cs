/* PWGenPlus
 * Copyright (c) 2018-2018 Makoto Sakaguchi <ycco34vx@gmail.com>
 *
 * This file is part of PWGenPlus.
 *
 * PWGenPlus is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * PWGenPlus is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with PWGenPlus.  If not, see <https://www.gnu.org/licenses/>.
 */

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
