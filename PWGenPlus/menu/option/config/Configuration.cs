/* PWGenPlus
 * Copyright (c) 2018-2018 Makoto Sakaguchi <ycco34vx@gmail.com>
 *
 * This file is part of PWGenPlus.
 *
 * PWGenPlus is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * PWGenPlus is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with PWGenPlus.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;

namespace PWGenPlus.menu.option.config
{
    internal static class Hotkey
    {
        public enum HotkeyActions
        {
            None, SinglePassw, PasswList, PasswClipboard, PasswMsgBox, OpenMPPG
        }

        public static Dictionary<HotkeyActions, string> HotkeyActionsEnumDictionary { get; } = new Dictionary<HotkeyActions, string>()
        {
            { HotkeyActions.None, "No further action" },
            { HotkeyActions.SinglePassw, "Generate single password in main window" },
            { HotkeyActions.PasswList, "Generate multiple passwords" },
            { HotkeyActions.PasswClipboard, "Generate password and copy it to the clipboard" },
            { HotkeyActions.PasswMsgBox, "Generate password and show it in a message box" },
            { HotkeyActions.OpenMPPG, "Open MP password generator" }
        };
    }

    internal static class FileEncording
    {
        public enum Encording
        {
            ANSI, UTF16, UTF16BOM, UTF8
        }

        public static Dictionary<Encording, string> EncordingEnumDictionary { get; } = new Dictionary<Encording, string>()
        {
            { Encording.ANSI, "ANSI" },
            { Encording.UTF16, "UTF-16" },
            { Encording.UTF16BOM, "UTF-16 Big Endian" },
            { Encording.UTF8, "UTF-8" }
        };
    }

    internal class Configuration
    {
    }
}
