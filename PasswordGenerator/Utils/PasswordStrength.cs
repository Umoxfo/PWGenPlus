/*
   PasswordGenerator
   Copyright (c) 2018-2019 Makoto Sakaguchi <ycco34vx@gmail.com>

   This file is part of PasswordGenerator.

   PasswordGenerator is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   PasswordGenerator is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with PasswordGenerator.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Linq;

namespace Umoxfo.Security.Password.Utils
{
    internal struct Result
    {
        public int ScoreIndex { get; }
        public double Score { get; }

        internal Result(int scoreIndex, double score)
        {
            ScoreIndex = scoreIndex;
            Score = score;
        }//Result
    }

    internal static class PasswordStrength
    {
        private const double MaxScore = 100.000d;
        private const double BasePoint = MaxScore / 6;

        protected internal struct CrackTime
        {
            internal const int OneHandredSeconds = 100;
            internal const int TenThousandSeconds = 10_000;
            internal const int OneMillionSecounds = 1_000_000;
            internal const int OneHundredMillionSecounds = 100_000_000;
            internal const long TenBillionSecounds = 10_000_000_000;
            internal const long OneTrillionSecounds = 1_000_000_000_000;
        }

        private static readonly Zxcvbn.Zxcvbn zxcvbn = new Zxcvbn.Zxcvbn();

        /// <summary>
        /// Check the strength of a password according to OWASP Authentication General Guidelines (https://github.com/OWASP/CheatSheetSeries/blob/master/cheatsheets/Authentication_Cheat_Sheet.md#implement-proper-password-strength-controls).
        /// The higher the score, the stronger the password.
        /// </summary>
        /// <param name="password">The password that needs to be evaluated</param>
        /// <returns>Returns the score of a password</returns>
        public static Result CheckStrength(string password)
        {
            double score = 0.000d;

            if (string.IsNullOrEmpty(password)) return new Result();

            #region Minimum Password Requirement
            /* Password Length */
            int passwordLength = password.Length;
            if (passwordLength < 8) score -= 32;
            //As a requirement of complexity
            else if (passwordLength < 10) score -= 2;

            /* Password Complexity */
            /*
             * The index corresponds to the following values:
             *   0: Uppercase Letters
             *   1: Lowercase Letters
             *   2: Digits
             *   3: Special Characters
             *   4: Identical Characters
             */
            int[] complexity = CharacterCounts(password);

            /*
             * Contains at least 3 out of the following 4 complexity rules
             *   at least 1 uppercase character
             *   at least 1 lowercase character
             *   at least 1 digit
             *   at least 1 special character (include space)
             */
            int containComplexRules = complexity.Take(4).Where(x => x >= 1).Count();
            if (containComplexRules < 3) score -= (3 - containComplexRules) * 2;

            // Not more than 2 identical characters
            if (complexity[4] > 2) score -= (complexity[4] * 6) + 2;
            #endregion

            int scoreIndex = CalculatePasswordScore(password);

            return new Result(scoreIndex, score + (BasePoint * scoreIndex));
        }//CheckStrength

        private static int[] CharacterCounts(string password)
        {
            int[] characterCounts = new int[5];
            int counter = 0;
            char lastCharacter = password[0];
            foreach (char ch in password.ToCharArray())
            {
                if (char.IsUpper(ch)) characterCounts[0]++;
                else if (char.IsLower(ch)) characterCounts[1]++;
                else if (char.IsDigit(ch)) characterCounts[2]++;
                else if (char.IsSymbol(ch) || char.IsPunctuation(ch) || char.IsSeparator(ch)) characterCounts[3]++;

                // Count consecutive identical characters
                if (lastCharacter == ch)
                {
                    counter++;
                }
                else
                {
                    if (counter > characterCounts[4]) characterCounts[4] = counter;
                    lastCharacter = ch;
                    counter = 1;
                }//if-else
            }//foreach

            return characterCounts;
        }//CharacterCounts

        private static int CalculatePasswordScore(string password)
        {
            Zxcvbn.Result result = zxcvbn.EvaluatePassword(password);

            //Convert the crack time to a score
            double crackTimeSeconds = result.CrackTime;
            if (crackTimeSeconds < CrackTime.OneHandredSeconds) return 0;
            else if (crackTimeSeconds < CrackTime.TenThousandSeconds) return 1;
            else if (crackTimeSeconds < CrackTime.OneMillionSecounds) return 2;
            else if (crackTimeSeconds < CrackTime.OneHundredMillionSecounds) return 3;
            else if (crackTimeSeconds < CrackTime.TenBillionSecounds) return 4;
            else if (crackTimeSeconds < CrackTime.OneTrillionSecounds) return 5;
            else return 6;
        }//CalculatePasswordScore
    }
}
