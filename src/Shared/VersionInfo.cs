using System.Reflection;

[assembly: AssemblyTitle(AppIdentity.DisplayName)]
[assembly: AssemblyProduct(AppIdentity.DisplayName)]
[assembly: AssemblyCompany("Miquel Garrigo")]
[assembly: AssemblyCopyright("Copyright © 2026 Miquel Garrigo")]
[assembly: AssemblyVersion(ProductVersion.Assembly)]
[assembly: AssemblyFileVersion(ProductVersion.Assembly)]
[assembly: AssemblyInformationalVersion(ProductVersion.Current)]

internal static class ProductVersion
{
    internal const string Current = "1.7.2";
    internal const string Assembly = "1.7.2.0";
}
