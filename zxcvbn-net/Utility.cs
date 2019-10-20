using System.Globalization;

namespace Zxcvbn
{
    /// <summary>
    /// A few useful extension methods used through the Zxcvbn project
    /// </summary>
    static class Utility
    {
        internal static void SetTranslation(in Translation translation)
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

            Properties.Resources.Culture = CultureInfo.GetCultureInfo(cultureName);
        }//SetTranslation

        /// <summary>
        /// Quickly convert a string to an integer, uses TryParse so any non-integers will return zero
        /// </summary>
        /// <param name="str">String to parse into an int</param>
        /// <returns>Parsed int or zero</returns>
        public static int ToInt(this string str) => int.TryParse(str, out int r) ? r : 0;
    }
}
