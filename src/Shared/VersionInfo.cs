using System.Reflection;

[assembly: AssemblyTitle("Pantalla bloqueada")]
[assembly: AssemblyProduct("Pantalla bloqueada")]
[assembly: AssemblyCompany("Miquel Garrigo")]
[assembly: AssemblyCopyright("Copyright © 2026 Miquel Garrigo")]
[assembly: AssemblyVersion(ProductVersion.Assembly)]
[assembly: AssemblyFileVersion(ProductVersion.Assembly)]
[assembly: AssemblyInformationalVersion(ProductVersion.Current)]

internal static class ProductVersion
{
    internal const string Current = "1.3.0";
    internal const string Assembly = "1.3.0.0";
}
