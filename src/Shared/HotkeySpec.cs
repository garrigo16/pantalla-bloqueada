using System;
using System.Collections.Generic;

// Autor: Miquel Garrigo
internal struct HotkeySpec
{
    internal readonly int Modifiers, Key;
    // Key=0 representa Ñ, resuelta con la distribución del teclado actual.
    internal HotkeySpec(int modifiers, int key) { Modifiers = modifiers; Key = key; }
    internal string Error
    {
        get
        {
            if ((Modifiers & 4) != 0) return "Shift se usa para abrir el pinball.";
            if ((Modifiers & ~11) != 0 || Modifiers == 0) return "Elige Ctrl, Alt o Win junto con una tecla.";
            if (!(Key == 0 || (Key >= 0x30 && Key <= 0x39) || (Key >= 0x41 && Key <= 0x5a) || (Key >= 0x70 && Key <= 0x87)))
                return "Elige una tecla de la lista.";
            if (WindowsUsesShortcut) return "Windows usa " + this + ". Elige otra combinación.";
            return null;
        }
    }
    internal bool WindowsUsesShortcut
    {
        get
        {
            if (Key == 0x7b) return true; // F12 está reservada para el depurador de Windows.

            bool letter = Key >= 'A' && Key <= 'Z';
            bool number = Key >= '0' && Key <= '9';
            if ((Modifiers & 8) != 0)
            {
                // Letras y números con Win: Windows usa esta familia en el escritorio y la barra de tareas.
                return letter || number || (Modifiers == 10 && Key == 0x73);
            }

            // Atajos de edición, Explorador, menús y Lupa documentados por Microsoft.
            switch (Modifiers)
            {
                case 1: return "ADP".IndexOf((char)Key) >= 0 || Key == 0x73 || Key == 0x77;
                case 2: return "ABCDEFHILMNOPRSTUVWXYZ".IndexOf((char)Key) >= 0 ||
                    (Key >= '1' && Key <= '9') || Key == 0x73 || Key == 0x74;
                case 3: return "DFHIKLMPR".IndexOf((char)Key) >= 0;
                default: return false;
            }
        }
    }
    internal static string KeyLabel(int key)
    { return key == 0 ? "Ñ" : (key >= 0x70 && key <= 0x87 ? "F" + (key - 0x6f) : ((char)key).ToString()); }
    public override string ToString()
    {
        var parts = new List<string>();
        if ((Modifiers & 8) != 0) parts.Add("Win");
        if ((Modifiers & 2) != 0) parts.Add("Ctrl");
        if ((Modifiers & 1) != 0) parts.Add("Alt");
        if ((Modifiers & 4) != 0) parts.Add("Shift");
        parts.Add(KeyLabel(Key)); return string.Join(" + ", parts.ToArray());
    }
}
