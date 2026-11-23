internal static class ReleaseSource
{
    internal const string Owner = "garrigo16";
    // Dirección estable: los actualizadores anteriores también consultan este repositorio.
    internal const string Repository = "pantalla-bloqueada";
    internal const string RepositoryUrl = "https://github.com/" + Owner + "/" + Repository;
    internal const string LatestApi = "https://api.github.com/repos/" + Owner + "/" + Repository + "/releases/latest";
    internal const string InstallerName = "Iniciar.exe";
}
