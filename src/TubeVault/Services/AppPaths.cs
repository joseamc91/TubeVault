namespace TubeVault;

internal static class AppPaths
{
    public static string RootDirectory { get; } = FindRootDirectory();

    public static string ToolsDirectory => Path.Combine(RootDirectory, "tools");

    public static string ConfigDirectory => Path.Combine(RootDirectory, "config");

    public static string LogsDirectory => Path.Combine(RootDirectory, "logs");

    private static string FindRootDirectory()
    {
        // Durante el desarrollo, localiza la solución. En una publicación portable,
        // la carpeta del ejecutable pasa a ser la raíz de TubeVault.
        var assemblyDirectory = Path.GetDirectoryName(typeof(AppPaths).Assembly.Location);
        var startingDirectory = string.IsNullOrWhiteSpace(assemblyDirectory)
            ? AppContext.BaseDirectory
            : assemblyDirectory;
        var directory = new DirectoryInfo(startingDirectory);

        for (var level = 0; level < 6 && directory is not null; level++)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TubeVault.sln")))
            {
                var expectedBinDirectory = Path.GetFullPath(Path.Combine(
                    directory.FullName,
                    "src",
                    "TubeVault",
                    "bin")) + Path.DirectorySeparatorChar;
                var actualDirectory = Path.GetFullPath(startingDirectory)
                                      .TrimEnd(Path.DirectorySeparatorChar)
                                      + Path.DirectorySeparatorChar;

                // Solo una DLL ejecutada desde el binario real del proyecto puede
                // usar la raíz de desarrollo. Una publicación siempre usa su carpeta.
                return actualDirectory.StartsWith(
                    expectedBinDirectory,
                    StringComparison.OrdinalIgnoreCase)
                    ? directory.FullName
                    : AppContext.BaseDirectory;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
