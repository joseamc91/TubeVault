using System.Reflection;

namespace TubeVault;

// Lee la identidad pública desde el ensamblado generado por TubeVault.csproj.
internal static class AppMetadata
{
    private static readonly Assembly AppAssembly = typeof(AppMetadata).Assembly;

    public static string ProductName { get; } =
        AppAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
        ?? AppAssembly.GetName().Name
        ?? "TubeVault";

    public static string Version { get; } = GetVersion();

    public static string UserAgent => $"{ProductName}/{Version}";

    private static string GetVersion()
    {
        var informationalVersion = AppAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            // Un eventual sufijo de commit no forma parte de la versión visible.
            return informationalVersion.Split('+', 2)[0];
        }

        return AppAssembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
