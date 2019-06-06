using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Umoxfo.Security.Password.Generator
{
    internal class Diceware
    {
        private static readonly char[,] ReplacementTable = new char[6, 6]
        {
           // Therd (3rd) Roll
           // 1    2    3    4    5    6
 /* F  1 */ {'`', '!', '#', '$', '%', 'ˆ'},
 /* o  2 */ {'&', '*', '(', ')', '-', '='},
 /* u  3 */ {'+', '[', ']', '\\', '{', '}'},
 /* r  4 */ {':', ';', '"', '\'', '<', '>'},
 /* t  5 */ {'?', '/', '0', '1', '2', '3'},
 /* h  6 */ {'4', '5', '6', '7', '8', '9'}
        };

        private readonly ReadOnlyDictionary<string, string> dicewareWordList;

        internal Diceware() : this(@".\words\dicewarewordlist.csv")
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

        /*        public  void Test()
                {
                    foreach (KeyValuePair<string, string> item in dicewareWordList)
                    {
                        Console.WriteLine("[{0}: {1}]", item.Key, item.Value);
                    }
                }*/
    }
}
