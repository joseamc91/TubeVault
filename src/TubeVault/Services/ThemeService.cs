namespace TubeVault;

internal static class ThemeService
{
    public static ThemeColors GetColors(AppTheme theme)
    {
        return theme == AppTheme.Dark
            ? new ThemeColors(
                Window: Color.FromArgb(20, 31, 48),
                Surface: Color.FromArgb(31, 45, 65),
                Input: Color.FromArgb(39, 54, 75),
                Text: Color.FromArgb(238, 243, 249),
                SecondaryText: Color.FromArgb(179, 192, 208),
                Border: Color.FromArgb(70, 88, 111),
                SecondaryButton: Color.FromArgb(43, 59, 81),
                SecondaryButtonHover: Color.FromArgb(54, 72, 96),
                Disabled: Color.FromArgb(62, 75, 92),
                DisabledText: Color.FromArgb(151, 163, 178),
                Error: Color.FromArgb(245, 137, 137),
                Success: Color.FromArgb(126, 211, 157))
            : new ThemeColors(
                Window: Color.FromArgb(247, 247, 247),
                Surface: Color.White,
                Input: Color.White,
                Text: Color.FromArgb(32, 32, 32),
                SecondaryText: Color.FromArgb(90, 90, 90),
                Border: Color.FromArgb(220, 220, 220),
                SecondaryButton: Color.White,
                SecondaryButtonHover: Color.FromArgb(235, 235, 235),
                Disabled: Color.FromArgb(225, 225, 225),
                DisabledText: Color.FromArgb(115, 115, 115),
                Error: Color.FromArgb(176, 34, 34),
                Success: Color.FromArgb(32, 126, 67));
    }

    public static void Apply(Control root, AppTheme theme)
    {
        var colors = GetColors(theme);
        ApplyControl(root, colors);
    }

    private static void ApplyControl(Control control, ThemeColors colors)
    {
        control.ForeColor = control.Tag as string == "secondary"
            ? colors.SecondaryText
            : colors.Text;

        control.BackColor = (control.Tag as string) switch
        {
            "surface" => colors.Surface,
            "separator" => colors.Border,
            _ when control is Label && control.Parent is not null => control.Parent.BackColor,
            _ => colors.Window
        };

        switch (control)
        {
            case EmptyStateGraphic emptyState:
                emptyState.BackColor = colors.Surface;
                emptyState.TileColor = colors.Input;
                emptyState.BorderColor = colors.Border;
                emptyState.DetailColor = colors.SecondaryText;
                break;

            case SuccessStateGraphic successState:
                successState.BackColor = colors.Surface;
                successState.AccentColor = colors.Success;
                successState.HaloColor = CreateSuccessHalo(colors);
                break;

            case ArtworkBox artwork:
                artwork.BackColor = colors.Input;
                artwork.BorderColor = colors.Border;
                artwork.PlaceholderColor = colors.SecondaryText;
                break;

            case RoundedPanel roundedPanel:
                roundedPanel.BorderColor = colors.Border;
                break;

            case VerticallyCenteredTextBox centeredTextBox:
                centeredTextBox.BackColor = colors.Input;
                centeredTextBox.ForeColor = colors.Text;
                break;

            case TextBox textBox:
                textBox.BackColor = colors.Input;
                textBox.ForeColor = colors.Text;
                break;

            case ComboBox comboBox:
                comboBox.BackColor = colors.Input;
                comboBox.ForeColor = colors.Text;
                break;

            case ListView listView:
                listView.BackColor = colors.Input;
                listView.ForeColor = colors.Text;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyControl(child, colors);
        }
    }

    private static Color CreateSuccessHalo(ThemeColors colors)
    {
        return Color.FromArgb(
            52,
            colors.Success.R,
            colors.Success.G,
            colors.Success.B);
    }
}

internal sealed record ThemeColors(
    Color Window,
    Color Surface,
    Color Input,
    Color Text,
    Color SecondaryText,
    Color Border,
    Color SecondaryButton,
    Color SecondaryButtonHover,
    Color Disabled,
    Color DisabledText,
    Color Error,
    Color Success);
