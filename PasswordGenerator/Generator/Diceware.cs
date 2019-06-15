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

        private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private static readonly string[][] ReplacementTable =
        {
                        // Therd (3rd) Roll
                        // 1    2    3    4    5    6
 /* F  1 */ new string[] {"~", "!", "#", "$", "%", "ˆ"},
 /* o  2 */ new string[] {"&", "*", "(", ")", "-", "="},
 /* u  3 */ new string[] {"+", "[", "]", @"\", "{", "}"},
 /* r  4 */ new string[] {":", ";", "\"", "'", "<", ">"},
 /* t  5 */ new string[] {"?", "/", "0", "1", "2", "3"},
 /* h  6 */ new string[] {"4", "5", "6", "7", "8", "9"}
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
            string[] words = new string[numberWords];
            for (int i = 0; i < numberWords; i++)
            {
                dicewareWordList.TryGetValue(GenerateKey(), out words[i]);
            }//for

            // Extra security without adding another word
            if (extraSecurity) ToSecurePhrase(ref words);

            return string.Join(" ", words);
        }//GetPassphrase

        private static string GenerateKey()
        {
            int wordKey = 0;

            // Roll the dice 5 times
            for (int r = 1; r < DiceSides; r++)
            {
                wordKey = (wordKey << 4) | RollDice();
            }//for

            return Convert.ToString(wordKey, 16);
        }//GenerateKey

        private static void ToSecurePhrase(ref string[] passphrase)
        {
            // Choose a word in the passphrase
            int passphraseIndex = RollDice() - 1;

            // Choose a letter in the word
            int insertIndex = RollDice() - 1;

            // Pick the added character from the replacement table
            string addedLetter = ReplacementTable[RollDice() - 1][RollDice() - 1];

            // Insert one special character or digit into the passphrase
            passphrase[passphraseIndex] = passphrase[passphraseIndex].Insert(insertIndex, addedLetter);
        }//ToSecurePhrase

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
