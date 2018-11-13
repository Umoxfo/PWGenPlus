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

using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using PasswordGenerator.Settings;

using PWGenPlus.GUI.Dialogs;
using PWGenPlus.Windows.Menu.File;
using PWGenPlus.Windows.Menu.Option.Config;
using PWGenPlus.Windows.Menu.Tools;

namespace PWGenPlus.Windows
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly OpenFileDialog _openFileDialog;

        public ObservableCollection<Password> Passwords { get; set; } = new ObservableCollection<Password>();

        public MainWindow()
        {
            InitializeComponent();

            _openFileDialog = new OpenFileDialog
            {
                FilterIndex = 0,
                Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|Trigram files (*.tgm)|*.tgm"
            };
        }

        private void OpenProfileEditorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new ProfileEditorDialog
            {
                Owner = this
            }.ShowDialog();
        }//OpenProfileEditor_Click

        private void CloseCmdExecuted(object target, ExecutedRoutedEventArgs e) => this.Close();

        private void CloseCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void EncryptClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new PasswordEnterDialog
            {
                Owner = this,
                Title = "Encrypt",
                passwordLabel = { Background = new SolidColorBrush(Colors.Yellow) },
            }.ShowDialog();
        }//EncryptClipboardMenuItem_Click

        private void DecryptClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new PasswordEnterDialog
            {
                Owner = this,
                Title = "Decrypt",
                passwordLabel = { Background = new SolidColorBrush(Colors.Lime) },
            }.ShowDialog();
        }//DecryptClipboardMenuItem_Click

        private void FromFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _openFileDialog.Title = "Select high-entropy file";
            _openFileDialog.FilterIndex = 0;
            bool result = _openFileDialog.ShowDialog() ?? false;
        }//FromFileMenuItem_Click

        private void ConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new ConfigurationDialog
            {
                Owner = this
            }.ShowDialog();
        }//ConfigMenuItem_Click

        private void FileBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            _openFileDialog.Title = "Select word list file";
            _openFileDialog.FilterIndex = 1;
            bool result = _openFileDialog.ShowDialog() ?? false;
        }//FileBrowseButton_Click

        private void CreateRandomDataFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new CreateRandomDataFileDialog
            {
                Owner = this
            }.ShowDialog();
        }//CreateRandomDataFileMenuItem_Click

        private void CreateTrigramFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new CreateTrigramFileDialog
            {
                Owner = this
            }.ShowDialog();
        }//CreateTrigramFileMenuItem_Click

        private void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordSettings passwordSettings = new PasswordSettings();

            // Character
            if (charCheckBox.IsChecked ?? false)
            {
                // Set the password length
                int passwordLength = charLengthIntegerUpDown.Value;
                if (passwordLength > 0)
                {
                    passwordSettings.MaxLength = passwordLength;
                }//if

                // Set the character set
                StringBuilder sb = new StringBuilder();

                // Uppercase
                if (upperCaseCheckBox.IsChecked ?? false)
                {
                    sb.Append(DefaultEntries.CharacterSet.UppercaseLetters);
                }//if

                // Lowercase
                if (lowerCaseCheckBox.IsChecked ?? false)
                {
                    sb.Append(DefaultEntries.CharacterSet.LowercaseLetters);
                }//if

                // Numbers
                if (numbersCheckBox.IsChecked ?? false)
                {
                    sb.Append(DefaultEntries.CharacterSet.Numbers);
                }//if

                // Symbols
                if (symbolsCheckBox.IsChecked ?? false)
                {
                    sb.Append(DefaultEntries.CharacterSet.Symbols);
                }//if

                // Custom Characters
                if (customCharSetCheckBox.IsChecked ?? false)
                {
                    sb.Append(customCharacterTextBox.Text);
                }//if

                // Password Encoding
                if (passwordEncordTreeViewItem.IsExpanded)
                {
                    //passwordSettings.HexLower = hexLowerRadioButton.IsChecked ?? false;
                    //passwordSettings.HexUpper = hexUpperRadioButton.IsChecked ?? false;
                    //passwordSettings.Base64 = base64RadioButton.IsChecked ?? false;
                }//if

                //Escape Dubious Symbols
                passwordSettings.ExcludeDuplicates = eacapeDubiousCheckBox.IsChecked ?? false;

                // Password Encoding
                if (pronunceablePasswordTreeViewItem.IsExpanded)
                {
                    //passwordSettings.Phonetic = phoneticRadioButton.IsChecked ?? false;
                    //passwordSettings.Phoneticx = phoneticxRadioButton.IsChecked ?? false;
                }//if
            }//if

            // Words
            if (wordCheckBox.IsChecked ?? false)
            {
                // Set the number of words used for the password
                int wordCount = wordCountIntegerUpDown.Value;
                if (wordCount > 0)
                {
                    passwordSettings.WordCounts = wordCount;
                }//if

                // Set the word list file used for the password
                passwordSettings.WordList = wordListComoBox.SelectedValue;

                // Whether set the length range for the passphrases or not
                if (specifyLengthCheckBox.IsChecked ?? false)
                {
                    // Set the length range for the passphrases
                    passwordSettings.SpecifyLength = specifyLengthNumericBox.Value;

                    // Generate passwords as a combination of words with characters
                    passwordSettings.CombineWords = cmbineWordsCharsCeckBox.IsChecked ?? false;
                }//if
            }//if

            // Password Format
            if (passwordFormatCheckBox.IsChecked ?? false)
            {
                passwordSettings.PasswordFormat = passwordFormatComoBox.SelectedValue.ToString();
            }//if

            // Set the amount of passwords to generate
            passwordSettings.Quantity = passwordsQuantityNumericBox.Value;

            PWGenController.GeneratePasswords(passwordSettings).ForEach(p => Passwords.Add(p));

            passwordViewListBox.MaxWidth = double.PositiveInfinity;
        }//GeneratePasswordButton_Click
    }
}
