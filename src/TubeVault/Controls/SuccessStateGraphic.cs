using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace TubeVault;

// Gráfico ligero de confirmación para el estado final, sin depender de assets externos.
internal sealed class SuccessStateGraphic : Control
{
    private Color accentColor = Color.FromArgb(32, 126, 67);
    private Color haloColor = Color.FromArgb(225, 242, 231);

    public SuccessStateGraphic()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = false;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => accentColor;
        set { accentColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HaloColor
    {
        get => haloColor;
        set { haloColor = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var scale = DeviceDpi / 96F;
        var center = new PointF(Width / 2F, Height / 2F);
        var haloDiameter = Math.Min(112F * scale, Math.Min(Width, Height) - 4F * scale);
        var circleDiameter = haloDiameter * 0.72F;

        using var haloBrush = new SolidBrush(HaloColor);
        e.Graphics.FillEllipse(
            haloBrush,
            center.X - haloDiameter / 2F,
            center.Y - haloDiameter / 2F,
            haloDiameter,
            haloDiameter);

        using var circleBrush = new SolidBrush(AccentColor);
        e.Graphics.FillEllipse(
            circleBrush,
            center.X - circleDiameter / 2F,
            center.Y - circleDiameter / 2F,
            circleDiameter,
            circleDiameter);

        using var checkPen = new Pen(Color.White, Math.Max(4F, 5F * scale))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        e.Graphics.DrawLines(checkPen,
        [
            new PointF(center.X - 18F * scale, center.Y + 1F * scale),
            new PointF(center.X - 5F * scale, center.Y + 14F * scale),
            new PointF(center.X + 21F * scale, center.Y - 15F * scale)
        ]);
    }
}
