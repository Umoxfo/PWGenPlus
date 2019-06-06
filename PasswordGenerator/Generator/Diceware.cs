using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Umoxfo.Security.Password.Generator
{
    internal class Diceware
    {
        private static readonly ReadOnlyDictionary<string, string> dicewareWordList;

        static Diceware()
        {
            Dictionary<string, string> tmp = new Dictionary<string, string>();

            // Read a word list file
            using (StreamReader sr = new StreamReader(@".\words\dicewarewordlist.csv"))
            {
                while (!sr.EndOfStream)
                {
                    string[] values = sr.ReadLine().Split(',');

                    Console.WriteLine($"{values[0]} { values[1]}");
                    tmp.Add(values[0], values[1]);
                }//while
            }

            dicewareWordList = new ReadOnlyDictionary<string, string>(tmp);
        }//Diceware()

        public static void Test()
        {
            Console.WriteLine("Hello World!");
        }
    }
}
