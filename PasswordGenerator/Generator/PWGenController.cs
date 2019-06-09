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
using System.Linq;

using Umoxfo.Security.Password.Settings;

namespace Umoxfo.Security.Password.Generator
{
    public sealed class PWGenController
    {
        private static readonly PasswordGenerator passGen = new PasswordGenerator();

        /// <summary>
        /// Generate a list of passwords
        /// </summary>
        /// <returns>A list of passwords</returns>
        public static List<Password> GeneratePasswords(in PasswordSettings settings)
        {
            //new Diceware().Test();

            char[] characterSet = settings.CharSet.Distinct().ToArray();
            int length = settings.Length;
            PasswordEncoding passwordEncoding = settings.Encoding;

            List<Password> passwordList = new List<Password>();
            HashSet<string> duplicateCheck = new HashSet<string>();

            //Calculate the maximum number of possible passwords
            int quantity = (int)Math.Min(settings.Quantity, Math.Pow(characterSet.Length, length));
            for (int i = 0; i < quantity; i++)
            {
                string pwd;
                do
                {
                    pwd = passGen.GetPassword(characterSet, length, passwordEncoding);
                }
                while (!duplicateCheck.Add(pwd));

                passwordList.Add(new Password(pwd));
            }//for

            passGen.Dispose();

            return passwordList;
        }//GeneratePasswords
    }
}
