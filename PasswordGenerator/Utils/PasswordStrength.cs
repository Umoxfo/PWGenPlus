#region copyright
// PasswordGenerator
// Copyright (c) 2018-2018 Makoto Sakaguchi
//
// This file is part of PasswordGenerator.
//
// PasswordGenerator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// PasswordGenerator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with PasswordGenerator.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System.Text.RegularExpressions;

namespace PasswordGenerator.Utils
{
    internal class PasswordStrength
    {
        private PasswordStrength()
        {
        }

        /// <summary>
        /// Check how a strong a password is. The higher the score, the stronger the password
        /// </summary>
        /// <param name="password">The password that needs to be evaluated</param>
        /// <returns>Returns the score of a password</returns>
        public static int CheckStrength(string password)
        {
            int score = 1;

            if (string.IsNullOrEmpty(password)) return 0;

            // Password Length
            int passwordLength = password.Length;
            if (passwordLength < 2) return 0;
            if (passwordLength < 4) return score;
            if (passwordLength >= 8) score++;
            if (passwordLength >= 10) score++;
            if (passwordLength >= 14) score++;

            if (Regex.Match(password, @"\d", RegexOptions.ECMAScript).Success) score++;
            if (Regex.Match(password, @"[a-z]", RegexOptions.ECMAScript).Success && Regex.Match(password, @"[A-Z]", RegexOptions.ECMAScript).Success) score++;
            if (Regex.Match(password, @"[:,µ,;, ,<,>,+,!,@,#,$,%,^,&,*,?,_,~,-,£,(,);\[,\],⟨,⟩]", RegexOptions.ECMAScript).Success) score++;

            return score;
        }//CheckStrength
    }
}
