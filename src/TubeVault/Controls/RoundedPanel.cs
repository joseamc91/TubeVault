using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace TubeVault;

// Superficie ligera para las dos tarjetas principales de MainForm.
internal sealed class RoundedPanel : Panel
{
    private int cornerRadius = 12;
    private Color borderColor = Color.Transparent;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    [DefaultValue(12)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Math.Max(0, value);
            UpdateRegion();
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;
            Invalidate();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (BorderColor == Color.Transparent || Width < 2 || Height < 2)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreatePath(Rectangle.Inflate(ClientRectangle, -1, -1), ScaledRadius());
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawPath(pen, path);
    }

    private void UpdateRegion()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = CreatePath(ClientRectangle, ScaledRadius());
        var previousRegion = Region;
        Region = new Region(path);
        previousRegion?.Dispose();
    }

    private int ScaledRadius()
    {
        return (int)Math.Round(cornerRadius * DeviceDpi / 96F);
    }

    private static GraphicsPath CreatePath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        radius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);

        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
