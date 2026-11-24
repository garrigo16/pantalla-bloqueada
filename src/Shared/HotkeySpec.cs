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
            if ((Modifiers & ~15) != 0 || (Modifiers & 11) == 0) return "Elige Ctrl, Alt o Win junto con una tecla. Puedes añadir Shift.";
            if (!(Key == 0 || (Key >= 0x30 && Key <= 0x39) || (Key >= 0x41 && Key <= 0x5a) || (Key >= 0x70 && Key <= 0x87)))
                return "Elige una letra, un número o una tecla de función.";
            if (Key == 0x7b) return "F12 está reservada por Windows. Elige otra tecla.";
            if ((Modifiers & 8) != 0 && Key == 0x4c) return "Win + L bloquea Windows. Elige otra combinación para la simulación.";
            return null;
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
