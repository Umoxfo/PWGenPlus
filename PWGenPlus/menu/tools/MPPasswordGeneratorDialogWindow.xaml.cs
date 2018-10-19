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
