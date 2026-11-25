using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

internal sealed class FakeHotkeyPlatform : IHotkeyPlatform
{
    internal readonly Dictionary<int, HotkeyChord> Registered = new Dictionary<int, HotkeyChord>();
    internal readonly HashSet<HotkeyChord> Conflicts = new HashSet<HotkeyChord>();
    internal bool NoEnye;
    internal int Resolutions;
    public int ResolveEnye() { Resolutions++; if (NoEnye) throw new ShortcutException("Sin Ñ"); return 192; }
    public bool Register(int id, int modifiers, int key, out int error)
    {
        var chord = new HotkeyChord(modifiers, key);
        error = Conflicts.Contains(chord) ? 1409 : 0;
        if (error != 0) return false;
        if (Registered.ContainsKey(id) || Registered.ContainsValue(chord)) throw new Exception("Reserva duplicada");
        Registered.Add(id, chord); return true;
    }
    public void Unregister(int id) { if (!Registered.Remove(id)) throw new Exception("Se libera un atajo ajeno o dos veces"); }
    internal int Id(int modifiers, int key)
    { foreach (var item in Registered) if (item.Value.Equals(new HotkeyChord(modifiers, key))) return item.Key; throw new Exception("No está reservado"); }
}

internal static class HotkeyTests
{
    private static int checks;
    private static void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; Console.WriteLine("OK " + text); }
    private static void Reject(Action action, string text)
    { try { action(); } catch (ShortcutException) { Check(true, text); return; } throw new Exception("No se rechazó: " + text); }
    private static AppSettings Custom(int key, bool same)
    { return new AppSettings { ActivateModifiers = 3, ActivateKey = key, SameHotkey = same, UnlockModifiers = 3, UnlockKey = key + 1 }; }
    [STAThread]
    private static int Main()
    {
        try
        {
            var serializer = new XmlSerializer(typeof(AppSettings));
            AppSettings old;
            using (var input = new StringReader("<AppSettings><TimeoutSeconds>73</TimeoutSeconds><ShowClock>false</ShowClock></AppSettings>")) old = (AppSettings)serializer.Deserialize(input);
            Check(old.SameHotkey && old.ActivateModifiers == 8 && old.ActivateKey == 0 && old.UnlockModifiers == 8 && old.UnlockKey == 0 && old.TimeoutSeconds == 73 && !old.ShowClock,
                "Los ajustes anteriores reciben Win+Ñ y mantienen sus preferencias");
            using (var bytes = new MemoryStream())
            { var custom = Custom(0x77, false); serializer.Serialize(bytes, custom); bytes.Position = 0; var restored = (AppSettings)serializer.Deserialize(bytes);
                Check(!restored.SameHotkey && restored.ActivateKey == 0x77 && restored.UnlockKey == 0x78 && restored.ActivateModifiers == 3, "Se guardan y recuperan ambos atajos personalizados"); }
            Check(new HotkeySpec(8, 0).ToString() == "Win + Ñ" && new HotkeySpec(7, 0x77).ToString() == "Ctrl + Alt + Shift + F8", "Los nombres de las combinaciones son legibles");
            Check(new HotkeySpec(0, 0x41).Error != null && new HotkeySpec(4, 0x41).Error != null && new HotkeySpec(16, 0x41).Error != null, "Rechaza teclas de escritura sueltas y modificadores inválidos");
            Check(new HotkeySpec(8, 0x4c).Error != null && new HotkeySpec(2, 0x7b).Error != null && new HotkeySpec(2, 0xff).Error != null, "Rechaza Win+L, F12 y teclas no admitidas");
            for (int key = 'A'; key <= 'Z'; key++)
                if (new HotkeySpec(8, key).Error == null) throw new Exception("Win + " + (char)key + " debe quedar libre para Windows.");
            Check(new HotkeySpec(8, '1').Error != null && new HotkeySpec(2, 'C').Error != null &&
                new HotkeySpec(1, 0x73).Error != null && new HotkeySpec(3, 'F').Error != null &&
                new HotkeySpec(3, 'B').Error == null, "Descarta atajos de Windows según los modificadores y conserva combinaciones libres");
            var previous = new AppSettings { SameHotkey = false, ActivateModifiers = 7, ActivateKey = 0x77,
                UnlockModifiers = 8, UnlockKey = 'E', TimeoutSeconds = 73 };
            previous.NormalizeShortcuts();
            Check(previous.ActivateModifiers == 3 && previous.ActivateKey == 0x77 &&
                previous.UnlockModifiers == 8 && previous.UnlockKey == 0 && !previous.SameHotkey && previous.TimeoutSeconds == 73,
                "Al cargar ajustes antiguos quita Shift y sustituye solo el atajo reservado");
            var editor = new ShortcutEditor("Prueba", new HotkeySpec(3, 'B'));
            Check(editor.Spec.Key == 'B' && editor.Spec.Error == null, "El selector acepta Ctrl+Alt+B");
            editor.Win.IsChecked = true;
            Check(editor.Spec.Key == -1 && editor.Spec.Error != null, "El selector pide otra tecla cuando la selección entra en conflicto");
            foreach (System.Windows.Controls.ComboBoxItem item in editor.Key.Items)
                if ((int)item.Tag >= 'A' && (int)item.Tag <= 'Z') throw new Exception("El selector aún ofrece letras reservadas con Win.");
            editor.Key.SelectedValue = 0;
            Check(editor.Spec.Key == 0 && editor.Spec.Error == null, "El selector permite Ñ con Win y actualiza la combinación");

            var platform = new FakeHotkeyPlatform();
            using (var manager = new HotkeyRegistration(platform))
            {
                using (var change = manager.Prepare(old)) change.Commit();
                int original = platform.Id(8, 192);
                Check(manager.ActionFor(platform.Id(12, 192)) == ShortcutAction.Game, "El juego añade Shift al atajo de salida inicial");
                Check(manager.IsRegistered && platform.Registered.Count == 2 && manager.ActionFor(original) == ShortcutAction.Toggle, "Un atajo compartido alterna y reserva también el juego la simulación");
                using (var change = manager.Prepare(old)) change.Commit();
                Check(platform.Registered.Count == 2 && platform.Id(8, 192) == original, "Guardar el mismo atajo reutiliza su reserva");
                Check(platform.Resolutions == 2, "Cada preparación resuelve Ñ una sola vez");
                var distinct = Custom(0x77, false);
                using (var change = manager.Prepare(distinct))
                {
                    Check(manager.ActionFor(original) == ShortcutAction.Toggle && platform.Registered.Count == 5, "Durante el guardado los atajos anteriores siguen funcionando");
                    Check(manager.ActionFor(platform.Id(3, 0x77)) == ShortcutAction.None, "No ejecuta combinaciones nuevas antes de confirmar el guardado");
                    change.Commit();
                }
                int activate = platform.Id(3, 0x77), unlock = platform.Id(3, 0x78);
                Check(manager.ActionFor(platform.Id(7, 0x78)) == ShortcutAction.Game, "Con atajos separados el juego deriva de la salida, no de la activación");
                Check(manager.ActionFor(activate) == ShortcutAction.Activate && manager.ActionFor(unlock) == ShortcutAction.Unlock && manager.ActionFor(original) == ShortcutAction.None,
                    "Dos combinaciones distintas activan y salen por separado");
                Check(platform.Registered.Count == 3, "Al confirmar se libera la combinación anterior");
                using (var abandoned = manager.Prepare(Custom(0x79, true))) { }
                Check(platform.Registered.Count == 3 && manager.ActionFor(unlock) == ShortcutAction.Unlock, "Cancelar o fallar el guardado conserva los atajos anteriores");
                platform.Conflicts.Add(new HotkeyChord(3, 0x7a));
                Reject(delegate { manager.Prepare(Custom(0x79, false)); }, "Un conflicto en el segundo atajo rechaza el cambio completo");
                Check(platform.Registered.Count == 3 && manager.ActionFor(activate) == ShortcutAction.Activate, "El conflicto retira la reserva provisional y conserva las anteriores");
                platform.Conflicts.Clear();
                platform.Conflicts.Add(new HotkeyChord(7, 0x79));
                Reject(delegate { manager.Prepare(Custom(0x79, true)); }, "Un conflicto en el atajo del juego también rechaza el cambio completo");
                Check(platform.Registered.Count == 3 && manager.ActionFor(unlock) == ShortcutAction.Unlock, "El conflicto del juego conserva el desbloqueo anterior");
                platform.Conflicts.Clear();
                var shiftedExit = Custom(0x79, true); shiftedExit.ActivateModifiers = 7;
                Reject(delegate { manager.Prepare(shiftedExit); }, "Shift queda libre en el atajo compartido para abrir el juego");
                var collision = Custom(0x77, false); collision.ActivateModifiers = 7; collision.ActivateKey = collision.UnlockKey;
                Reject(delegate { manager.Prepare(collision); }, "También rechaza Shift en una activación independiente");
                var reserved = new AppSettings { SameHotkey = false, ActivateModifiers = 8, ActivateKey = 'E', UnlockModifiers = 3, UnlockKey = 0x78 };
                Reject(delegate { manager.Prepare(reserved); }, "Una combinación reservada no llega al registro de Windows");
                Check(platform.Registered.Count == 3 && manager.ActionFor(unlock) == ShortcutAction.Unlock, "El rechazo conserva la activación y la salida en uso");
                var swap = Custom(0x78, false); swap.UnlockKey = 0x77;
                using (var change = manager.Prepare(swap)) change.Commit();
                Check(manager.ActionFor(activate) == ShortcutAction.Unlock && manager.ActionFor(unlock) == ShortcutAction.Activate, "Intercambiar los atajos no pierde sus reservas");
                var identical = Custom(0x78, false); identical.UnlockKey = identical.ActivateKey;
                using (var change = manager.Prepare(identical)) change.Commit();
                Check(platform.Registered.Count == 2 && manager.ActionFor(unlock) == ShortcutAction.Toggle, "Dos campos con la misma combinación alternan sin reservas duplicadas");
                var pending = manager.Prepare(Custom(0x79, true)); manager.Dispose(); pending.Dispose();
                Check(platform.Registered.Count == 0 && !manager.IsRegistered && manager.ActionFor(unlock) == ShortcutAction.None, "Cerrar cancela los cambios pendientes y libera todos los atajos");
            }
            var international = new FakeHotkeyPlatform { NoEnye = true };
            using (var manager = new HotkeyRegistration(international))
            {
                Reject(delegate { manager.Prepare(new AppSettings()); }, "Sin Ñ se puede detectar que es necesario configurar otra tecla");
                Check(!manager.IsRegistered && international.Registered.Count == 0, "Un arranque sin atajo disponible no deja una reserva incompleta");
                using (var change = manager.Prepare(Custom(0x77, true))) change.Commit();
                Check(manager.IsRegistered, "Los atajos personalizados funcionan en teclados sin Ñ");
            }
            Console.WriteLine("Superadas " + checks + " comprobaciones de atajos."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
