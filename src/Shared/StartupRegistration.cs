using System;
using System.IO;
using Microsoft.Win32;

// Autor: Miquel Garrigo
internal static class StartupRegistration
{
    internal const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    internal static bool IsAppCommand(string command)
    {
        const string suffix = "\" --background";
        if (command == null || command.Length <= suffix.Length + 1 || !command.StartsWith("\"", StringComparison.Ordinal) ||
            !command.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            string path = command.Substring(1, command.Length - suffix.Length - 1);
            string name = Path.GetFileName(path);
            return Path.IsPathRooted(path) &&
                (string.Equals(name, AppIdentity.ExecutableName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(name, AppIdentity.LegacyExecutableName, StringComparison.OrdinalIgnoreCase));
        }
        catch (ArgumentException) { return false; }
    }

    internal static string Read(RegistryKey key)
    {
        if (key == null) return null;
        string current = key.GetValue(AppIdentity.StartupName) as string;
        if (current != null) return current;
        string legacy = key.GetValue(AppIdentity.LegacyStartupName) as string;
        return IsAppCommand(legacy) ? legacy : null;
    }

    internal static void Write(RegistryKey key, string command)
    {
        if (command != null)
        {
            if (!IsAppCommand(command)) throw new IOException("La entrada de inicio no pertenece a " + AppIdentity.DisplayName + ".");
            // También restaura el nombre antiguo si el instalador revierte una migración.
            bool legacyTarget = command.EndsWith(AppIdentity.LegacyExecutableName + "\" --background", StringComparison.OrdinalIgnoreCase);
            string target = legacyTarget ? AppIdentity.LegacyStartupName : AppIdentity.StartupName;
            string other = legacyTarget ? AppIdentity.StartupName : AppIdentity.LegacyStartupName;
            object previous = key.GetValue(target);
            if (previous != null && !IsAppCommand(previous as string)) throw new IOException("Ya existe otra entrada con el nombre " + target + ".");
            key.SetValue(target, command, RegistryValueKind.String);
            if (IsAppCommand(key.GetValue(other) as string)) key.DeleteValue(other, false);
        }
        else
        {
            if (IsAppCommand(key.GetValue(AppIdentity.StartupName) as string)) key.DeleteValue(AppIdentity.StartupName, false);
            if (IsAppCommand(key.GetValue(AppIdentity.LegacyStartupName) as string)) key.DeleteValue(AppIdentity.LegacyStartupName, false);
        }
    }
}
