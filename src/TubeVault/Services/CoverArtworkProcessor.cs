using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TubeVault;

internal static class CoverArtworkProcessor
{
    private const int MaximumSize = 500;

    // El llamador conserva el origen y es propietario del Bitmap independiente devuelto.
    public static Bitmap CreateCover(Image source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var side = Math.Min(source.Width, source.Height);
        var crop = new Rectangle((source.Width - side) / 2, (source.Height - side) / 2, side, side);

        using var cropped = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(cropped))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImage(source, new Rectangle(0, 0, side, side), crop, GraphicsUnit.Pixel);
        }

        if (side <= MaximumSize)
        {
            return (Bitmap)cropped.Clone();
        }

        var result = new Bitmap(MaximumSize, MaximumSize, PixelFormat.Format32bppArgb);
        try
        {
            using var graphics = Graphics.FromImage(result);
            using var attributes = new ImageAttributes();
            // La interpolación solo muestrea el cuadrado recortado, sin arrastrar sus laterales originales.
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(cropped, new Rectangle(0, 0, MaximumSize, MaximumSize),
                0, 0, side, side, GraphicsUnit.Pixel, attributes);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }
}
