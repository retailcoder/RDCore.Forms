// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using RDCore.SDK.Workspace.Pictures;

namespace RDCore.Forms.Model;

/// <summary>
/// Reads what a picture's header says about it before anything decodes it, and refuses a picture too large or too malformed to decode
/// safely, as error 481.
/// </summary>
/// <remarks>
/// The header is read by <see cref="PictureHeader.TryRead"/>, which checks every offset against the bytes and never throws; its
/// remarks list what it refuses, and its <see cref="PictureHeader.MaxBytes"/>, <see cref="PictureHeader.MaxSide"/> and
/// <see cref="PictureHeader.MaxPixels"/> are the bounds. A refused picture is error 481, "Invalid picture", as
/// <see cref="FormError.InvalidPicture"/>.
/// </remarks>
public static class FormPictures
{
    /// <summary>
    /// Reads a picture's header.
    /// </summary>
    /// <param name="bytes">The picture, as its file holds it.</param>
    /// <param name="info">What the header says: set when the picture is accepted, <see langword="default"/> otherwise.</param>
    /// <returns>
    /// <see langword="null"/> when the picture is accepted; otherwise <see cref="FormError.InvalidPicture"/>, for no bytes or a picture
    /// that <see cref="PictureHeader.TryRead"/> refuses.
    /// </returns>
    public static FormError? Inspect(byte[]? bytes, out PictureInfo info)
    {
        info = default;
        return bytes is not null && PictureHeader.TryRead(bytes, out info) ? null : FormError.InvalidPicture;
    }
}
