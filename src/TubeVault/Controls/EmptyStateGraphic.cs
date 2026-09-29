using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace TubeVault;

// Ilustración ligera para explicar el estado inicial sin depender de assets externos.
internal sealed class EmptyStateGraphic : Control
{
    private Color tileColor = Color.White;
    private Color borderColor = Color.Gainsboro;
    private Color detailColor = Color.Gray;

    public EmptyStateGraphic()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = false;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TileColor
    {
        get => tileColor;
        set { tileColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => borderColor;
        set { borderColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color DetailColor
    {
        get => detailColor;
        set { detailColor = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var scale = DeviceDpi / 96F;
        var tileSize = Math.Min(112F * scale, Math.Min(Width, Height) * 0.62F);
        var tile = new RectangleF(
            (Width - tileSize) / 2F,
            (Height - tileSize) / 2F - 8F * scale,
            tileSize,
            tileSize);

        using var tilePath = CreateRoundedPath(tile, 24F * scale);
        using var tileBrush = new LinearGradientBrush(
            tile,
            TileColor,
            ControlPaint.Light(TileColor, 0.08F),
            45F);
        using var borderPen = new Pen(BorderColor, Math.Max(1F, scale));
        e.Graphics.FillPath(tileBrush, tilePath);
        e.Graphics.DrawPath(borderPen, tilePath);

        var buttonWidth = 48F * scale;
        var buttonHeight = 34F * scale;
        var button = new RectangleF(
            tile.X + (tile.Width - buttonWidth) / 2F,
            tile.Y + (tile.Height - buttonHeight) / 2F,
            buttonWidth,
            buttonHeight);

        using var buttonPath = CreateRoundedPath(button, 9F * scale);
        using var accentBrush = new SolidBrush(Color.FromArgb(0, 103, 192));
        e.Graphics.FillPath(accentBrush, buttonPath);

        var triangle = new[]
        {
            new PointF(button.X + 19F * scale, button.Y + 9F * scale),
            new PointF(button.X + 19F * scale, button.Bottom - 9F * scale),
            new PointF(button.Right - 14F * scale, button.Y + button.Height / 2F)
        };
        using var whiteBrush = new SolidBrush(Color.White);
        e.Graphics.FillPolygon(whiteBrush, triangle);

        using var detailPen = new Pen(DetailColor, Math.Max(1.5F, 1.7F * scale))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        DrawAccentLines(e.Graphics, tile, detailPen, scale);
    }

    private static void DrawAccentLines(Graphics graphics, RectangleF tile, Pen pen, float scale)
    {
        var middleY = tile.Y + tile.Height * 0.43F;
        graphics.DrawLine(pen, tile.X - 18F * scale, middleY, tile.X - 8F * scale, middleY + 4F * scale);
        graphics.DrawLine(pen, tile.X - 14F * scale, middleY - 20F * scale, tile.X - 5F * scale, middleY - 14F * scale);
        graphics.DrawLine(pen, tile.Right + 8F * scale, middleY + 4F * scale, tile.Right + 18F * scale, middleY);
        graphics.DrawLine(pen, tile.Right + 5F * scale, middleY - 14F * scale, tile.Right + 14F * scale, middleY - 20F * scale);

        var arrowY = tile.Bottom + 22F * scale;
        graphics.DrawArc(pen, tile.Right - 4F * scale, arrowY - 12F * scale, 35F * scale, 23F * scale, 20, 125);
        graphics.DrawLine(pen, tile.Right + 23F * scale, arrowY - 10F * scale, tile.Right + 30F * scale, arrowY - 12F * scale);
        graphics.DrawLine(pen, tile.Right + 23F * scale, arrowY - 10F * scale, tile.Right + 25F * scale, arrowY - 3F * scale);
    }

    private static GraphicsPath CreateRoundedPath(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2F, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
