﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace PWGenPlus.menu.tools
{
    /// <summary>
    /// CreateRandomDataFileDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class CreateRandomDataFileDialog : Window
    {
        private readonly SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            FilterIndex = 0,
            Filter = "All files (*.*)|*.*"
        };
        private static string saveFileName;
        private static int fileSizeByByte;

        public CreateRandomDataFileDialog()
        {
            InitializeComponent();
        }//CreateRandomDataFileDialog

        // Save dialog for the random data file
        private void SaveCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            if (saveFileDialog.ShowDialog() ?? false)
            {
                fileNameTextBox.Text = saveFileDialog.FileName;
                saveFileName = saveFileDialog.SafeFileName;
            }//if
        }//SaveCmdExecuted

        private void SaveCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void CreateFileButton_Click(object sender, RoutedEventArgs e)
        {
            //fileSizeByByte = (int)fileSizeNumericBox.Value * (int)sizeUnitComboBox.SelectedValue;

            // Create the file.
            using (FileStream fs = File.Create(fileNameTextBox.Text, fileSizeByByte))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("This is some text in the file.");
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }

            string infoMessage = $"File \"{saveFileName}\" successfully created.\n\n{fileSizeByByte} bytes written.";
            MessageBox.Show(infoMessage, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }//CreateFileButton_Click

        // From Close button
        private void CloseCmdExecuted(object target, ExecutedRoutedEventArgs e) => this.Close();

        private void CloseCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
    }//CreateRandomDataFileDialog

    internal static class FileSizeUtil
    {
        public enum FileSizeUnit
        {
            Byte, KB, MB
        }//FileSizeUnit

        public static Dictionary<FileSizeUnit, int> FileSizeUnitEnumDictionary { get; } = new Dictionary<FileSizeUnit, int>()
        {
            { FileSizeUnit.Byte, 1 },
            { FileSizeUnit.KB, 1024 },   // Kilobytes: 1024 bytes
            { FileSizeUnit.MB, 1048576 } // Megabytes: 1024^2 bytes
        };
    }//FileSizeUtil
}
