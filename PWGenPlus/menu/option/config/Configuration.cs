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
