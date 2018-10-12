//PWGenPlus
// Copyright(C) 2018-2018  Makoto Sakaguchi<ycco34vx@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.If not, see<https://www.gnu.org/licenses/>.

namespace PasswordGenerator.Password
{
    public class PasswordSettings
    {
        //PasswordSettings

        #region Properties

        /* Character */
        /// <summary>
        ///     The minimum length of a password
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        ///     The maximum length of a password
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        ///     The character set that can be used to generate passwords
        /// </summary>
        public string CharSet { get; set; }

        /* Encode character set */
        /// <summary>
        ///     A boolean to indicate if the password should be converted into lowercase hexadecimal format
        /// </summary>
        public bool HexLower { get; set; }

        /// <summary>
        ///     A boolean to indicate if the password should be converted into uppercase hexadecimal format
        /// </summary>
        public bool HexUpper { get; set; }

        /// <summary>
        ///     A boolean to indicate if the password should be converted into base64 format
        /// </summary>
        public bool Base64 { get; set; }

        /// <summary>
        ///     A boolean to indicate whether to allow duplicate passwords or not
        /// </summary>
        public bool ExcludeDuplicates { get; set; }

        /* Pronounceable Password */
        /// <summary>
        ///     A boolean to indicate if the password should be pronounceable (lowercase letter)
        /// </summary>
        public bool Phonetic { get; set; }

        /// <summary>
        ///     A boolean to indicate if the password should be pronounceable (mixed-case letter)
        /// </summary>
        public bool Phoneticx { get; set; }

        /* Word */
        /// <summary>
        ///     The words of a passphrase
        /// </summary>
        public int WordCounts { get; set; }

        /// <summary>
        ///     The words of a passphrase
        /// </summary>
        public object WordList { get; set; }

        /// <summary>
        ///     The length range for the passphrases
        /// </summary>
        public int SpecifyLength { get; set; }

        /// <summary>
        ///     A boolean to indicate whether words and characters are separated by delimiters
        ///     each word is combined with one or more characters depending on the number of words and the number of characters
        /// </summary>
        public bool CombineWords { get; set; }

        /* Password Format */
        /// <summary>
        ///     The format string to format the generated password or passphrase
        /// </summary>
        public string PasswordFormat { get; set; }

        /// <summary>
        ///     The amount of passwords that need to be generated
        /// </summary>
        public int Quantity { get; set; }
        #endregion
    }
}