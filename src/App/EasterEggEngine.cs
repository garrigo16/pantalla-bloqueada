using System;
using System.Collections.Generic;

// Autor: Miquel Garrigo
internal enum FunKey { Other, Letter, Escape, Enter, Space, F1, Up, Down, Left, Right, A, B }
internal enum FunTarget { Background, Clock, Network }
internal enum FunKind { Message, Houdini, Stamp, Sleepy, Eyes, Shake, Evade, PostIt, Embarrassed, Konami, Magic, Networks, Pixel }

internal sealed class FunReaction
{
    internal readonly FunKind Kind;
    internal readonly string Text;
    internal readonly int Milliseconds;
    internal FunReaction(FunKind kind, string text, int milliseconds) { Kind = kind; Text = text; Milliseconds = milliseconds; }
}

internal sealed class FunStats
{
    internal int Attempts, EnterPresses, Clicks, Bursts, Magic, Konami;
    internal string Summary()
    {
        if (Attempts == 0) return "Mientras no estabas: nadie ha llamado a la puerta.";
        var badges = new List<string>();
        if (Attempts >= 10) badges.Add("Diplomático del teclado");
        if (EnterPresses >= 3) badges.Add("Especialista en Enter");
        if (Bursts > 0) badges.Add("Velocista del teclado");
        if (Magic > 0) badges.Add("Aprendiz de mago");
        if (Konami > 0) badges.Add("30 vidas extra");
        return "Mientras no estabas: " + Attempts + (Attempts == 1 ? " intento." : " intentos. Tienes admiradores.") +
            (badges.Count == 0 ? "" : "\nLogros: " + string.Join(" · ", badges.ToArray()));
    }
}

internal sealed class EasterEggEngine
{
    internal const string Desperation = "La desesperación no forma parte de los protocolos de seguridad";
    private static readonly FunKey[] KonamiKeys = { FunKey.Up, FunKey.Up, FunKey.Down, FunKey.Down, FunKey.Left, FunKey.Right, FunKey.Left, FunKey.Right, FunKey.B, FunKey.A };
    private const string MagicWord = "abracadabra";
    private static readonly char[] MagicLetters = MagicWord.ToCharArray();
    private static readonly string[] Messages = { "Esa no era.", "Frío, frío.", "El teclado no acepta sobornos.", "Este equipo está en una reunión importantísima.", "Tu intento ha llegado al departamento de intentos." };
    private readonly Func<DateTime> clock;
    private readonly Func<HumorMode> mode;
    private DateTime lastReaction, lastAttempt, motionStarted;
    private int streak, message, konamiIndex, magicIndex;
    private double motion;
    internal FunStats Stats { get; private set; }

    internal EasterEggEngine(Func<HumorMode> humor, Func<DateTime> now)
    { mode = humor; clock = now; Reset(); }

    internal void Reset()
    {
        Stats = new FunStats();
        lastReaction = lastAttempt = motionStarted = DateTime.MinValue;
        streak = message = konamiIndex = magicIndex = 0;
        motion = 0;
    }

    private FunReaction React(FunKind kind, string text, int milliseconds, bool priority)
    {
        var selected = mode();
        if (selected == HumorMode.Discreto) return null;
        bool visual = kind == FunKind.Eyes || kind == FunKind.Shake || kind == FunKind.Evade || kind == FunKind.Konami;
        bool mixed = kind == FunKind.Embarrassed || kind == FunKind.Pixel;
        if (!mixed && ((selected == HumorMode.Sarcasmo && visual) || (selected == HumorMode.Mascota && !visual))) return null;
        var now = clock();
        if (!priority && now - lastReaction < TimeSpan.FromSeconds(2.5)) return null;
        lastReaction = now;
        return new FunReaction(kind, text, milliseconds);
    }

    private FunReaction Attempt()
    {
        Stats.Attempts++;
        var now = clock();
        streak = now - lastAttempt <= TimeSpan.FromMilliseconds(650) ? streak + 1 : 1;
        lastAttempt = now;
        if (streak == 3) { Stats.Bursts++; return React(FunKind.Message, "Frío, frío.", 1600, true); }
        if (streak == 6) return React(FunKind.Message, Desperation, 2600, true);
        if (streak == 10) return React(FunKind.Embarrassed, "Ahora me da vergüenza.\n" + Desperation, 3000, true);
        return null;
    }

    internal FunReaction Key(FunKey key, bool repeat, bool commandModifier)
    {
        if (repeat || commandModifier) return null;
        var escalation = Attempt();
        if (key == FunKey.Enter) Stats.EnterPresses++;
        if (key != FunKey.Letter && key != FunKey.A && key != FunKey.B) magicIndex = 0;
        if (Advance(KonamiKeys, ref konamiIndex, key))
        {
            Stats.Konami++;
            return React(FunKind.Konami, "30 vidas extra. Sigues bloqueado.", 2200, true);
        }
        if (escalation != null) return escalation;
        switch (key)
        {
            case FunKey.Escape: return React(FunKind.Houdini, "Buen intento, Houdini.", 1500, false);
            case FunKey.Enter: return React(FunKind.Stamp, "SOLICITUD RECIBIDA\nPlazo estimado: 3 cafés.", 2200, false);
            case FunKey.Space: return React(FunKind.Sleepy, "Cinco minutitos más.", 1800, false);
            case FunKey.F1: return React(FunKind.Message, "El departamento de ayuda está de vacaciones.", 2000, false);
            default: return React(FunKind.Message, Messages[message++ % Messages.Length], 1600, false);
        }
    }

    // Solo un índice sobre una palabra fija; no se acumula ni guarda texto escrito.
    internal FunReaction Text(string composition, bool commandModifier)
    {
        if (commandModifier || string.IsNullOrEmpty(composition)) return null;
        foreach (char value in composition)
            if (Advance(MagicLetters, ref magicIndex, char.ToLowerInvariant(value)))
            {
                Stats.Magic++;
                return React(FunKind.Magic, "La magia no está incluida en esta licencia.", 2400, true);
            }
        return null;
    }

    private static bool Advance<T>(T[] pattern, ref int index, T value)
    {
        var comparer = EqualityComparer<T>.Default;
        while (index > 0 && !comparer.Equals(pattern[index], value))
        {
            int fallback = index - 1;
            for (; fallback > 0; fallback--)
            {
                bool match = true;
                for (int j = 0; j < fallback; j++) if (!comparer.Equals(pattern[j], pattern[index - fallback + j])) { match = false; break; }
                if (match) break;
            }
            index = fallback;
        }
        if (comparer.Equals(pattern[index], value)) index++;
        if (index != pattern.Length) return false;
        index = 0;
        return true;
    }

    internal FunReaction Click(int count, FunTarget target)
    {
        Stats.Clicks++;
        var escalation = Attempt();
        if (count == 3) return React(FunKind.PostIt, "Vuelvo en 5 minutos.\nLos míos, no los de Windows.", 3000, true);
        if (target == FunTarget.Network) return React(FunKind.Networks,
            "Redes con mucha imaginación\nWi-Fi_Café · Conectado a la cafetera\nWifi_del_Vecino · Eso dice él\nImpresora_3ª · Sigue pensando", 3000, true);
        if (escalation != null) return escalation;
        return target == FunTarget.Clock ? React(FunKind.Evade, "Hoy no.", 1300, false) : React(FunKind.Shake, "", 600, false);
    }

    internal FunReaction Move(double pixels, bool dark)
    {
        if (pixels <= 1.5) return null;
        var now = clock();
        if (now - motionStarted > TimeSpan.FromMilliseconds(400)) { motion = 0; motionStarted = now; }
        motion += pixels;
        if (!dark || motion < 40) return null;
        motion = 0;
        return React(FunKind.Eyes, "", 1600, false);
    }

    internal FunReaction Pixel(bool backgroundVisible)
    {
        return backgroundVisible ? React(FunKind.Pixel, "Entrenamiento completado: 1 px.", 1600, false) : null;
    }

    internal static string CalendarSuffix(DateTime now, HumorMode humor)
    {
        if (humor == HumorMode.Discreto || humor == HumorMode.Mascota) return "";
        if (now.Month == 12 && now.Day == 24) return " · hoy hasta el bloqueo descansa";
        if (now.Month == 12 && now.Day == 28) return " · este candado es un inocente";
        if (now.DayOfWeek == DayOfWeek.Monday) return " · ánimo";
        if (now.DayOfWeek == DayOfWeek.Friday && now.Hour >= 14) return " · casi, casi";
        return "";
    }
}
