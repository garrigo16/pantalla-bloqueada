// Autor: Miquel Garrigo
internal sealed partial class LunarScene
{
    // Start with the six original visitors, then the additional Kanto species.
    // Habitats and heights come from PokeAPI data. Animation cells match the pinned SpriteCollab revision.
    private static Actor[] GuestCatalog()
    {
        return new[]
        {
            Guest("0001", 40, 40, .7, 21, false, 4, 4, 4, 4, 4, 4), // Bulbasaur
            Guest("0004", 32, 32, .6, 22, false, 6, 8, 6, 8), // Charmander
            Guest("0007", 32, 32, .5, 18, true, 12, 8, 12, 8), // Squirtle
            Guest("0037", 32, 40, .6, 20, false, 6, 4, 4, 4, 6), // Vulpix
            Guest("0043", 24, 40, .5, 20, false, 8, 4, 8, 6, 8, 4, 8, 6), // Oddish
            Guest("0054", 24, 40, .8, 24, true, 8, 12, 8, 12), // Psyduck
            Guest("0002", 32, 32, 1.0, 0, false, 8, 10, 8, 10), // ivysaur
            Guest("0003", 32, 32, 2.0, 0, false, 8, 16, 8, 16), // venusaur
            Guest("0008", 32, 40, 1.0, 0, true, 8, 10, 8, 10), // wartortle
            Guest("0009", 32, 40, 1.6, 0, true, 8, 14, 8, 14), // blastoise
            Guest("0010", 32, 32, 0.3, 0, false, 10, 10, 10), // caterpie
            Guest("0011", 32, 48, 0.7, 0, false, 4, 2, 2, 2, 2, 4, 4, 4, 4, 4), // metapod
            Guest("0012", 32, 56, 1.1, 0, false, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8), // butterfree
            Guest("0013", 32, 40, 0.3, 0, false, 8, 10, 8), // weedle
            Guest("0014", 24, 40, 0.6, 0, false, 8, 4, 4, 4, 10), // kakuna
            Guest("0015", 32, 48, 1.0, 0, false, 4, 4, 4, 4), // beedrill
            Guest("0016", 32, 32, 0.3, 0, false, 6, 4, 4, 4, 4), // pidgey
            Guest("0017", 32, 40, 1.1, 0, false, 6, 10, 6, 10), // pidgeotto
            Guest("0018", 32, 40, 1.5, 0, false, 8, 12, 8, 12), // pidgeot
            Guest("0019", 48, 40, 0.3, 0, false, 6, 4, 4, 4, 4, 4, 4), // rattata
            Guest("0020", 40, 48, 0.7, 0, false, 4, 6, 4, 4, 4, 4), // raticate
            Guest("0023", 40, 48, 2.0, 0, false, 6, 6, 6, 6, 6, 6), // ekans
            Guest("0024", 40, 56, 3.5, 0, false, 6, 6, 8, 6, 6, 6), // arbok
            Guest("0026", 40, 48, 0.8, 0, false, 8, 10, 8, 10), // raichu
            Guest("0029", 40, 48, 0.4, 0, false, 6, 4, 4, 4, 4, 4, 4), // nidoran-f
            Guest("0030", 24, 32, 0.8, 0, false, 6, 8, 6, 8), // nidorina
            Guest("0031", 32, 40, 1.3, 0, false, 8, 10, 8, 10), // nidoqueen
            Guest("0032", 40, 48, 0.5, 0, false, 6, 6, 5, 6, 6, 4), // nidoran-m
            Guest("0033", 40, 40, 0.9, 0, false, 6, 12, 6, 12), // nidorino
            Guest("0034", 40, 48, 1.4, 0, false, 8, 14, 8, 14), // nidoking
            Guest("0038", 40, 40, 1.1, 0, false, 8, 10, 8, 10), // ninetales
            Guest("0039", 32, 40, 0.5, 0, false, 6, 4, 4, 4, 6), // jigglypuff
            Guest("0040", 32, 40, 1.0, 0, false, 8, 10, 8, 10), // wigglytuff
            Guest("0044", 32, 32, 0.8, 0, false, 6, 8, 6, 8), // gloom
            Guest("0045", 32, 32, 1.2, 0, false, 8, 10, 8, 10), // vileplume
            Guest("0046", 32, 24, 0.3, 0, false, 6, 8, 6, 8), // paras
            Guest("0047", 32, 32, 1.0, 0, false, 8, 10, 8, 10), // parasect
            Guest("0048", 32, 48, 1.0, 0, false, 8, 6, 6, 6, 8), // venonat
            Guest("0049", 40, 48, 1.5, 0, false, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8), // venomoth
            Guest("0055", 32, 40, 1.7, 0, true, 8, 12, 8, 12), // golduck
            Guest("0056", 32, 56, 0.5, 0, false, 8, 4, 4, 4, 8, 4, 4, 4), // mankey
            Guest("0057", 32, 40, 1.0, 0, false, 6, 8, 6, 8), // primeape
            Guest("0058", 32, 40, 0.7, 0, false, 6, 8, 6, 8), // growlithe
            Guest("0059", 32, 48, 1.9, 0, false, 8, 10, 8, 10), // arcanine
            Guest("0060", 32, 40, 0.6, 0, true, 8, 6, 6, 6, 6), // poliwag
            Guest("0061", 32, 40, 1.0, 0, true, 8, 10, 8, 10), // poliwhirl
            Guest("0062", 40, 40, 1.3, 0, true, 8, 10, 8, 10), // poliwrath
            Guest("0069", 24, 32, 0.7, 0, false, 8, 10, 8, 10), // bellsprout
            Guest("0070", 32, 32, 1.0, 0, false, 8, 10, 8, 10), // weepinbell
            Guest("0071", 32, 56, 1.7, 0, false, 6, 8, 6, 8), // victreebel
            Guest("0077", 32, 40, 1.0, 0, false, 8, 10, 8, 10), // ponyta
            Guest("0078", 40, 56, 1.7, 0, false, 8, 10, 8, 10), // rapidash
            Guest("0079", 24, 32, 1.2, 0, true, 8, 10, 8, 10), // slowpoke
            Guest("0080", 32, 40, 1.6, 0, true, 8, 12, 8, 12), // slowbro
            Guest("0083", 24, 32, 0.8, 0, false, 6, 12, 6, 12), // farfetchd
            Guest("0084", 32, 40, 1.4, 0, false, 8, 12, 8, 12), // doduo
            Guest("0085", 32, 48, 1.8, 0, false, 8, 12, 8, 12), // dodrio
            Guest("0096", 32, 40, 1.0, 0, false, 8, 12, 8, 12), // drowzee
            Guest("0097", 40, 40, 1.6, 0, false, 8, 10, 8, 10), // hypno
            Guest("0098", 32, 32, 0.4, 0, true, 8, 10, 8, 10), // krabby
            Guest("0099", 32, 32, 1.3, 0, true, 8, 10, 8, 10), // kingler
            Guest("0102", 32, 32, 0.4, 0, false, 6, 8, 6, 8), // exeggcute
            Guest("0103", 40, 48, 2.0, 0, false, 8, 12, 8, 12), // exeggutor
            Guest("0108", 32, 40, 1.2, 0, false, 10, 14, 10, 14), // lickitung
            Guest("0113", 24, 32, 1.1, 0, false, 8, 10, 8, 10), // chansey
            Guest("0114", 32, 48, 1.0, 0, false, 8, 6, 6, 6, 6, 6), // tangela
            Guest("0115", 40, 56, 2.2, 0, false, 16, 10, 16, 10), // kangaskhan
            Guest("0123", 40, 48, 1.5, 0, false, 8, 10, 8, 10), // scyther
            Guest("0125", 40, 56, 1.1, 0, false, 8, 10, 8, 10), // electabuzz
            Guest("0127", 32, 48, 1.5, 0, false, 8, 12, 8, 12), // pinsir
            Guest("0128", 40, 56, 1.4, 0, false, 8, 6, 6, 6, 6, 6, 6), // tauros
        };
    }
}
