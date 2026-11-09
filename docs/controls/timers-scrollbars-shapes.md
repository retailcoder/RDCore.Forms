# Timers, scroll bars and shapes

A Timer raises an event at an interval, a scroll bar sets a value, and a Shape and a Line draw figures. The Line's drawing and the scroll bar's focus were measured with VB6.

## Timer

[`VBTimer`](../api/RDCore.Forms.Avalonia.Controls.VBTimer.yml) draws nothing and takes no focus.

- It raises Timer every `Interval` milliseconds while it is on a shown form, its own `Enabled` is True and its `Interval` is more than 0.
- A new value for `Enabled` or `Interval` begins a new interval. Stopping it drops a tick still waiting to be raised.
- A tick lands on the system timer's tick, as a VB6 Timer's does: on Windows, about every 15.6 milliseconds.
- A tick is raised on the UI thread after input and rendering. A tick that comes while the one before it waits to be raised is dropped, so an event that takes longer than the interval is raised again once it returns, never several times in a row.

The interval is counted on the system's clock. [`VBControl.SetTimeProvider`](../api/RDCore.Forms.Avalonia.Controls.VBControl.yml) sets another `TimeProvider` on a window, for the Timers on it.

## HScrollBar and VScrollBar

[`VBScrollBar`](../api/RDCore.Forms.Avalonia.Controls.VBScrollBar.yml) shows both classes, horizontal and vertical.

- The component's `Value` is the bar's. Moving the thumb sets it, to a whole number, and a change from code moves the thumb. Change is raised each time it changes, either way.
- Its arrows move it by `SmallChange`, and a click on its track by `LargeChange`.
- `Min` is at its left or top end and `Max` at its right or bottom end, so a `Min` greater than `Max` runs it the other way.
- It takes the focus, and raises GotFocus and LostFocus.

> [!NOTE]
> **Not implemented.** The Scroll event: dragging the thumb raises Change at each whole number it passes. The keyboard does not move a scroll bar.

## Shape

[`VBShape`](../api/RDCore.Forms.Avalonia.Controls.VBShape.yml) is windowless and takes no input: a click on it is a click on what is under it.

|Property|Drawn|
|---|---|
|`Shape`|A rectangle or an oval fills the control. A square or a circle is as wide as the control's shorter side, centred. A rounded figure's corners have a radius of a tenth of its shorter side.|
|`BackStyle`, `BackColor`|Opaque fills the figure with `BackColor`, under its fill.|
|`FillStyle`, `FillColor`|Solid fills the figure. A line style draws one-pixel lines eight pixels apart, from the control's top left corner.|
|`BorderStyle`, `BorderColor`, `BorderWidth`|The border is drawn inside the control, `BorderWidth` pixels wide. A one-pixel border is dashed as GDI dashes it; a wider one is solid. Inside Solid draws as Solid, and Transparent draws no border.|

## Line

[`VBLine`](../api/RDCore.Forms.Avalonia.Controls.VBLine.yml) draws a line pixel for pixel as VB6 draws it. It is windowless and takes no input.

- `X1`, `Y1`, `X2` and `Y2` are in the container's scale: the form's or a PictureBox's `ScaleMode`, and twips in a Frame. A user scale's unit is the container's inside over its `ScaleWidth` and `ScaleHeight`, in single precision, from `ScaleLeft` and `ScaleTop`.
- Each end is held as whole twips, rounded half to even, and drawn on the nearest pixel. The line follows its ends and its container's scale as they change.
- A one-pixel line covers every pixel from its first end up to its second, the second left out. Its `BorderStyle` dashes it from the first end: Dash is 18 pixels on and 6 off, and Dot 3 and 3.
- A wider line is solid whatever its `BorderStyle`, and covers both ends. Across, it covers `BorderWidth` rows centred on the end's row, the extra one above where the width is even.
- Inside Solid draws as Solid, and Transparent draws nothing. Every `DrawMode` draws as Copy Pen (13).
