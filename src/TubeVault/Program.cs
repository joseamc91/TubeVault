namespace TubeVault;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Configura WinForms con los valores recomendados para .NET moderno.
        ApplicationConfiguration.Initialize();

        Application.Run(new MainForm());
    }
}
