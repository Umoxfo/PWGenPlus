/* PasswordGenerator
 * Copyright (c) 2018-2018 Makoto Sakaguchi <ycco34vx@gmail.com>
 *
 * This file is part of PasswordGenerator.
 *
 * PasswordGenerator is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * PasswordGenerator is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with PasswordGenerator.  If not, see <https://www.gnu.org/licenses/>.
 */

using PasswordGenerator.Utils;

namespace PasswordGenerator
{
    /// <summary>
    /// This class represents a password
    /// </summary>
    public class Password
    {
        private string _actualPassword;

        /// <summary>
        /// The actual password in plain text
        /// </summary>
        public string ActualPassword
        {
            get => _actualPassword;
            set
            {
                _actualPassword = value;
                Length = value.Length;
                Strength = PasswordStrength.CheckStrength(value);
            }
        }

        /// <summary>
        /// The length of the password
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// The strength of a password, indicated by a number ranging from 0 to 6.
        /// The higher the score, the stronger the password
        /// </summary>
        public int Strength { get; private set; }
    }
}
