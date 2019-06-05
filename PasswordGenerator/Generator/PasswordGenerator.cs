using System;
using System.Security.Cryptography;
using Umoxfo.Security.Password.Settings;

namespace Umoxfo.Security.Password.Generator
{
    public sealed class PasswordGenerator
    {
        private readonly RNGCryptoServiceProvider rngCsp;

        public PasswordGenerator() => rngCsp = new RNGCryptoServiceProvider();

        /// <summary>
        /// Generate a random string
        /// </summary>
        /// <param name="characterArray">The character-set that the generator can use</param>
        /// <param name="length">The length of the string that needs to be generated</param>
        /// <param name="passwordEncoding">The encoding of the raw password used when converting to be a string</param>
        /// <returns>Generated password string</returns>
        public string GetPassword(in char[] characterArray, int length, in PasswordEncoding passwordEncoding)
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
                        int valueIndex = RollDice(characterArray.Length);

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
        }//GetPassword

        private int RollDice(int numberSides)
        {
            if (numberSides <= 0) throw new ArgumentOutOfRangeException("Invalid character-set");

            // The maximum value in the full sets of the dice sides that can come up in an int32.
            int maxValueOfFullSets = (int.MaxValue / numberSides) * numberSides;

            // Create a byte array to hold the random value.
            byte[] randomNumber = new byte[4];
            uint value;
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

        public void Dispose() => rngCsp.Dispose();
    }
}
