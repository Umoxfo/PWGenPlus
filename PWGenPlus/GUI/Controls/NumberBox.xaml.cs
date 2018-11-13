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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace PWGenPlus
{
    /// <summary>
    /// NumberBox.xaml
    /// </summary>
    public partial class NumberBox : TextBox
    {
        #region MinValue Property
        private static readonly DependencyPropertyKey MinValuePropertyKey =
            DependencyProperty.RegisterReadOnly("MinValue", typeof(int), typeof(NumberBox), new PropertyMetadata(1));

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
            DependencyProperty.RegisterReadOnly("MaxValue", typeof(int), typeof(NumberBox), new PropertyMetadata(int.MaxValue));

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
            DependencyProperty.Register("Value", typeof(int), typeof(NumberBox),
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
            if (d is NumberBox obj)
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
        public new double SelectionOpacity { get; set; }

        [Browsable(false)]
        public new bool SpellCheck { get; set; }

        [Browsable(true)]
        public new string Text { get; set; }

        [Browsable(false)]
        public new int UndoLimit { get; set; }

        public NumberBox()
        {
            InitializeComponent();
        }
    }
}
