using Avalonia.Controls;
using Avalonia.LogicalTree;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

// A control that raises its own KeyDown, KeyPress and KeyUp, and gives its form the key first with FormKeys.Preview. The form's window
// raises the form's own key events for the others.
internal interface IRaisesKeyEvents;

// The form's part in its controls' key events, as measured with VB6, native and p-code agreeing. With KeyPreview True, the form raises
// a key event before the control that has the focus does:
//  - KeyDown: a KeyCode the form's handler sets to 0 raises no KeyDown on the control, and the key still does what it does. Another
//    code is not passed on: the control's KeyDown has the key's own.
//  - KeyPress: the control's KeyPress has the KeyAscii the form's handler left, and 0 raises none and types nothing.
//  - KeyUp: the control's KeyUp has the key's own code.
internal static class FormKeys
{
    // The code the control raises its own event with, or 0 for none.
    public static short Preview(Control control, EventDefinition definition, short code, short? shift = null)
    {
        if (control.FindLogicalAncestorOfType<VBFormView>() is not { } view || !view.Form.Root.Get(P.KeyPreview))
            return code;
        var after = Raise(view, definition, code, shift);
        if (ReferenceEquals(definition, VBEvents.KeyPress))
            return after;
        return ReferenceEquals(definition, VBEvents.KeyDown) && after == 0 ? (short)0 : code;
    }

    // Raises a key event of the form's root, and answers the code its handlers left: the code raised where there are none.
    public static short Raise(VBFormView view, EventDefinition definition, short code, short? shift = null)
    {
        FormValue[] arguments = shift is { } mask
            ? [FormValue.FromInteger(code), FormValue.FromInteger(mask)]
            : [FormValue.FromInteger(code)];
        return VBControl.Raise(view, definition, arguments) is { } after && after[0].Kind == FormValueKind.Integer
            ? after[0].AsInteger()
            : code;
    }
}
