# Timers, scroll bars and shapes

A Timer raises an event at an interval, a scroll bar and a SpinButton set a value, and a Shape and a Line draw figures. The Line's drawing and the scroll bar's focus, drag and keys were measured with VB6.

## Timer

[`RDFormsTimer`](../api/RDCore.Forms.Avalonia.Controls.RDFormsTimer.yml) draws nothing and takes no focus.

- It raises Timer every `Interval` milliseconds while it is on a shown form, its own `Enabled` is True and its `Interval` is more than 0.
- A new value for `Enabled` or `Interval` begins a new interval. Stopping it drops a tick still waiting to be raised.
- A tick lands on the system timer's tick, as a VB6 Timer's does: on Windows, about every 15.6 milliseconds.
- A tick is raised on the UI thread after input and rendering. A tick that comes while the one before it waits to be raised is dropped, so an event that takes longer than the interval is raised again once it returns, never several times in a row.

The interval is counted on the system's clock. [`RDFormsControl.SetTimeProvider`](../api/RDCore.Forms.Avalonia.Controls.RDFormsControl.yml) sets another `TimeProvider` on a window, for the Timers on it.

## HScrollBar and VScrollBar

[`RDFormsScrollBar`](../api/RDCore.Forms.Avalonia.Controls.RDFormsScrollBar.yml) shows both classes, horizontal and vertical.

- The component's `Value` is the bar's. Moving the thumb sets it, to a whole number, and a change from code moves the thumb.
- Its arrows move it by `SmallChange`, and a click on its track by `LargeChange`. Each raises Change, and so does a change from code.
- Dragging the thumb raises Scroll at each whole number it reaches, and no Change. Released, it raises one Change.
- `Min` is at its left or top end and `Max` at its right or bottom end, so a `Min` greater than `Max` runs it the other way.
- It takes the focus, and raises GotFocus and LostFocus.
- With the focus, the arrow keys move it by `SmallChange` and Page Up and Page Down by `LargeChange`: Left, Up and Page Up toward `Min`, and the others toward `Max`. Home takes it to the lower of `Min` and `Max`, and End to the higher. Each raises Change where the value changes; a move stops at the end.
- It raises KeyDown, KeyPress and KeyUp, the form's first where its `KeyPreview` is True. KeyDown comes before the key moves it, and KeyUp after. A `KeyCode` of 0 left by the form's KeyDown or its own cancels the key: the bar does not move, and KeyUp is still raised.

RDForms' own, not measured with VB6: a thumb released where it started raises no Change.

## SpinButton

[`RDFormsSpinButton`](../api/RDCore.Forms.Avalonia.Controls.RDFormsSpinButton.yml) is two arrows that move a value, from MSForms. `Value`, `Min`, `Max` and `SmallChange` are Longs.

|Case|Behaviour|
|---|---|
|The up or right arrow|Raises SpinUp, then moves `Value` by `SmallChange` toward `Max`.|
|The down or left arrow|Raises SpinDown, then moves `Value` by `SmallChange` toward `Min`.|
|A move past `Min` or `Max`|Stops at the end. An arrow at the end still raises its SpinUp or SpinDown.|
|`Min` greater than `Max`|The arrows move `Value` the other way.|
|Change|Raised when `Value` changes, by an arrow or by code, and only then.|
|An arrow held down|Repeats. The second move comes five times `Delay` milliseconds after the press, and the others every `Delay` milliseconds (50 by default).|
|`Orientation`|Vertical puts the arrows one above the other, and Horizontal side by side, the down arrow on the left. Auto, the default, puts them side by side where the control is wider than it is tall.|
|The focus|It takes it, and raises GotFocus and LostFocus, KeyDown, KeyPress and KeyUp. The Up and Right keys spin it up, and the Down and Left keys down.|

Its behaviour is RDForms' own, not measured with MSForms. The arrows are drawn in its `ForeColor` on its `BackColor`.

## Shape

[`RDFormsShape`](../api/RDCore.Forms.Avalonia.Controls.RDFormsShape.yml) is windowless and takes no input: a click on it is a click on what is under it.

|Property|Drawn|
|---|---|
|`Shape`|A rectangle or an oval fills the control. A square, a rounded square or a circle is as wide as the control's shorter side, centred. A rounded figure's corners have a radius of a tenth of its shorter side.|
|`BackStyle`, `BackColor`|Opaque fills the figure with `BackColor`, under its fill.|
|`FillStyle`, `FillColor`|Solid fills the figure. A line style draws one-pixel lines eight pixels apart, from the control's top left corner.|
|`BorderStyle`, `BorderColor`, `BorderWidth`|The border is drawn inside the control, `BorderWidth` pixels wide. A one-pixel border is dashed as GDI dashes it; a wider one is solid. Inside Solid draws as Solid, and Transparent draws no border.|

## Line

[`RDFormsLine`](../api/RDCore.Forms.Avalonia.Controls.RDFormsLine.yml) draws a line pixel for pixel as VB6 draws it. It is windowless and takes no input.

- `X1`, `Y1`, `X2` and `Y2` are in the container's scale: the form's or a PictureBox's `ScaleMode`, and twips in a Frame. A user scale's unit is the container's inside over its `ScaleWidth` and `ScaleHeight`, in single precision, from `ScaleLeft` and `ScaleTop`.
- Each end is held as whole twips, rounded half to even, and drawn on the nearest pixel. The line follows its ends and its container's scale as they change.
- A one-pixel line covers every pixel from its first end up to its second, the second left out. Its `BorderStyle` dashes it from the first end: Dash is 18 pixels on and 6 off, and Dot 3 and 3.
- A wider line is solid whatever its `BorderStyle`, and covers both ends. Across, it covers `BorderWidth` rows centred on the end's row, the extra one above where the width is even. A vertical line covers as many columns, the extra one to the left.
- Inside Solid draws as Solid, and Transparent draws nothing. Every `DrawMode` draws as Copy Pen (13).

## Converting from MSForms

The names an MSForms ScrollBar and SpinButton take in RDForms (see [Converting from MSForms](index.md#converting-from-msforms)):

|MSForms|RDForms|
|---|---|
|`ScrollBar`|`RDForms.HScrollBar` or `RDForms.VScrollBar`, by its `Orientation`. A variable declared `As ScrollBar` is `As RDForms.Control` (RDC40009), and a `TypeName` compared with `"ScrollBar"` is RDC40012.|
|`SpinButton`|`RDForms.SpinButton`, with its members of the same name.|
|`Value`, `Min`, `Max`, `SmallChange`, `LargeChange` and the other members of the same name|The same.|

> [!NOTE]
> **Not implemented.** A scroll bar's and a SpinButton's BeforeDragOver, BeforeDropOrPaste and Error are declared and never raised.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---
