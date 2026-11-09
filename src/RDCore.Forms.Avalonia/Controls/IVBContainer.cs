// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia.Controls;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A control that holds other controls: a Frame or a PictureBox.
/// </summary>
public interface IVBContainer
{
    /// <summary>
    /// The canvas the controls it holds are placed on, in the container's own coordinates.
    /// </summary>
    /// <remarks>
    /// A Frame's canvas covers the whole Frame, its caption included. A PictureBox's starts inside its border, at its client origin.
    /// </remarks>
    Canvas ChildHost { get; }
}
