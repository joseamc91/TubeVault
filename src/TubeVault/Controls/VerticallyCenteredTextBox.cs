using System.Diagnostics.CodeAnalysis;

namespace TubeVault;

// Mantiene un TextBox estándar a su altura natural y lo centra dentro del borde exterior.
// Así evitamos dibujo personalizado y conservamos selección, portapapeles y accesibilidad.
internal sealed class VerticallyCenteredTextBox : UserControl
{
    public const int CompactHeight = 36;

    private readonly TextBox editor = new()
    {
        BorderStyle = BorderStyle.None,
        Margin = Padding.Empty
    };

    public VerticallyCenteredTextBox()
    {
        AutoSize = false;
        BorderStyle = BorderStyle.FixedSingle;
        Size = new Size(100, CompactHeight);
        MinimumSize = new Size(0, CompactHeight);
        MaximumSize = new Size(0, CompactHeight);
        TabStop = false;

        editor.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
        Controls.Add(editor);
    }

    [AllowNull]
    public override string Text
    {
        get => editor.Text;
        set => editor.Text = value;
    }

    public void SetPlaceholderText(string value)
    {
        editor.PlaceholderText = value;
    }

    public void SetReadOnly(bool value)
    {
        editor.ReadOnly = value;
    }

    public void Clear()
    {
        editor.Clear();
    }

    public new bool Focus()
    {
        return editor.Focus();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionEditor();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        editor.Font = Font;
        PositionEditor();
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        editor.Focus();
    }

    private void PositionEditor()
    {
        const int horizontalPadding = 6;
        editor.Location = new Point(
            horizontalPadding,
            Math.Max(0, (ClientSize.Height - editor.Height) / 2));
        editor.Width = Math.Max(0, ClientSize.Width - horizontalPadding * 2);
    }
}
