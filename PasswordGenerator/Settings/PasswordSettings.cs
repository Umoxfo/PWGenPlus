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
namespace Umoxfo.Security.Password.Settings
{
    public enum PasswordEncoding
    {
        Character,
        Base64,
        HexLower,
        HexUpper
    }

    public enum PronounceablePassword
    {
        Phonetic,
        Phoneticx
    }

    public class PasswordSettings
    {
        //PasswordSettings

        /* Character */
        #region Character Properties
        /// <summary>
        /// The length of a password
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// The character set that can be used to generate passwords
        /// </summary>
        public string CharSet { get; set; }

        /// <summary>
        /// The Password Encoding
        /// <list type="bullet">
        ///  Lowercase hexadecimal
        ///  Uppercase hexadecimal
        ///  Base64
        /// </list>
        /// </summary>
        public PasswordEncoding Encoding { get; set; }

        /// <summary>
        /// A boolean to indicate whether to allow duplicate passwords or not
        /// </summary>
        public bool ExcludeDuplicates { get; set; }

        /* Pronounceable Password */
        /// <summary>
        /// Pronounceable Password
        ///  Pronounceable (lowercase letter)
        ///  Pronounceable (mixed-case letter)
        /// </summary>
        public PronounceablePassword Pronunciation { get; set; }
        #endregion

        /* Word */
        #region Word Properties
        /// <summary>
        /// The number of words as a passphrase
        /// </summary>
        public int WordCounts { get; set; }

        /// <summary>
        /// The words of a passphrase
        /// </summary>
        public object WordList { get; set; }

        /// <summary>
        /// The length range for the passphrases
        /// </summary>
        public int SpecifyLength { get; set; }

        /// <summary>
        /// A boolean to indicate whether words and characters are separated by delimiters
        /// each word is combined with one or more characters depending on the number of words and the number of characters
        /// </summary>
        public bool CombineWords { get; set; }
        #endregion

        /* Password Format */
        #region Password Format Properties
        /// <summary>
        /// The format string to format the generated password or passphrase
        /// </summary>
        public string PasswordFormat { get; set; }

        /// <summary>
        /// The amount of passwords that need to be generated
        /// </summary>
        public int Quantity { get; set; }
        #endregion
    }//PasswordSettings
}