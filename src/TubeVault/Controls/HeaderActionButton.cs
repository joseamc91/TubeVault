using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace TubeVault;

internal enum HeaderActionIcon
{
    None,
    Settings
}

internal sealed class HeaderActionButton : RoundedButton
{
    private HeaderActionIcon icon;
    private bool hasNotification;
    private bool showChevron;

    [DefaultValue(HeaderActionIcon.None)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal HeaderActionIcon Icon
    {
        get => icon;
        set
        {
            icon = value;
            Invalidate();
        }
    }

    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool HasNotification
    {
        get => hasNotification;
        set
        {
            hasNotification = value;
            Invalidate();
        }
    }

    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool ShowChevron
    {
        get => showChevron;
        set
        {
            showChevron = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (icon == HeaderActionIcon.Settings)
        {
            DrawSettingsIcon(e.Graphics);
        }

        if (hasNotification)
        {
            DrawNotification(e.Graphics);
        }

        if (showChevron)
        {
            DrawChevron(e.Graphics);
        }
    }

    private void DrawChevron(Graphics graphics)
    {
        var x = ClientRectangle.Right - 18F;
        var y = ClientRectangle.Height / 2F;
        using var pen = new Pen(ForeColor, 1.6F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        graphics.DrawLines(pen,
        [
            new PointF(x - 2F, y - 4F),
            new PointF(x + 2F, y),
            new PointF(x - 2F, y + 4F)
        ]);
    }

    private void DrawSettingsIcon(Graphics graphics)
    {
        var center = new PointF(ClientRectangle.Width / 2F, ClientRectangle.Height / 2F);
        using var pen = new Pen(ForeColor, 1.7F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        graphics.DrawEllipse(pen, center.X - 5F, center.Y - 5F, 10F, 10F);
        graphics.DrawEllipse(pen, center.X - 1.5F, center.Y - 1.5F, 3F, 3F);

        for (var ray = 0; ray < 8; ray++)
        {
            var angle = ray * Math.PI / 4;
            var inner = new PointF(
                center.X + (float)Math.Cos(angle) * 6.5F,
                center.Y + (float)Math.Sin(angle) * 6.5F);
            var outer = new PointF(
                center.X + (float)Math.Cos(angle) * 9F,
                center.Y + (float)Math.Sin(angle) * 9F);
            graphics.DrawLine(pen, inner, outer);
        }
    }

    private void DrawNotification(Graphics graphics)
    {
        var scale = DeviceDpi / 96F;
        var diameter = 7F * scale;
        using var brush = new SolidBrush(Color.FromArgb(210, 45, 45));
        using var border = new Pen(BackColor, Math.Max(1F, 1.5F * scale));
        var x = ClientRectangle.Right - (showChevron ? 34F : 10F) * scale;
        var y = ClientRectangle.Top + 8F * scale;
        graphics.FillEllipse(brush, x - diameter / 2F, y - diameter / 2F, diameter, diameter);
        graphics.DrawEllipse(border, x - diameter / 2F, y - diameter / 2F, diameter, diameter);
    }
}
