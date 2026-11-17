// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RDCore.Forms.Model;
using RDCore.SDK.Runtime.Stdole;
using RDCore.SDK.Workspace.Pictures;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// Decodes the pictures components hold, such as a <c>Picture</c>, for drawing.
/// </summary>
/// <remarks>
/// A picture is decoded only when <see cref="FormPictures.Inspect"/> accepts its header, so a decode allocates at most
/// <see cref="PictureHeader.MaxPixels"/> pixels. Its pixels are those the SDK decodes (<see cref="PictureDecoder"/>), alpha included, from
/// the picture <c>LoadPicture</c> makes of the same bytes (<see cref="StdPictureObject.TryLoad"/>): an icon or a cursor draws the image
/// <c>LoadPicture</c> loads when asked for no size. A picture <c>LoadPicture</c> refuses, or whose pixels the SDK does not decode, is refused
/// the same way. Metafiles are recognized and not drawn.
/// </remarks>
public static class RDFormsPicture
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
    public static Bitmap? Decode(byte[]? bytes, out FormError? error) => DecodeWithFormat(bytes, out error, out _);

    [ThreadStatic]
    private static int decodes;

    /// <summary>
    /// How many pictures this thread has decoded since it started. The controls that hold the same picture bytes share one decode, so
    /// setting one picture on any number of controls counts one.
    /// </summary>
    public static int DecodesOnThisThread => decodes;

    // Decodes a picture, and says the format its header names, where it is accepted.
    internal static Bitmap? DecodeWithFormat(byte[]? bytes, out FormError? error, out PictureFormat? format)
    {
        error = null;
        format = null;
        if (bytes is null || bytes.Length == 0)
            return null;
        decodes++;
        if (FormPictures.Inspect(bytes, out var info) is { } refused)
        {
            error = refused;
            return null;
        }
        format = info.Format;
        if (!info.HasPixelSize)
            return null;
        // The picture LoadPicture makes of the same bytes, and the pixels the SDK decodes from it: a picture LoadPicture refuses, or whose
        // pixels are not decoded, is invalid, and the form still opens. Of an icon or a cursor, the image LoadPicture loads for no size.
        if (StdPictureObject.TryLoad(bytes, 0, 0, () => 1, out var loaded) is not StdPictureLoadFailure.None
            || (loaded!.Image is { } image ? PictureDecoder.TryDecodeIconImage(bytes, image, out var decoded) : PictureDecoder.TryDecode(bytes, out decoded))
                is not PictureDecodeFailure.None
            || decoded is not { Width: > 0 and <= PictureHeader.MaxSide, Height: > 0 and <= PictureHeader.MaxSide })
        {
            error = FormError.InvalidPicture;
            return null;
        }
        return Premultiplied(decoded);
    }

    // The decoded pixels as a bitmap of one pixel per pixel of the form (96 DPI), premultiplied, as the pictures drawn and masked here are.
    private static WriteableBitmap Premultiplied(DecodedPicture picture)
    {
        var bitmap = new WriteableBitmap(new PixelSize(picture.Width, picture.Height), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using var buffer = bitmap.Lock();
        var bgra = picture.Bgra.Span;
        var row = new byte[picture.Width * 4];
        for (var y = 0; y < picture.Height; y++)
        {
            var pixels = bgra.Slice(y * row.Length, row.Length);
            for (var x = 0; x < row.Length; x += 4)
            {
                var alpha = pixels[x + 3];
                row[x] = (byte)((pixels[x] * alpha + 127) / 255);
                row[x + 1] = (byte)((pixels[x + 1] * alpha + 127) / 255);
                row[x + 2] = (byte)((pixels[x + 2] * alpha + 127) / 255);
                row[x + 3] = alpha;
            }
            Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, row.Length);
        }
        return bitmap;
    }

    // The picture a component holds and the error it was refused with. The holders of the same bytes share one decode, as VB shares
    // one picture object between the controls it is set on: the bytes are decoded once, whatever the number of controls.
    internal sealed class Holder
    {
        private byte[]? decodedFrom;
        private Shared? held;

        public Bitmap? Picture => held?.Picture;

        public FormError? Error => held?.Error;

        // The format its header names, where it is accepted.
        public PictureFormat? Format => held?.Format;

        // The colour of the picture's lower-left pixel, or null where it draws none.
        public Color? LowerLeft => held?.LowerLeft;

        // Whether the value changes the picture: the same bytes are not decoded again.
        public bool Take(byte[]? bytes)
        {
            if (ReferenceEquals(bytes, decodedFrom))
                return false;
            decodedFrom = bytes;
            held?.Release();
            held = Shared.Acquire(bytes);
            return true;
        }

        // The picture with its pixels of the mask colour left out, made once for each picture and colour; the picture as it is where
        // there is no mask. VB6 masks a picture of type bitmap (a bitmap, a GIF, a JPEG) and draws an icon or a cursor whole. A PNG,
        // which VB6 does not load, is masked as a bitmap.
        public Bitmap? MaskedBy(Color? mask) => held?.MaskedBy(mask);
    }

    // A decode the holders of the same bytes share. It is disposed when the last of them takes other bytes.
    private sealed class Shared
    {
        private static readonly ConditionalWeakTable<byte[], Shared> decoded = new();
        private readonly byte[] bytes;
        private readonly Dictionary<Color, Bitmap> masked = [];
        private Color? lowerLeft;
        private int holders;

        private Shared(byte[] bytes)
        {
            this.bytes = bytes;
            Picture = DecodeWithFormat(bytes, out var error, out var format);
            Error = error;
            Format = format;
        }

        public Bitmap? Picture { get; }

        public FormError? Error { get; }

        public PictureFormat? Format { get; }

        public Color? LowerLeft
        {
            get
            {
                if (Picture is null)
                    return null;
                lock (decoded)
                    return lowerLeft ??= PixelAt(Picture, 0, Picture.PixelSize.Height - 1);
            }
        }

        public static Shared? Acquire(byte[]? bytes)
        {
            if (bytes is null)
                return null;
            lock (decoded)
            {
                if (!decoded.TryGetValue(bytes, out var shared))
                {
                    shared = new Shared(bytes);
                    decoded.Add(bytes, shared);
                }
                shared.holders++;
                return shared;
            }
        }

        public void Release()
        {
            lock (decoded)
            {
                if (--holders > 0)
                    return;
                decoded.Remove(bytes);
                Picture?.Dispose();
                foreach (var picture in masked.Values)
                    picture.Dispose();
                masked.Clear();
            }
        }

        public Bitmap? MaskedBy(Color? mask)
        {
            if (Picture is null || mask is not { } colour || Format is PictureFormat.Icon or PictureFormat.Cursor)
                return Picture;
            lock (decoded)
            {
                if (!masked.TryGetValue(colour, out var made))
                {
                    made = Masked(Picture, colour);
                    masked.Add(colour, made);
                }
                return made;
            }
        }
    }

    // The colour of one pixel of a picture, its alpha left out.
    internal static Color PixelAt(Bitmap picture, int x, int y)
    {
        using var copy = new WriteableBitmap(picture.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using var buffer = copy.Lock();
        picture.CopyPixels(buffer);
        var value = Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
        var (first, g, third) = ((byte)value, (byte)(value >> 8), (byte)(value >> 16));
        return buffer.Format == PixelFormats.Rgba8888 ? Color.FromRgb(first, g, third) : Color.FromRgb(third, g, first);
    }

    // A copy of a picture whose opaque pixels of exactly one colour are transparent.
    internal static Bitmap Masked(Bitmap picture, Color mask)
    {
        var copy = new WriteableBitmap(picture.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using var buffer = copy.Lock();
        picture.CopyPixels(buffer);
        var (first, third) = buffer.Format == PixelFormats.Rgba8888 ? (mask.R, mask.B) : (mask.B, mask.R);
        var row = new byte[buffer.Size.Width * 4];
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            var at = buffer.Address + y * buffer.RowBytes;
            Marshal.Copy(at, row, 0, row.Length);
            for (var x = 0; x < row.Length; x += 4)
            {
                if (row[x + 3] == 255 && row[x] == first && row[x + 1] == mask.G && row[x + 2] == third)
                    row.AsSpan(x, 4).Clear();
            }
            Marshal.Copy(row, 0, at, row.Length);
        }
        return copy;
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
        // A stretched picture repeats its pixels, as VB6's StretchBlt does. The backend's default filter blends neighbors, and differs
        // by platform.
        using (context.PushRenderOptions(Unfiltered))
            context.DrawImage(picture, source, destination);
    }

    private static readonly RenderOptions Unfiltered = new() { BitmapInterpolationMode = BitmapInterpolationMode.None };
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

    public override void Render(DrawingContext context) => RDFormsPicture.Draw(context, picture, Bounds.Size);
}
