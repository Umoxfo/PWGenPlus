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

using System.Linq;
using System.Text.RegularExpressions;

namespace Umoxfo.Security.Password.Utils
{
    internal static class PasswordStrength
    {
        private const double MAX_SCORE = 40.000d;
        private const double SECTION_POINTS = (MAX_SCORE - 5.000) / 3;

        /// <summary>
        /// Check how a strong a password is. The higher the score, the stronger the password
        /// </summary>
        /// <param name="password">The password that needs to be evaluated</param>
        /// <returns>Returns the score of a password</returns>
        public static double CheckStrength(string password)
        {
            double score = 0.000d;
            double points = SECTION_POINTS / 3;

            if (string.IsNullOrEmpty(password)) return 0;

            // Password Length
            int passwordLength = password.Length;
            if (passwordLength < 10) return 0;
            if (passwordLength < 12) return 1;
            if (passwordLength >= 16) score += points + 1;
            if (passwordLength >= 20) score += points;
            if (passwordLength >= 24) score += points;

            // Password Complexity
            if (HasDigit(password)) score += points;
            if (HasLowerAndUpperLetter(password)) score += points;
            if (HasSpecialCharacter(password)) score += points;

            // Not more than 2 identical characters
            if (Regex.IsMatch(password, @"^(?!(.)\1{2,}).*$")) score += SECTION_POINTS;

            // Unique character rate
            score += (password.Distinct().Count() / passwordLength) * 5.00d;

            return score;
        }//CheckStrength

        private static bool HasDigit(string password) => password.Any(x => char.IsDigit(x));

        private static bool HasLowerAndUpperLetter(string password) => password.Any(x => char.IsLower(x)) && password.Any(x => char.IsUpper(x));

        private static bool HasSpecialCharacter(string password) => password.Any(x => char.IsSymbol(x) || char.IsPunctuation(x) || char.IsSeparator(x));
    }
}
