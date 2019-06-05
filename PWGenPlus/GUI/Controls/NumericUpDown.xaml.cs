﻿/* PWGenPlus
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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace PWGenPlus
{
    /// <summary>
    /// NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        #region MinValue Property
        private static readonly DependencyPropertyKey MinValuePropertyKey =
            DependencyProperty.RegisterReadOnly("MinValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(1));

        public static readonly DependencyProperty MinValueProperty = MinValuePropertyKey.DependencyProperty;

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common Properties")]
        public int MinValue
        {
            get => (int)GetValue(MinValueProperty);
            private set => SetValue(MinValueProperty, value);
        }
        #endregion

        #region MaxValue Property
        private static readonly DependencyPropertyKey MaxValuePropertyKey =
            DependencyProperty.RegisterReadOnly("MaxValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(255));

        public static readonly DependencyProperty MaxValueProperty = MaxValuePropertyKey.DependencyProperty;

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common Properties")]
        public int MaxValue
        {
            get => (int)GetValue(MaxValueProperty);
            private set => SetValue(MaxValueProperty, value);
        }
        #endregion

        #region Value Property
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(1, OnPropertyChanged, new CoerceValueCallback(CoerceValue)));

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common Properties")]
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Method intentionally left empty.
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            if (d is NumericUpDown obj)
            {
                int value = (int)baseValue;
                if (value < obj.MinValue)
                {
                    return obj.MinValue;
                }//if

                if (value > obj.MaxValue)
                {
                    return obj.MaxValue;
                }//if

                return value;
            }//if

            return baseValue;
        }//CoerceValue
        #endregion

        [Browsable(false)]
        public new object Content { get; set; }

        public NumericUpDown()
        {
            InitializeComponent();
        }

        private void NumberButtonUp_Click(object sender, RoutedEventArgs e) => Value++;

        private void NumberButtonDown_Click(object sender, RoutedEventArgs e) => Value--;
    }
}
