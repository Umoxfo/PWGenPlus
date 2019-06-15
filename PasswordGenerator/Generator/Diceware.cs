using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Umoxfo.Security.Password.Generator
{
    internal class Diceware
    {
        private const byte DiceSides = 6;
        // The maximum valid value of dice roll by 6-sided dice
        private const int MaxFairValue = 251;
        private const int WordLength = 7;

        private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private static readonly string[,] ReplacementTable = new string[6, 6]
        {
           // Therd (3rd) Roll
           // 1    2    3    4    5    6
 /* F  1 */ {"~", "!", "#", "$", "%", "ˆ"},
 /* o  2 */ {"&", "*", "(", ")", "-", "="},
 /* u  3 */ {"+", "[", "]", @"\", "{", "}"},
 /* r  4 */ {":", ";", "\"", "'", "<", ">"},
 /* t  5 */ {"?", "/", "0", "1", "2", "3"},
 /* h  6 */ {"4", "5", "6", "7", "8", "9"}
        };

        private readonly ReadOnlyDictionary<string, string> dicewareWordList;

        internal Diceware() : this(@".\words\diceware_wordlist.csv")
        {
        }//Diceware()

        internal Diceware(string wordListFilePath)
        {
            Dictionary<string, string> tmp = new Dictionary<string, string>();

            // Read a word list file
            using (StreamReader sr = new StreamReader(wordListFilePath))
            {
                while (!sr.EndOfStream)
                {
                    string[] values = sr.ReadLine().Split(',');

                    tmp.Add(values[0], values[1]);
                }//while
            }

            dicewareWordList = new ReadOnlyDictionary<string, string>(tmp);
        }//Diceware(string wordListFilePath)

        internal string GetPassphrase(int numberWords, bool extraSecurity)
        {
            string[] wordKeys = GenerateKeys(numberWords);

            StringBuilder stringBuilder = new StringBuilder(WordLength * numberWords);
            foreach (string key in wordKeys)
            {
                dicewareWordList.TryGetValue(key, out string phrase);

                /*
                if (extraSecurity)
                {
                    phrase = ToSecurePhrase(phrase);
                }//if
                */

                stringBuilder.Append(phrase).Append(" ");
            }//foreach

            return stringBuilder.ToString().Trim();
        }//GetPassphrase

        private static string[] GenerateKeys(int numWords)
        {
            string[] wordKeys = new string[numWords];

            for (int i = 0; i < numWords; i++)
            {
                int wordKey = 0;

                // Roll the dice 5 times
                for (int r = 1; r < DiceSides; r++)
                {
                    wordKey = (wordKey << 8) | RollDice();
                }//for

                // Add new number to array of wordKeys
                wordKeys[i] = Convert.ToString(wordKey);
            }

            return wordKeys;
        }//GenerateKeys

/*
        private static string ToSecurePhrase(string word)
        {
            // Choose a letter in the word to replace
            int insertIndex = RollDice(DiceSides) - 1;

            // Pick the added character from the replacement table
            string addedLetter = ReplacementTable[RollDice(DiceSides) - 1, RollDice(DiceSides) - 1];

            return word.Insert(insertIndex, addedLetter);
        }//ToSecurePhrase
*/

        private static byte RollDice()
        {
            byte[] randomNumber = new byte[1];
            do
            {
                rngCsp.GetBytes(randomNumber);
            }
            while (randomNumber[0] > MaxFairValue);

            // Return the number of six-sided dice
            return (byte)((randomNumber[0] % DiceSides) + 1);
        }//RollDice


        /*        public  void Test()
                {
                    foreach (KeyValuePair<string, string> item in dicewareWordList)
                    {
                        Console.WriteLine("[{0}: {1}]", item.Key, item.Value);
                    }
                }*/
    }
}
