using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace TubeVault;

// PictureBox con un placeholder estable para evitar cambios de layout sin carátula.
internal sealed class ArtworkBox : PictureBox
{
    private int cornerRadius = 10;
    private Color borderColor = Color.Transparent;
    private Color placeholderColor = SystemColors.GrayText;

    public ArtworkBox()
    {
        DoubleBuffered = true;
        SizeMode = PictureBoxSizeMode.Zoom;
        TabStop = false;
    }

    [DefaultValue(10)]
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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color PlaceholderColor
    {
        get => placeholderColor;
        set
        {
            placeholderColor = value;
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

    protected override void OnPaint(PaintEventArgs pe)
    {
        base.OnPaint(pe);
        pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (Image is null)
        {
            DrawPlaceholderSurface(pe.Graphics);
            DrawMusicPlaceholder(pe.Graphics);
        }

        if (BorderColor != Color.Transparent && Width > 1 && Height > 1)
        {
            using var path = CreatePath(Rectangle.Inflate(ClientRectangle, -1, -1), ScaledRadius());
            using var pen = new Pen(BorderColor);
            pe.Graphics.DrawPath(pen, path);
        }
    }

    private void DrawPlaceholderSurface(Graphics graphics)
    {
        using var background = new LinearGradientBrush(
            ClientRectangle,
            BackColor,
            ControlPaint.Light(BackColor, 0.07F),
            45F);
        graphics.FillRectangle(background, ClientRectangle);

        var haloSize = Math.Min(Width, Height) * 0.52F;
        var halo = new RectangleF(
            (Width - haloSize) / 2F,
            (Height - haloSize) / 2F,
            haloSize,
            haloSize);
        using var haloBrush = new SolidBrush(Color.FromArgb(24, PlaceholderColor));
        graphics.FillEllipse(haloBrush, halo);
    }

    private void DrawMusicPlaceholder(Graphics graphics)
    {
        var scale = DeviceDpi / 96F;
        var iconWidth = Math.Max(24F, Math.Min(Width, Height) * 0.28F);
        var iconHeight = iconWidth * 0.9F;
        var left = (Width - iconWidth) / 2F;
        var top = (Height - iconHeight) / 2F;
        var stroke = Math.Max(2F, 2.2F * scale);

        using var pen = new Pen(PlaceholderColor, stroke)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var brush = new SolidBrush(PlaceholderColor);

        graphics.DrawLine(pen, left + iconWidth * 0.32F, top + iconHeight * 0.18F,
            left + iconWidth * 0.32F, top + iconHeight * 0.72F);
        graphics.DrawLine(pen, left + iconWidth * 0.32F, top + iconHeight * 0.18F,
            left + iconWidth * 0.78F, top + iconHeight * 0.08F);
        graphics.DrawLine(pen, left + iconWidth * 0.78F, top + iconHeight * 0.08F,
            left + iconWidth * 0.78F, top + iconHeight * 0.60F);

        graphics.FillEllipse(brush, left, top + iconHeight * 0.62F,
            iconWidth * 0.38F, iconHeight * 0.26F);
        graphics.FillEllipse(brush, left + iconWidth * 0.46F, top + iconHeight * 0.50F,
            iconWidth * 0.38F, iconHeight * 0.26F);
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
