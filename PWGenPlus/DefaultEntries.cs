// PWGenPlus
// Copyright (C) 2018-2018  Makoto Sakaguchi <ycco34vx@gmail.com>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;

namespace PWGenPlus
{
    internal static class DefaultEntries
    {
        public static class CharacterSet
        {
            public const string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            public const string LowercaseLetters = "abcdefghijklmnopqrstuvwxyz";
            public const string Numbers = "0123456789";
            public const string Symbols = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~€£µ";
            public const string Brackets = "()[]{}<>";
            public const string Punctuation = ",.;:";
            public const string Ambiguous = "1Iil!|0Oo`'\",;.:\\/";
        }//CharacterSet

        public static readonly List<string> WordListFile = new List<string> {"<Default>"};

        public static readonly string[] FormatList =
        {
            "%{%4u%4l%2d%s%}",
            "%{%6A%L%d%}",
            "%3[%8q %]",
            "%*d",
            "%*10-20A",
            "%6q%2d%s%6q%2d%s",
            "%5[%w-%2d %]",
            "%U%9A",
            "%32h",
            "%5[%2h-%]%2h"
        };
    } //DefaultEntries
}