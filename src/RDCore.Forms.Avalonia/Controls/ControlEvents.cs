// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// Where a press's release has to land for a control to raise Click. The pressed control keeps the pointer either way, so its MouseUp
// comes wherever the release lands.
internal enum ClickRelease
{
    // Anywhere: a TextBox and a ListBox raise Click on any release of a press on them.
    Anywhere,
    // Over the control again, whether or not the pointer left it: a Label, an Image, a Frame and a PictureBox.
    OverTheControl,
}

// How a control's DblClick comes. A control with one raises it on a double click's second press, and that press's release raises no
// Click. A CommandButton and a CheckBox have none: a double click on either is two clicks.
internal enum DblClick
{
    None,
    // The second press raises DblClick and no MouseDown: a Label, an Image, a PictureBox, a TextBox, a Frame and a ListBox.
    InPlaceOfMouseDown,
    // The second press raises MouseDown, then DblClick: an OptionButton.
    AfterMouseDown,
}

// A form's window, which raises its controls' GotFocus and LostFocus as the program's active form changes, and as the focus moves
// between its controls while it is the active one.
internal interface IFormFocus
{
    // A control of the window took the focus, or lost it, in the window system: to no element, where toNone is true, as the window
    // system takes the focus from the window, or to the control `to`, where the element that took it is one.
    void FocusMoved(Control control, bool got, bool toNone, Control? to = null);
}

// A control whose user edit is kept as the focus leaves it, with a BeforeUpdate and an AfterUpdate.
internal interface IKeepsEdits
{
    // Raises BeforeUpdate, then AfterUpdate, where the user changed the control's value since it was last kept, and answers false
    // where the BeforeUpdate's handler cancelled: the focus stays on the control, and the edit stays to keep.
    bool KeepEdit();
}

// The focus, click and mouse events VB raises for a control, as measured with VB6: native and p-code builds agree.
internal static class ControlEvents
{
    // The controls whose current press is a double click's second, so that its release raises no Click.
    private static readonly ConditionalWeakTable<Control, object> secondPresses = new();
    private static readonly object marker = new();

    // The control's own focus. GotFocus and LostFocus bubble, and the focus moving between a control and a part of it, such as a
    // ListBox's item or a ComboBox's text box, is neither: VB raises one GotFocus as the control is entered and one LostFocus as it
    // is left. A control inside a container is its own, so a PictureBox loses the focus to a TextBox it holds. In a form's window, the
    // window decides what VB raises (IFormFocus): the window system's activation moves the focus too, where VB raises nothing.
    public static void AttachFocus<T>() where T : Control
    {
        InputElement.GotFocusEvent.AddClassHandler<T>((control, e) =>
        {
            if (e is FocusChangedEventArgs moved
                && RDFormsControl.Owns(control, moved.NewFocusedElement) && !RDFormsControl.Owns(control, moved.OldFocusedElement))
                FocusMoved(control, got: true);
        }, handledEventsToo: true);
        InputElement.LostFocusEvent.AddClassHandler<T>((control, e) =>
        {
            if (e is FocusChangedEventArgs moved
                && RDFormsControl.Owns(control, moved.OldFocusedElement) && !RDFormsControl.Owns(control, moved.NewFocusedElement))
                FocusMoved(control, got: false, toNone: moved.NewFocusedElement is null, to: ControlKeys.Owner(moved.NewFocusedElement));
        }, handledEventsToo: true);
    }

    // Whether an event's handler set its Cancel, the argument of the given place, to True.
    public static bool Cancelled(FormValue[]? after, int place = 0) =>
        after is { } arguments && arguments[place] is { Kind: FormValueKind.Boolean } cancel && cancel.AsBoolean();

    private static void FocusMoved(Control control, bool got, bool toNone = false, Control? to = null)
    {
        if (TopLevel.GetTopLevel(control) is IFormFocus form)
            form.FocusMoved(control, got, toNone, to);
        else
            RDFormsControl.Raise(control, got ? Events.GotFocus : Events.LostFocus);
    }

    // Click on a release. A Button raises its own Click; this is for the others. A double click's second release raises none.
    public static void AttachClick<T>(ClickRelease release) where T : Control =>
        InputElement.PointerReleasedEvent.AddClassHandler<T>((control, e) =>
        {
            if (RDFormsControl.Owns(control, e.Source) && !IsSecondPress(control)
                && (release == ClickRelease.Anywhere || ReleasedOver(control, e)))
                RDFormsControl.Raise(control, Events.Click);
        });

    // MouseDown, MouseUp and MouseMove, with VB's arguments: Button, Shift, X and Y.
    //  - Button is 1, 2 or 4 for the left, right and middle button; on MouseMove it is the buttons held.
    //  - Shift is 1, 2 and 4 for Shift, Ctrl and Alt.
    //  - X and Y are twips from the control's client origin, even on a pixel form: the whole pixel under the pointer, times 15.
    //  - A CommandButton and a CheckBox raise MouseDown, Click, MouseUp: Click comes before MouseUp. A TextBox raises MouseDown,
    //    MouseUp, Click.
    // So the order is set by registration. A Button raises Click in its own release handler, which Avalonia registered first, so
    // MouseUp follows it there. A control whose Click is raised on release by AttachClick attaches its mouse events first.
    // A form's own X and Y are in its scale instead: inScale converts the twips to its ScaleMode.
    public static void AttachMouse<T>(DblClick dblClick = DblClick.None, bool inScale = false) where T : Control
    {
        InputElement.PointerPressedEvent.AddClassHandler<T>((control, e) =>
        {
            var second = dblClick != DblClick.None && e.ClickCount % 2 == 0 && IsVBButton(e.GetCurrentPoint(control))
                         && RDFormsControl.Owns(control, e.Source);
            if (second)
                secondPresses.AddOrUpdate(control, marker);
            else
                secondPresses.Remove(control);
            if (!second || dblClick == DblClick.AfterMouseDown)
                RaiseMouse(control, e, Events.MouseDown, inScale);
            if (second)
                RDFormsControl.Raise(control, Events.DblClick, FormValue.FromBoolean(false));
        }, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<T>((control, e) => RaiseMouse(control, e, Events.MouseUp, inScale),
            handledEventsToo: true);
        InputElement.PointerMovedEvent.AddClassHandler<T>((control, e) => RaiseMouse(control, e, Events.MouseMove, inScale),
            handledEventsToo: true);
    }

    // Whether the press now held on the control is a double click's second, whose release raises no Click.
    public static bool IsSecondPress(Control control) => secondPresses.TryGetValue(control, out _);

    // The Shift argument of a mouse or key event: 1, 2 and 4 for Shift, Ctrl and Alt.
    public static short ShiftOf(KeyModifiers modifiers) =>
        (short)((modifiers.HasFlag(KeyModifiers.Shift) ? 1 : 0) | (modifiers.HasFlag(KeyModifiers.Control) ? 2 : 0)
                | (modifiers.HasFlag(KeyModifiers.Alt) ? 4 : 0));

    private static void RaiseMouse(Control control, PointerEventArgs e, EventDefinition definition, bool inScale)
    {
        if (RDFormsControl.Owns(control, e.Source) && MouseArguments(control, e, moved: ReferenceEquals(definition, Events.MouseMove), inScale) is { } arguments)
            RDFormsControl.Raise(control, definition, arguments);
    }

    // The Button, Shift, X and Y of a mouse event over the control, as AttachMouse raises them: none for a press or a release of a
    // button VB has no event for. A MultiPage and a TabStrip raise theirs with the tab's index before them.
    public static FormValue[]? MouseArguments(Control control, PointerEventArgs e, bool moved, bool inScale = false)
    {
        var point = e.GetCurrentPoint(control);
        var button = moved
            ? (point.Properties.IsLeftButtonPressed ? 1 : 0) | (point.Properties.IsRightButtonPressed ? 2 : 0)
              | (point.Properties.IsMiddleButtonPressed ? 4 : 0)
            : point.Properties.PointerUpdateKind switch
            {
                PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => 1,
                PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => 2,
                PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => 4,
                _ => 0,
            };
        // VB has no event for the other buttons.
        if (button == 0 && !moved)
            return null;
        double x = Twips(point.Position.X), y = Twips(point.Position.Y);
        if (inScale)
            (x, y) = RDFormsDrawingLayer.ScaleOf(RDFormsControl.GetInstance(control)).FromTwips(x, y);
        return [FormValue.FromInteger((short)button), FormValue.FromInteger(ShiftOf(e.KeyModifiers)), FormValue.FromSingle((float)x),
            FormValue.FromSingle((float)y)];
    }

    private static float Twips(double pixels) => (float)(Math.Floor(pixels) * ScaleModeExtensions.TwipsPerPixel);

    private static bool IsVBButton(PointerPoint point) => point.Properties.PointerUpdateKind
        is PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.RightButtonPressed or PointerUpdateKind.MiddleButtonPressed;

    // Whether a release is over the control whose press it ends: hit-tested where it is, since the pressed control has the pointer,
    // and through the window, so that a control drawn over this one has the point.
    private static bool ReleasedOver(Control control, PointerEventArgs e) =>
        TopLevel.GetTopLevel(control) is { } window && RDFormsControl.Owns(control, window.InputHitTest(e.GetPosition(window)));
}
