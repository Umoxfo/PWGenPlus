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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;

using Umoxfo.Security.Password.Settings;

namespace Umoxfo.Security.Password.Generator
{
    public sealed class PWGenController
    {
        private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        /// <summary>
        /// Generate a list of passwords
        /// </summary>
        /// <returns>A list of passwords</returns>
        public static List<Password> GeneratePasswords(PasswordSettings settings)
        {
            ReadOnlyCollection<char> characterSet = Array.AsReadOnly(settings.CharSet.Distinct().ToArray());
            int length = settings.Length;
            PasswordEncoding passwordEncoding = settings.Encoding;

            List<Password> passwordList = new List<Password>();
            HashSet<string> duplicateCheck = new HashSet<string>();

            //Calculate the maximum number of possible passwords
            int quantity = (int)Math.Min(settings.Quantity, Math.Pow(characterSet.Count, length));
            for (int i = 0; i < quantity; i++)
            {
                string pwd;
                do
                {
                    pwd = GetRandomString(characterSet, length, passwordEncoding);

                }
                while (!duplicateCheck.Add(pwd));

                passwordList.Add(new Password(pwd));
            }//for

            rngCsp.Dispose();

            return passwordList;
        }//GeneratePasswords

        /// <summary>
        /// Generate a random string
        /// </summary>
        /// <param name="characterArray">The character-set that the generator can use</param>
        /// <param name="length">The length of the string that needs to be generated</param>
        /// <param name="passwordEncoding">The encoding of the raw password used when converting to be a string</param>
        /// <returns>Generated password string</returns>
        private static string GetRandomString(IReadOnlyList<char> characterArray, int length, PasswordEncoding passwordEncoding)
        {
            byte[] buf = new byte[length];
            rngCsp.GetBytes(buf);

            switch (passwordEncoding)
            {
                case PasswordEncoding.Base64: return Convert.ToBase64String(buf).Remove(length);
                case PasswordEncoding.HexLower: return BitConverter.ToString(buf, 0, length / 2).Replace("-", "").ToLower();
                case PasswordEncoding.HexUpper: return BitConverter.ToString(buf, 0, length / 2).Replace("-", "");
                default:
                    char[] result = new char[length];

                    int[] previousValueIndexs = new int[2];
                    for (int i = 0; i < length; i++)
                    {
                        int valueIndex = RollDice(characterArray.Count);

                        // Not more than 2 identical characters in a row
                        if ((previousValueIndexs[0] == previousValueIndexs[1]) && (previousValueIndexs[1] == valueIndex))
                        {
                            i--;
                            continue;
                        }//if

                        previousValueIndexs[0] = previousValueIndexs[1];
                        previousValueIndexs[1] = valueIndex;

                        result[i] = characterArray[valueIndex];
                    }//for

                    return new string(result);
            }//switch
        }//GetRandomString

        private static int RollDice(int numberSides)
        {
            if (numberSides <= 0) throw new ArgumentOutOfRangeException("numberSides");

            // The maximum value in the full sets of the dice sides that can come up in an int32.
            int maxValueOfFullSets = (int.MaxValue / numberSides) * numberSides;

            // Create a byte array to hold the random value.
            byte[] randomNumber = new byte[4];
            uint value = 0;
            do
            {
                // Fill the array with a random value.
                rngCsp.GetBytes(randomNumber);
                value = BitConverter.ToUInt32(randomNumber, 0);
            }
            while (value >= maxValueOfFullSets);

            // The possible values are zero-based.
            return (int)(value % numberSides);
        }//RollDice
    }
}
