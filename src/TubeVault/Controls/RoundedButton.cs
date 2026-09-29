using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace TubeVault;

[Flags]
internal enum RoundedCorners
{
    None = 0,
    TopLeft = 1,
    TopRight = 2,
    BottomRight = 4,
    BottomLeft = 8,
    All = TopLeft | TopRight | BottomRight | BottomLeft
}

internal class RoundedButton : Button
{
    private int cornerRadius = 9;
    private RoundedCorners roundedCorners = RoundedCorners.All;
    private bool drawRoundedBorder;

    [DefaultValue(9)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Math.Max(0, value);
            UpdateRoundedRegion();
        }
    }

    [DefaultValue(RoundedCorners.All)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RoundedCorners RoundedCorners
    {
        get => roundedCorners;
        set
        {
            roundedCorners = value;
            UpdateRoundedRegion();
        }
    }

    // El borde nativo de Button es rectangular y deja segmentos al recortarlo
    // con una región redondeada. Esta opción permite dibujar un borde limpio
    // solo en los botones que lo necesitan.
    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DrawRoundedBorder
    {
        get => drawRoundedBorder;
        set
        {
            drawRoundedBorder = value;
            Invalidate();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateRoundedRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRoundedRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (!drawRoundedBorder || Width < 3 || Height < 3)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scaledRadius = (int)Math.Round(cornerRadius * DeviceDpi / 96F);
        var bounds = Rectangle.Inflate(ClientRectangle, -1, -1);

        using var path = CreatePath(bounds, scaledRadius, roundedCorners);
        using var pen = new Pen(FlatAppearance.BorderColor);
        e.Graphics.DrawPath(pen, path);
    }

    private void UpdateRoundedRegion()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0)
        {
            return;
        }

        var scaledRadius = (int)Math.Round(cornerRadius * DeviceDpi / 96F);
        scaledRadius = Math.Min(scaledRadius, Math.Min(Width, Height) / 2);

        using var path = CreatePath(ClientRectangle, scaledRadius, roundedCorners);
        var previousRegion = Region;
        Region = new Region(path);
        previousRegion?.Dispose();
    }

    private static GraphicsPath CreatePath(
        Rectangle bounds,
        int radius,
        RoundedCorners corners)
    {
        var path = new GraphicsPath();

        if (radius <= 0 || corners == RoundedCorners.None)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var diameter = radius * 2;
        var left = bounds.Left;
        var top = bounds.Top;
        var right = bounds.Right;
        var bottom = bounds.Bottom;

        path.StartFigure();
        path.AddLine(
            left + CornerOffset(corners, RoundedCorners.TopLeft, radius),
            top,
            right - CornerOffset(corners, RoundedCorners.TopRight, radius),
            top);

        AddCorner(path, corners, RoundedCorners.TopRight,
            new Rectangle(right - diameter, top, diameter, diameter), 270, right, top);
        path.AddLine(
            right,
            top + CornerOffset(corners, RoundedCorners.TopRight, radius),
            right,
            bottom - CornerOffset(corners, RoundedCorners.BottomRight, radius));

        AddCorner(path, corners, RoundedCorners.BottomRight,
            new Rectangle(right - diameter, bottom - diameter, diameter, diameter), 0, right, bottom);
        path.AddLine(
            right - CornerOffset(corners, RoundedCorners.BottomRight, radius),
            bottom,
            left + CornerOffset(corners, RoundedCorners.BottomLeft, radius),
            bottom);

        AddCorner(path, corners, RoundedCorners.BottomLeft,
            new Rectangle(left, bottom - diameter, diameter, diameter), 90, left, bottom);
        path.AddLine(
            left,
            bottom - CornerOffset(corners, RoundedCorners.BottomLeft, radius),
            left,
            top + CornerOffset(corners, RoundedCorners.TopLeft, radius));

        AddCorner(path, corners, RoundedCorners.TopLeft,
            new Rectangle(left, top, diameter, diameter), 180, left, top);
        path.CloseFigure();
        return path;
    }

    private static int CornerOffset(RoundedCorners corners, RoundedCorners corner, int radius)
    {
        return corners.HasFlag(corner) ? radius : 0;
    }

    private static void AddCorner(
        GraphicsPath path,
        RoundedCorners corners,
        RoundedCorners corner,
        Rectangle arcBounds,
        float startAngle,
        int squareX,
        int squareY)
    {
        if (corners.HasFlag(corner))
        {
            path.AddArc(arcBounds, startAngle, 90);
        }
        else
        {
            path.AddLine(squareX, squareY, squareX, squareY);
        }
    }
}
