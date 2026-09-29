using System.Globalization;
using System.Resources;

namespace TubeVault;

internal sealed class TextService
{
    private static readonly ResourceManager Resources = new(
        "TubeVault.Resources.UiText",
        typeof(TextService).Assembly);

    private CultureInfo culture = CultureInfo.GetCultureInfo("es");

    public AppLanguage Language { get; private set; } = AppLanguage.Spanish;

    public void SetLanguage(AppLanguage language)
    {
        Language = language;
        culture = CultureInfo.GetCultureInfo(language == AppLanguage.English ? "en" : "es");
    }

    public string Get(string key, params object[] arguments)
    {
        var value = Resources.GetString(key, culture) ?? key;
        return arguments.Length == 0
            ? value
            : string.Format(culture, value, arguments);
    }
}
