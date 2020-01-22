using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Umoxfo.Zxcvbn
{
    /// <summary>
    /// A few useful extension methods used through the Zxcvbn project
    /// </summary>
    internal static class Utility
    {
        internal static void SetTranslation(in Translation translation) => Properties.Resources.Culture = ToCultureInfo(translation);

        internal static CultureInfo ToCultureInfo(in Translation translation)
        {
            string cultureName;

            switch (translation)
            {
                case Translation.German: cultureName = "de-DE"; break;
                case Translation.France: cultureName = "fr-FR"; break;
                case Translation.English:
                default:
                    cultureName = "en-US";
                    break;
            }//switch

            return CultureInfo.GetCultureInfo(cultureName);
        }//ToCultureInfo

        /// <summary>
        /// Quickly convert a string to an integer, uses TryParse so any non-integers will return zero
        /// </summary>
        /// <param name="str">String to parse into an int</param>
        /// <returns>Parsed int or zero</returns>
        public static int ToInt(this string str) => int.TryParse(str, out int r) ? r : 0;

        /// <summary>
        /// Returns a list of the lines of text from an embedded dictionary resource.
        /// </summary>
        /// <param name="dictionaryResource">The dictionary resource as text</param>
        /// <returns>An enumerable of lines of the dictionary resource or null if the resource does not exist</returns>
        internal static IEnumerable<string> GetDictionaryResourceLines(in string dictionaryResource)
        {
            List<string> dict = new List<string>();

            using (StringReader sr = new StringReader(dictionaryResource))
            {
                string word;
                while ((word = sr.ReadLine()) != null)
                {
                    dict.Add(word);
                }
            }

            return dict;
        }//GetDictionaryResourceLines
    }
}
