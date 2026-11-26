using System;
using System.IO;
using System.Security.Cryptography;

// Autor: Miquel Garrigo
internal enum PokemonPinballTitle { Original, RubySapphire }
internal enum PokemonPinballEngine { PinballNs, JvppbPort, Mgba }

internal static class PokemonPinballRom
{
    internal static string Title(PokemonPinballTitle game)
    {
        return game == PokemonPinballTitle.Original ? "Pokémon Pinball" : "Pokémon Pinball: Rubí y Zafiro";
    }

    internal static PokemonPinballEngine Validate(string path, PokemonPinballTitle game)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new IOException("Elige el archivo de " + Title(game) + " en Configurar → Tema → Pokémon.");
        using (var input = File.OpenRead(path))
        using (var sha = SHA1.Create())
        {
            if (input.Length > 0 && input.Length <= 16 * 1024 * 1024)
                return Identify(BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant(), game);
        }
        return Identify("", game);
    }

    internal static PokemonPinballEngine Identify(string sha1, PokemonPinballTitle game)
    {
        if (game == PokemonPinballTitle.Original)
        {
            if (sha1 == "24c693b8048594e2bf61ec1dc2182e75d81e532d") return PokemonPinballEngine.Mgba;
            if (sha1 == "9402014d14969432142abfde728c6f1a10ee4dac" ||
                sha1 == "9407074cc9b5a93d731a07e2d24b9ca06ecf17d7") return PokemonPinballEngine.PinballNs;
            throw new IOException("El archivo no es compatible. Elige Pokémon Pinball Europa, USA/Australia con vibración o Generations 1.6.");
        }

        if (sha1 == "b8b7881eaa9856b5ada99078ad1053d31cb79827") return PokemonPinballEngine.Mgba;
        if (sha1 == "9fec81ce2c5df589e0371a0bf2f92a5fe8db730b") return PokemonPinballEngine.JvppbPort;
        throw new IOException("El archivo no es compatible. Elige Pokémon Pinball: Rubí y Zafiro Europa o USA.");
    }
}
