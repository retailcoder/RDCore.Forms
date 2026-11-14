// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RDCore.Forms.Model;
using RDCore.SDK.Workspace.Pictures;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// Decodes the pictures components hold, such as a <c>Picture</c>, for drawing.
/// </summary>
/// <remarks>
/// A picture is decoded only when <see cref="FormPictures.Inspect"/> accepts its header, so a decode allocates at most
/// <see cref="PictureHeader.MaxPixels"/> pixels. A picture the decoder then fails on is refused the same way. Metafiles are recognized
/// and not drawn.
/// </remarks>
public static class VBPicture
{
    /// <summary>
    /// Decodes a picture.
    /// </summary>
    /// <param name="bytes">The picture, as its file holds it, or <see langword="null"/> for none.</param>
    /// <param name="error">
    /// <see cref="FormError.InvalidPicture"/> where the picture is refused, by <see cref="FormPictures.Inspect"/> or by the decoder;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// The picture, or <see langword="null"/> where there is none, it is refused, or it is a metafile. The caller disposes it.
    /// </returns>
    public static Bitmap? Decode(byte[]? bytes, out FormError? error)
    {
        error = null;
        if (bytes is null || bytes.Length == 0)
            return null;
        if (FormPictures.Inspect(bytes, out var info) is { } refused)
        {
            error = refused;
            return null;
        }
        if (!info.HasPixelSize)
            return null;
        // A cursor is an icon with a hot spot: decoded as one.
        if (info.Format == PictureFormat.Cursor)
        {
            bytes = (byte[])bytes.Clone();
            bytes[2] = 1;
        }
        Bitmap? bitmap = null;
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            bitmap = new Bitmap(stream);
        }
        catch (Exception)
        {
            // A header that passed with a body the decoder cannot read: the picture is invalid, and the form still opens.
        }
        if (bitmap is not null && bitmap.PixelSize is { Width: > 0 and <= PictureHeader.MaxSide, Height: > 0 and <= PictureHeader.MaxSide })
            return bitmap;
        bitmap?.Dispose();
        error = FormError.InvalidPicture;
        return null;
    }

    // The picture a component holds, decoded once for each value it takes, and the error it was refused with.
    internal sealed class Holder
    {
        private byte[]? decodedFrom;

        public Bitmap? Picture { get; private set; }

        public FormError? Error { get; private set; }

        // Whether the value changes the picture: the same bytes are not decoded again.
        public bool Take(byte[]? bytes)
        {
            if (ReferenceEquals(bytes, decodedFrom))
                return false;
            decodedFrom = bytes;
            Picture?.Dispose();
            Picture = Decode(bytes, out var error);
            Error = error;
            return true;
        }
    }

    // Draws a picture at its size in pixels from the top left corner of the area, clipped to it, or stretched over it.
    internal static void Draw(DrawingContext context, Bitmap? picture, Size area, bool stretch = false)
    {
        if (picture is null || area.Width <= 0 || area.Height <= 0)
            return;
        // A pixel of the picture is a pixel of the form, whatever resolution its file states. The source is in the bitmap's own units,
        // which that resolution sets.
        var pixels = picture.PixelSize;
        var destination = stretch ? new Rect(area) : new Rect(0, 0, Math.Min(pixels.Width, area.Width), Math.Min(pixels.Height, area.Height));
        var source = stretch
            ? new Rect(picture.Size)
            : new Rect(0, 0, destination.Width * picture.Size.Width / pixels.Width, destination.Height * picture.Size.Height / pixels.Height);
        context.DrawImage(picture, source, destination);
    }
}

// A layer that draws a picture at its size in pixels from its top left corner, clipped to it: a PictureBox's, under its controls.
internal sealed class PictureLayer : Control
{
    private Bitmap? picture;

    public PictureLayer() => ClipToBounds = true;

    public Bitmap? Picture
    {
        get => picture;
        set
        {
            picture = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context) => VBPicture.Draw(context, picture, Bounds.Size);
}
